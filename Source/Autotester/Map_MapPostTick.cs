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
    private static bool defsLoaded;
    private static bool optionsShown;
    private static bool closeModOptions;
    private static bool cleanupFinished = false;
    private static bool allModsSpawned = false;
    private static List<Thing> allSpawnedThings;
    private static int currentThingIndexSelected;
    private static int currentModIndexSelected = 0;

	public static void Postfix()
	{
		var autoTestMethod =
				AccessTools.Method("SpawnModContent.DebugAutotests:SpawnModDefs", [typeof(ModContentPack)]);
		if (autoTestMethod == null)
		{
			Log.Message($"[Autotester]: SpawnModContent not found.");
			return;
		}

		if (currentModIndexSelected >= LoadedModManager.RunningMods.Count() && !allModsSpawned)
		{
			Log.Message($"[Autotester]: Spawning all items from all mods.");
			autoTestMethod.Invoke(null, LoadedModManager.RunningMods.ToArray());
			Current.CameraDriver.SetRootPosAndSize(Current.CameraDriver.MapPosition.ToVector3(), 60f);

			allModsSpawned = true;
		}

		if (allModsSpawned)
		{
			return;
		}

        ModContentPack modBeingTested = LoadedModManager.RunningMods.ElementAt(currentModIndexSelected);
		if (defsLoaded)
		{
			if (allSpawnedThings == null)
			{
				allSpawnedThings = Find.CurrentMap.listerThings.AllThings
					.Where(thing => thing.def.modContentPack == modBeingTested).ToList();
			}

            if (allSpawnedThings.Empty())
            {
                reset();
                Log.Message($"[Autotester]: No valid items found on the map from {modBeingTested.Name}.");
                return;
            }

            if (GenTicks.TicksGame < 60 || GenTicks.TicksGame % 5 != 0)
			{
				return;
			}

			if (allSpawnedThings.Count == currentThingIndexSelected && !cleanupFinished)
			{
				//modBeingTested = LoadedModManager.RunningMods.Last();
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

				cleanupFinished = true;
                Log.Message("[Autotester]: Nothing more to test, shutting down.");
				return;
			}

			Find.Selector.ClearSelection();
			if (currentThingIndexSelected >= allSpawnedThings.Count)
			{
                reset();
                return;
			}
            var thing = allSpawnedThings[currentThingIndexSelected];
			currentThingIndexSelected++;
			if (thing is not { Spawned: true })
			{
				return;
			}

			Log.Message($"[Autotester]: Selecting {thing.Label} ({thing.def.defName}).");
			Find.Selector.Select(thing);
			return;
		}

		//if (!logShown && GenTicks.TicksGame > 25)
		//{
		//	Log.TryOpenLogWindow();
		//	logShown = true;
		//	return;
		//}

		if (GenTicks.TicksGame < 15)
		{
			return;
		}

		// modBeingTested = LoadedModManager.RunningMods.Last();
		// if (modBeingTested == null)
		// {
		//     defsLoaded = true;
		//     allSpawnedThings = [];
		//     Log.Message("[Autotester]: Cannot find any mod.");
		//     return;
		// }

		if (!optionsShown)
		{
			// modBeingTested = LoadedModManager.RunningMods.Last();
			optionsShown = true;

			var modObject =
				LoadedModManager.ModHandles.FirstOrDefault(mod => mod.Content.PackageId == modBeingTested.PackageId);
			if (modObject == null)
			{
				Log.Message($"[Autotester]: {modBeingTested.Name} modhandle could not be found.");
				return;
			}

			if (string.IsNullOrEmpty(modObject.SettingsCategory()))
			{
				Log.Message($"[Autotester]: {modBeingTested.Name} has no mod-settings.");
				return;
			}

			var settingsWindow = new Dialog_Options(modObject)
			{
				forcePause = false
			};
			Find.WindowStack.Add(settingsWindow);

			closeModOptions = true;
			return;
		}

		if (GenTicks.TicksGame < 30)
		{
			return;
		}

		if (closeModOptions)
		{
			closeModOptions = false;
			Find.WindowStack.TryRemove(typeof(Dialog_Options));
			return;
		}

		if (GenTicks.TicksGame < 45)
		{
			return;
		}

		defsLoaded = true;

		if (!modBeingTested.AllDefs.Any())
		{
            Log.Message($"[Autotester]: ------------------------------ Testing {modBeingTested.Name} ------------------------------");
            Log.Message($"[Autotester]: {modBeingTested.Name} does not have anything to spawn.");
            reset();
            return;
		}
		else
		{
			Log.Message($"[Autotester]: ------------------------------ Testing {modBeingTested.Name} ------------------------------");
		}

		Log.Message($"[Autotester]: Spawning all items from {modBeingTested.Name}.");
		autoTestMethod.Invoke(null, [modBeingTested]);
		Current.CameraDriver.SetRootPosAndSize(Current.CameraDriver.MapPosition.ToVector3(), 60f);

		if (GenTicks.TicksGame < 60)
		{
			return;
		}

		Log.Message($"[Autotester]: ------------------------------ Finished testing {modBeingTested.Name} ------------------------------");
		reset();
	}

	private static void reset()
	{
		defsLoaded = false;
		optionsShown = false;
		closeModOptions = false;
		cleanupFinished = false;
		allModsSpawned = false;
		allSpawnedThings = [];
		currentThingIndexSelected = 0;
		//int currentModIndexSelected = 0;
	}
}