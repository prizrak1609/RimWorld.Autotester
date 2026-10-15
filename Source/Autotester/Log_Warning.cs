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

        text = $"[WARNING]: {text}";

        if (text.Contains("Translation data for language") && Settings.GenerateTranslationReport)
        {
            var saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (text.NullOrEmpty())
            {
                saveLocation = GenFilePaths.SaveDataFolderPath;
            }

            saveLocation = Path.Combine(saveLocation, "TranslationReport.txt");

            text += $"{Environment.NewLine}Generated translation report to {saveLocation}.";
            LanguageReportGenerator.SaveTranslationReport();
        }
    }

    public static void Postfix(bool __state)
    {
        Debug.LogWarning(StackTraceUtility.ExtractStackTrace());
        Debug.LogWarning("[[Autotest failed]]");
    }
}