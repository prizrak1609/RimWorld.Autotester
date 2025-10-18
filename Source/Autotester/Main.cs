using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Verse;

namespace Autotester;

public class Main : Mod
{
    private static readonly string[] defaultAllowedErrors =
    [
        "Verbose mode detected",
        "Cannot draw radius ring of radius",
        "Could not process def-injections",
        "Could not generate a pawn after",
        "Pawn generation error",
        "Error while generating pawn",
        "coverage has duplicate items",
        "wipeCategories has duplicate categories"
    ];

    private static readonly string[] defaultAllowedWarnings =
    [
        "Scatterer",
        "SoS2 compatibility will happen soon",
        "Parsed "
    ];

    public static string[] AllowedErrors = [];
    public static string[] AllowedWarnings = [];

    public Main(ModContentPack content) : base(content)
    {
        var configFilePath = Path.Combine(GenFilePaths.ConfigFolderPath,
            GenText.SanitizeFilename($"Mod_{content.FolderName}_Autotester.xml"));
        // Create config file if not exist
        if (!File.Exists(configFilePath))
        {
            var defaultConfig =
                "<AutotesterConfig><Allowed></Allowed></AutotesterConfig>";
            File.WriteAllText(configFilePath, defaultConfig);
        }

        // Parse config file as XML
        var configXml = File.ReadAllText(configFilePath);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(configXml);

        // If any of the default errors/warnings are not in the file, add them and re-save the file
        // Add errors as <Error> nodes under /AutotesterConfig/Allowed and warnings as <Warning> nodes
        var somethingChanged = false;
        var allowedNode = xmlDoc.SelectSingleNode("/AutotesterConfig/Allowed");
        foreach (var error in defaultAllowedErrors)
        {
            if (allowedNode?.ChildNodes.Cast<XmlNode>()
                    .Any(node => node.Name == "Error" && node.InnerText == error) == true)
            {
                continue;
            }

            var errorNode = xmlDoc.CreateElement("Error");
            errorNode.InnerText = error;
            allowedNode?.AppendChild(errorNode);
            somethingChanged = true;
        }

        foreach (var warning in defaultAllowedWarnings)
        {
            if (allowedNode?.ChildNodes.Cast<XmlNode>()
                    .Any(node => node.Name == "Warning" && node.InnerText == warning) == true)
            {
                continue;
            }

            var warningNode = xmlDoc.CreateElement("Warning");
            warningNode.InnerText = warning;
            allowedNode?.AppendChild(warningNode);
            somethingChanged = true;
        }

        if (somethingChanged)
        {
            xmlDoc.Save(configFilePath);
        }

        // Load allowed errors and warnings into memory
        AllowedErrors = xmlDoc.SelectNodes("/AutotesterConfig/Allowed/Error")
            ?.Cast<XmlNode>()
            .Select(node => node.InnerText)
            .ToArray() ?? [];
        AllowedWarnings = xmlDoc.SelectNodes("/AutotesterConfig/Allowed/Warning")
            ?.Cast<XmlNode>()
            .Select(node => node.InnerText)
            .ToArray() ?? [];

        Log.Message(
            $"[Autotester]: Loaded {AllowedErrors.Length} errors and {AllowedWarnings.Length} warnings to ignore");
        new Harmony("Mlie.Autotester").PatchAll(Assembly.GetExecutingAssembly());
    }
}