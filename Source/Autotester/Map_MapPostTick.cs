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
    private static bool logShown;
    private static List<Thing> allSpawnedThings;
    private static int currentThingIndexSelected;

    public static void Postfix()
    {
        ModContentPack modBeingTested;
        if (defsLoaded)
        {
            if (allSpawnedThings == null)
            {
                modBeingTested = LoadedModManager.RunningMods.Last();
                allSpawnedThings = Find.CurrentMap.listerThings.AllThings
                    .Where(thing => thing.def.modContentPack == modBeingTested).ToList();
                if (allSpawnedThings.Count == 0)
                {
                    Log.Message($"[Autotester]: No valid items found on the map from {modBeingTested.Name}.");
                    return;
                }
            }

            if (GenTicks.TicksGame < 200 || GenTicks.TicksGame % 5 != 0)
            {
                return;
            }

            if (allSpawnedThings.Count <= currentThingIndexSelected)
            {
                modBeingTested = LoadedModManager.RunningMods.Last();
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

                Log.Message("[Autotester]: Nothing more to test, shutting down.");
                return;
            }

            Find.Selector.ClearSelection();
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

        if (!logShown && GenTicks.TicksGame > 25)
        {
            Log.TryOpenLogWindow();
            logShown = true;
            return;
        }

        if (GenTicks.TicksGame < 50)
        {
            return;
        }

        modBeingTested = LoadedModManager.RunningMods.Last();
        if (modBeingTested == null)
        {
            defsLoaded = true;
            allSpawnedThings = [];
            Log.Message("[Autotester]: Cannot find any mod.");
            return;
        }

        if (!optionsShown)
        {
            modBeingTested = LoadedModManager.RunningMods.Last();
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

        if (GenTicks.TicksGame < 75)
        {
            return;
        }

        if (closeModOptions)
        {
            closeModOptions = false;
            Find.WindowStack.TryRemove(typeof(Dialog_Options));
            return;
        }

        if (GenTicks.TicksGame < 100)
        {
            return;
        }

        defsLoaded = true;

        var autoTestMethod =
            AccessTools.Method("SpawnModContent.DebugAutotests:SpawnModDefs", [typeof(ModContentPack)]);
        if (autoTestMethod == null)
        {
            return;
        }


        if (!modBeingTested.AllDefs.Any())
        {
            Log.Message($"[Autotester]: {modBeingTested.Name} does not have anything to spawn.");
            allSpawnedThings = [];
            return;
        }

        Log.Message($"[Autotester]: Spawning all items from {modBeingTested.Name}.");
        autoTestMethod.Invoke(null, [modBeingTested]);
        Current.CameraDriver.SetRootPosAndSize(Current.CameraDriver.MapPosition.ToVector3(), 60f);
    }
}