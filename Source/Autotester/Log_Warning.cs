using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using Debug = UnityEngine.Debug;

namespace Autotester;

[HarmonyPatch(typeof(Log), nameof(Log.Warning))]
public static class Log_Warning
{
    public static void Prefix(ref string text, out bool __state)
    {
        __state = false;
        if (text.Contains(" causes compatibility errors by overwriting "))
        {
            var modName = LoadedModManager.RunningMods.Last()?.Name;
            if (text.StartsWith("[") && !text.StartsWith($"[{modName}]"))
            {
                return;
            }
        }

        text = $"[WARNING]: {text}";

        if (text.Contains("Translation data for language"))
        {
            var saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (text.NullOrEmpty())
            {
                saveLocation = GenFilePaths.SaveDataFolderPath;
            }

            saveLocation = Path.Combine(saveLocation, "TranslationReport.txt");

            text += $"{Environment.NewLine}Generated translation report to {saveLocation}, opening.";
            LanguageReportGenerator.SaveTranslationReport();
            Process.Start(new ProcessStartInfo
            {
                FileName = saveLocation,
                UseShellExecute = true
            });
        }

        var warningText = text;
        if (Main.AllowedWarnings.Any(allowedString => warningText.Contains(allowedString)))
        {
            return;
        }

        __state = true;
    }

    public static void Postfix(bool __state)
    {
        if (!__state)
        {
            return;
        }

        Debug.LogWarning(StackTraceUtility.ExtractStackTrace());
        Debug.LogError("[[Autotest failed]]");
    }
}