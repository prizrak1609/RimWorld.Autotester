using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Autotester;

[HarmonyPatch(typeof(Map), "MapPostTick")]
public static class Map_MapPostTick
{
    private static bool allowNextState = true;
    private static List<Thing> allSpawnedThings;
    private static int currentThingIndexSelected = 0;
    private static int currentModIndexSelected = 0;
	private static float secondsDelay = 2;
    private static int state = -1;
	private static bool allModsSpawned = false;
    private static List<String> ludeonMods = new List<string>() { "Anomaly", "Biotech", "Core", "Ideology", "Odyssey", "Royalty" };

    public static void Postfix()
	{
        if (currentModIndexSelected >= LoadedModManager.RunningMods.Count())
        {
            return;
        }

        var ticks = GenTicks.TicksPerRealSecond * secondsDelay;
		if (GenTicks.TicksGame % ticks == 0)
		{
			if (allowNextState)
			{
				allowNextState = false;
				state++;
			}
		}
		else
		{
			return;
		}

        ModContentPack modBeingTested = LoadedModManager.RunningMods.ElementAt(currentModIndexSelected);

		if (ludeonMods.Contains(modBeingTested.Name))
		{
			nextMod();
			return;
        }

        Log.Message($"[Autotester]: Mod index  {currentModIndexSelected}  \"{modBeingTested.Name}\": checking thing index  {currentThingIndexSelected}");

        switch (state)
		{
			case 0:
                Log.Message($"[Autotester]: ------------------------------ Testing {modBeingTested.Name} ------------------------------");
                showOptions(modBeingTested);
				break;
			case 1: closeOptions(); break;
            case 2: spawnItems(modBeingTested); break;
            case 3: loadDefs(modBeingTested); break;
			case 4: 
				Log.Message($"[Autotester]: ------------------------------ Finished testing {modBeingTested.Name} ------------------------------");
				nextState(1);
                break;
			default: nextMod(); break;
        }
	}

    private static void showOptions(ModContentPack modBeingTested)
	{
		if (Settings.OpenModOptions == false)
		{
            Log.Message($"[Autotester]: {modBeingTested.Name} skipping opening options.");
            nextState(1);
            return;
		}

		var modObject =
			LoadedModManager.ModHandles.FirstOrDefault(mod => mod.Content.PackageId == modBeingTested.PackageId);
		if (modObject == null)
		{
			Log.Message($"[Autotester]: {modBeingTested.Name} modhandle could not be found.");
            nextState(1);
            return;
		}

		if (string.IsNullOrEmpty(modObject.SettingsCategory()))
		{
			Log.Message($"[Autotester]: {modBeingTested.Name} has no mod-settings.");
			nextState(1);
			return;
		}

		var settingsWindow = new Dialog_Options(modObject)
		{
			forcePause = false
		};
		Find.WindowStack.Add(settingsWindow);

        nextState(2);
    }

	private static void nextState(float delay)
	{
		secondsDelay = delay;
        allowNextState = true;
    }

	private static void closeOptions()
	{
		while (Find.WindowStack.IsOpen<Dialog_Options>())
		{
			Find.WindowStack.TryRemove(typeof(Dialog_Options));
		}
        nextState(1);
    }

	private static void spawnItems(ModContentPack modBeingTested)
	{
		if (Settings.SpawnModItems == false)
		{
            Log.Message($"[Autotester]: {modBeingTested.Name} skipping spawning mod items.");
            nextState(1);
            return;
		}

		var autoTestMethod =
				AccessTools.Method("SpawnModContent.DebugAutotests:SpawnModDefs", [typeof(ModContentPack)]);
		if (autoTestMethod == null)
		{
			Log.Message($"[Autotester]: SpawnModContent not found.");
			nextState(1);
			return;
		}

		Log.Message($"[Autotester]: Spawning all items from {modBeingTested.Name}.");
		try
		{
			autoTestMethod.Invoke(null, [modBeingTested]);
		}
		catch (Exception ex)
		{
			Log.Error($"Failed to spawn items from mod \"{modBeingTested.Name}\": {ex}");
            nextState(1);
			return;
        }

		Current.CameraDriver.SetRootPosAndSize(Current.CameraDriver.MapPosition.ToVector3(), 60f);

		nextState(5);
	}

    private static void loadDefs(ModContentPack modBeingTested)
	{
		if (allSpawnedThings == null || allSpawnedThings.Empty())
		{
			allSpawnedThings = Find.CurrentMap.listerThings.AllThings
				.Where(thing => thing.def.modContentPack == modBeingTested && thing.def.defName != "PowerConduit").ToList();
		}

		if (allSpawnedThings.Empty())
		{
			nextState(1);
			Log.Message($"[Autotester]: No valid items found on the map from {modBeingTested.Name}.");
			return;
		}

		if (allSpawnedThings.Count == currentThingIndexSelected)
		{
			if (Settings.GenerateTranslationTemplatePerMod == false)
			{
                Log.Message($"[Autotester]: {modBeingTested.Name} skippinggenerating translation info.");
                nextState(1);
                return;
			}

            var defInjectMethodInfo =
                    AccessTools.Method(typeof(TranslationFilesCleaner), "CleanupDefInjectionsForDefType");
            var folderPath = Path.Combine(modBeingTested.RootDir, "Source", "TranslationTemplate");
            var dir = new DirectoryInfo(folderPath);
            var mod = modBeingTested.ModMetaData;
            Log.Message($"[Autotester]: Generating translation template to folder {dir.FullName}.");
            if (!dir.Exists)
            {
                dir.Create();
            }

            var files = dir.GetFiles("*.xml", SearchOption.AllDirectories);
            foreach (var langFile in files)
            {
                try
                {
                    langFile.Delete();
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not delete {langFile.Name}: {ex}");
                }
            }

            foreach (var defType in GenDefDatabase.AllDefTypesWithDatabases())
            {
                try
                {
                    defInjectMethodInfo.Invoke(null, [defType, dir.FullName, mod]);
                }
                catch (Exception ex2)
                {
                    Log.Error($"Could not process def-injections for type {defType.Name}: {ex2}");
                }
            }

            Log.Message("[Autotester]: Nothing more to test, proceed with next mode.");
			nextState(1);
			return;
		}

		Find.Selector.ClearSelection();
		
		var thing = allSpawnedThings[currentThingIndexSelected];
		currentThingIndexSelected++;
		if (thing is not { Spawned: true })
		{
			return;
		}

		secondsDelay = 0.5f;
		Log.Message($"[Autotester]: Selecting {thing.Label} ({thing.def.defName}).");
		Find.Selector.Select(thing);
	}

    private static void nextMod()
	{
		state = -1;
		allowNextState = true;
		allSpawnedThings = [];
		currentThingIndexSelected = 0;
		currentModIndexSelected++;
	}
}
