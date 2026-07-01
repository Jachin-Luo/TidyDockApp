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
            ShortcutDirectory = Path.Combine(AppDirectory, "shortcuts");
            ConfigPath = Path.Combine(ConfigDirectory, "settings.json");
        }

        public string AppDirectory { get; private set; }
        public string ConfigDirectory { get; private set; }
        public string IconCacheDirectory { get; private set; }
        public string ShortcutDirectory { get; private set; }
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
                    config = Normalize(config);
                    MigrateShortcutTargets(config);
                    return config;
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
            var backupPath = ConfigPath + ".bak";

            using (var stream = File.Create(tempPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(DockConfig));
                serializer.WriteObject(stream, Normalize(config));
                stream.Flush(true);
            }

            if (File.Exists(ConfigPath))
            {
                ReplaceConfig(tempPath, backupPath);
                return;
            }

            File.Move(tempPath, ConfigPath);
        }

        private void ReplaceConfig(string tempPath, string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(tempPath, ConfigPath, backupPath, true);
            }
            catch
            {
                File.Copy(ConfigPath, backupPath, true);
                File.Delete(ConfigPath);
                File.Move(tempPath, ConfigPath);
            }
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(IconCacheDirectory);
            Directory.CreateDirectory(ShortcutDirectory);
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

            foreach (var item in config.Items)
            {
                if (item != null && string.IsNullOrEmpty(item.Id))
                {
                    item.Id = NewId();
                }
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

        public string ImportShortcut(string sourcePath, string itemId)
        {
            if (!IsShortcutPath(sourcePath) || !File.Exists(sourcePath))
            {
                return sourcePath;
            }

            EnsureDirectories();

            if (IsPathUnderDirectory(sourcePath, ShortcutDirectory))
            {
                return sourcePath;
            }

            var safeItemId = string.IsNullOrEmpty(itemId) ? NewId() : itemId;
            var targetPath = Path.Combine(ShortcutDirectory, safeItemId + ".lnk");
            File.Copy(sourcePath, targetPath, true);
            return targetPath;
        }

        private void MigrateShortcutTargets(DockConfig config)
        {
            if (config == null || config.Items == null)
            {
                return;
            }

            foreach (var item in config.Items)
            {
                if (item == null ||
                    item.Type != "app" ||
                    string.IsNullOrEmpty(item.Target) ||
                    !IsShortcutPath(item.Target) ||
                    !File.Exists(item.Target) ||
                    IsPathUnderDirectory(item.Target, ShortcutDirectory))
                {
                    continue;
                }

                try
                {
                    item.Target = ImportShortcut(item.Target, item.Id);
                }
                catch
                {
                }
            }
        }

        private static bool IsShortcutPath(string path)
        {
            return string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathUnderDirectory(string path, string directory)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(directory))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return fullPath.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fullPath, fullDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
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
