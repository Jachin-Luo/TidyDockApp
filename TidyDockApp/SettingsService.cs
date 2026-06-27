using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace TidyDock
{
    public class SettingsService
    {
        public SettingsService()
        {
            AppDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TidyDock");
            ConfigDirectory = Path.Combine(AppDirectory, "config");
            IconCacheDirectory = Path.Combine(AppDirectory, "cache", "icons");
            ConfigPath = Path.Combine(ConfigDirectory, "settings.json");
        }

        public string AppDirectory { get; private set; }
        public string ConfigDirectory { get; private set; }
        public string IconCacheDirectory { get; private set; }
        public string ConfigPath { get; private set; }

        public string LogDirectory
        {
            get { return Path.Combine(AppDirectory, "logs"); }
        }

        public DockConfig Load()
        {
            EnsureDirectories();

            if (!File.Exists(ConfigPath))
            {
                var defaults = CreateDefaultConfig();
                Save(defaults);
                return defaults;
            }

            try
            {
                using (var stream = File.OpenRead(ConfigPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(DockConfig));
                    var config = serializer.ReadObject(stream) as DockConfig;
                    return Normalize(config);
                }
            }
            catch
            {
                var backupPath = ConfigPath + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                try
                {
                    File.Copy(ConfigPath, backupPath, true);
                }
                catch
                {
                }

                var defaults = CreateDefaultConfig();
                Save(defaults);
                return defaults;
            }
        }

        public void Save(DockConfig config)
        {
            EnsureDirectories();
            var tempPath = ConfigPath + ".tmp";

            using (var stream = File.Create(tempPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(DockConfig));
                serializer.WriteObject(stream, Normalize(config));
            }

            if (File.Exists(ConfigPath))
            {
                File.Delete(ConfigPath);
            }

            File.Move(tempPath, ConfigPath);
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(IconCacheDirectory);
            Directory.CreateDirectory(LogDirectory);
        }

        private DockConfig Normalize(DockConfig config)
        {
            var version = config == null ? 0 : config.Version;
            if (config == null)
            {
                config = new DockConfig();
            }

            if (config.Dock == null)
            {
                config.Dock = new DockSettings();
            }

            if (config.FolderPanel == null)
            {
                config.FolderPanel = new FolderPanelSettings();
            }

            if (config.Items == null)
            {
                config.Items = new System.Collections.Generic.List<DockItem>();
            }

            if (config.Dock.IconSize < 32)
            {
                config.Dock.IconSize = 52;
            }

            if (config.Dock.IconGap < 0)
            {
                config.Dock.IconGap = 10;
            }

            if (config.Dock.Opacity < 0 || config.Dock.Opacity > 1)
            {
                config.Dock.Opacity = 0.78;
            }

            if (version < 1)
            {
                config.Dock.StartVisible = true;
            }

            if (version < 2)
            {
                config.Dock.ShowTrayIcon = true;
                config.Dock.Language = "zh-CN";
            }

            if (version < 3)
            {
                config.Dock.AlwaysOnTop = false;
                config.Dock.ShowItemLabels = false;
            }

            if (version < 4)
            {
                config.Dock.EditMode = false;
            }

            if (!config.Dock.StartVisible && !config.Dock.ShowTrayIcon)
            {
                config.Dock.ShowTrayIcon = true;
            }

            if (string.IsNullOrEmpty(config.Dock.Language))
            {
                config.Dock.Language = "zh-CN";
            }

            if (string.IsNullOrEmpty(config.Dock.Theme))
            {
                config.Dock.Theme = "system";
            }

            if (config.FolderPanel.MaxItems <= 0)
            {
                config.FolderPanel.MaxItems = 300;
            }

            if (config.Version < 4)
            {
                config.Version = 4;
            }

            return config;
        }

        public DockConfig CreateDefaultConfig()
        {
            return new DockConfig();
        }

        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void ClearIconCache()
        {
            EnsureDirectories();
            try
            {
                var files = Directory.GetFiles(IconCacheDirectory);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
