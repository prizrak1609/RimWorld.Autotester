using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Verse;
using System.Xml.Serialization;

namespace Autotester
{
    [XmlRoot("AutotesterConfig")]
    public class Config
    {
        [XmlElement("spawn_mod_items")]
        public bool SpawnModItems = false;

        [XmlElement("open_mod_options")]
        public bool OpenModOptions = false;

        [XmlElement("generate_translation_template_per_mod")]
        public bool GenerateTranslationTemplatePerMod = false;

        [XmlElement("generate_translation_report")]
        public bool GenerateTranslationReport = false;

        [XmlElement("map_size")]
        public int MapSize = 0;

        public Config() { }
    }

    internal class Settings
    {
        public static readonly Settings instance = new Settings();
        public static bool SpawnModItems
        {
            get
            {
                return instance.config.SpawnModItems;
            }
        }

        public static bool OpenModOptions
        {
            get
            {
                return instance.config.OpenModOptions;
            }
        }

        public static bool GenerateTranslationTemplatePerMod
        {
            get
            {
                return instance.config.GenerateTranslationTemplatePerMod;
            }
        }

        public static bool GenerateTranslationReport
        {
            get
            {
                return instance.config.GenerateTranslationReport;
            }
        }

        public static int MapSize
        {
            get
            {
                return instance.config.MapSize;
            }
        }

        private Config config = new Config();

        private string configFilePath = "";

        private Settings()
        {
        }

        public void init(ModContentPack content)
        {
            configFilePath = Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename($"Mod_{content.FolderName}_Autotester.xml"));
            createIfAbsent();
        }

        public void readSettings()
        {
            var configXml = File.ReadAllText(configFilePath);
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            using (StringReader reader = new StringReader(configXml))
            {
                try
                {
                    config = (Config)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to parse config: {ex.Message}");
                }
            }
        }

        private void createIfAbsent()
        {
            if (!File.Exists(configFilePath))
            {
                var defaultConfig =
                    """
                    <AutotesterConfig>
                    <spawn_mod_items>true</spawn_mod_items>
                    <open_mod_options>false</open_mod_options>
                    <generate_translation_template_per_mod>false</generate_translation_template_per_mod>

                    <!-- stores TranslationReport.txt on Desktop or in GenFilePaths.SaveDataFolderPath(do not know where it is) -->
                    <generate_translation_report>false</generate_translation_report> 

                    <!-- if size 0 - use default game size, if size from 1 to 99 - use size 100 -->
                    <map_size>0</map_size> 
                    </AutotesterConfig> 
                    """;
                File.WriteAllText(configFilePath, defaultConfig);
            }
        }
    }
}
