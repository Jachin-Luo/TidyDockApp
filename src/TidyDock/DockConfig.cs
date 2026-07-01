using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TidyDock
{
    [DataContract]
    public class DockConfig
    {
        public DockConfig()
        {
            Version = 4;
            Dock = new DockSettings();
            FolderPanel = new FolderPanelSettings();
            Items = new List<DockItem>();
        }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public DockSettings Dock { get; set; }

        [DataMember]
        public FolderPanelSettings FolderPanel { get; set; }

        [DataMember]
        public List<DockItem> Items { get; set; }
    }

    [DataContract]
    public class DockSettings
    {
        public DockSettings()
        {
            Position = "bottom";
            Display = "primary";
            IconSize = 52;
            IconGap = 10;
            Magnification = 1.45;
            Opacity = 0.78;
            CornerRadius = 18;
            AutoHide = false;
            AlwaysOnTop = false;
            StartWithWindows = false;
            StartVisible = true;
            ShowTrayIcon = true;
            ShowItemLabels = false;
            EditMode = false;
            Language = "zh-CN";
            Theme = "system";
        }

        [DataMember]
        public string Position { get; set; }

        [DataMember]
        public string Display { get; set; }

        [DataMember]
        public int IconSize { get; set; }

        [DataMember]
        public int IconGap { get; set; }

        [DataMember]
        public double Magnification { get; set; }

        [DataMember]
        public double Opacity { get; set; }

        [DataMember]
        public int CornerRadius { get; set; }

        [DataMember]
        public bool AutoHide { get; set; }

        [DataMember]
        public bool AlwaysOnTop { get; set; }

        [DataMember]
        public bool StartWithWindows { get; set; }

        [DataMember]
        public bool StartVisible { get; set; }

        [DataMember]
        public bool ShowTrayIcon { get; set; }

        [DataMember]
        public bool ShowItemLabels { get; set; }

        [DataMember]
        public bool EditMode { get; set; }

        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public string Theme { get; set; }
    }

    [DataContract]
    public class FolderPanelSettings
    {
        public FolderPanelSettings()
        {
            MaxItems = 300;
            ShowHiddenFiles = false;
            MaxHeight = 420;
        }

        [DataMember]
        public int MaxItems { get; set; }

        [DataMember]
        public bool ShowHiddenFiles { get; set; }

        [DataMember]
        public int MaxHeight { get; set; }
    }

    [DataContract]
    public class DockItem
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Target { get; set; }

        [DataMember]
        public string Icon { get; set; }

        public bool IsSeparator
        {
            get { return Type == "separator"; }
        }

        public override string ToString()
        {
            if (Type == "separator")
            {
                return "\u5206\u9694\u7b26";
            }

            return "[" + Type + "] " + Name;
        }
    }

    public class FolderEntry
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsOverflowHint { get; set; }
    }
}
