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
        text = $"[ERROR]: {text}";
    }

    public static void Postfix(bool __state)
    {
        Debug.LogError(StackTraceUtility.ExtractStackTrace());
        Debug.LogError("[[Autotest failed]]");
    }
}