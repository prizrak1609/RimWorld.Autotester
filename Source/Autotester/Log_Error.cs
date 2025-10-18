using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using Debug = UnityEngine.Debug;

namespace Autotester;

[HarmonyPatch(typeof(Log), nameof(Log.Error))]
public static class Log_Error
{
    public static void Prefix(ref string text, out bool __state)
    {
        __state = false;
        var errorText = text;

        text = $"[ERROR]: {text}";

        if (Main.AllowedErrors.Any(allowedString => errorText.Contains(allowedString)))
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

        Debug.LogError(StackTraceUtility.ExtractStackTrace());
        Debug.LogError("[[Autotest failed]]");
        Process.GetCurrentProcess().Kill();
    }
}