using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Verse;

namespace Autotester;

public class Main : Mod
{
    public Main(ModContentPack content) : base(content)
    {
        Settings.instance.init(content);
        Settings.instance.readSettings();

        Log.Message($"[Autotester]: Loaded settings");
        new Harmony("Mlie.Autotester").PatchAll(Assembly.GetExecutingAssembly());
    }
}