namespace TidyDock
{
    public static class LocalizationService
    {
        public static string T(DockConfig config, string key)
        {
            var language = config != null && config.Dock != null ? config.Dock.Language : "zh-CN";
            var en = language == "en-US";

            if (key == "settings") return en ? "Settings" : "\u8bbe\u7f6e";
            if (key == "about") return en ? "About" : "\u5173\u4e8e";
            if (key == "close") return en ? "Close" : "\u5173\u95ed";
            if (key == "cancel") return en ? "Cancel" : "\u53d6\u6d88";
            if (key == "ok") return en ? "OK" : "\u786e\u5b9a";
            if (key == "open") return en ? "Open" : "\u6253\u5f00";
            if (key == "rename") return en ? "Rename" : "\u7f16\u8f91\u540d\u79f0";
            if (key == "editTarget") return en ? "Edit target" : "\u7f16\u8f91\u76ee\u6807";
            if (key == "targetPathOrUrl") return en ? "Target path or URL" : "\u76ee\u6807\u8def\u5f84\u6216 URL";
            if (key == "name") return en ? "Name" : "\u540d\u79f0";
            if (key == "changeIcon") return en ? "Change icon" : "\u66f4\u6362\u56fe\u6807";
            if (key == "showInExplorer") return en ? "Show in Explorer" : "\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u663e\u793a";
            if (key == "openInExplorer") return en ? "Open in Explorer" : "\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u6253\u5f00";
            if (key == "removeFromDock") return en ? "Remove from Dock" : "\u4ece Dock \u79fb\u9664";
            if (key == "addUrl") return en ? "Add URL" : "\u6dfb\u52a0 URL";
            if (key == "addSeparator") return en ? "Add separator" : "\u6dfb\u52a0\u5206\u9694\u7b26";
            if (key == "hideDock") return en ? "Hide Dock" : "\u9690\u85cf Dock";
            if (key == "toggleDock") return en ? "Show / Hide Dock" : "\u663e\u793a / \u9690\u85cf Dock";
            if (key == "exit") return en ? "Exit" : "\u9000\u51fa";
            if (key == "dropHint") return en ? "Drop apps, folders, or files" : "\u62d6\u5165\u5e94\u7528\u3001\u6587\u4ef6\u5939\u6216\u6587\u4ef6";
            if (key == "folderMissing") return en ? "Folder not found: " : "\u6587\u4ef6\u5939\u4e0d\u5b58\u5728\uff1a";
            if (key == "targetMissing") return en ? "Target not found: " : "\u76ee\u6807\u4e0d\u5b58\u5728\uff1a";
            if (key == "iconFilter") return en ? "Icons or images|*.ico;*.png;*.jpg;*.jpeg;*.bmp|All files|*.*" : "\u56fe\u6807\u6216\u56fe\u7247|*.ico;*.png;*.jpg;*.jpeg;*.bmp|\u6240\u6709\u6587\u4ef6|*.*";
            if (key == "appFileFilter") return en ? "Apps, shortcuts, or files|*.exe;*.lnk;*.*|All files|*.*" : "\u5e94\u7528\u3001\u5feb\u6377\u65b9\u5f0f\u6216\u6587\u4ef6|*.exe;*.lnk;*.*|\u6240\u6709\u6587\u4ef6|*.*";
            if (key == "settingsTitle") return en ? "TidyDock Settings" : "TidyDock \u8bbe\u7f6e";
            if (key == "appearance") return en ? "Dock Appearance" : "Dock \u5916\u89c2";
            if (key == "behavior") return en ? "Behavior" : "\u884c\u4e3a";
            if (key == "dockItems") return en ? "Dock Items" : "Dock \u9879\u76ee";
            if (key == "language") return en ? "Language" : "\u8bed\u8a00";
            if (key == "autoHide") return en ? "Auto hide" : "\u81ea\u52a8\u9690\u85cf";
            if (key == "startVisible") return en ? "Show Dock at startup" : "\u542f\u52a8\u65f6\u663e\u793a Dock";
            if (key == "showTrayIcon") return en ? "Show tray icon" : "\u663e\u793a\u6258\u76d8\u56fe\u6807";
            if (key == "showItemLabels") return en ? "Show icon names" : "\u663e\u793a\u56fe\u6807\u540d\u79f0";
            if (key == "editMode") return en ? "Edit mode" : "\u7f16\u8f91\u6a21\u5f0f";
            if (key == "editModeRequired") return en ? "Turn on edit mode before changing Dock items." : "\u8bf7\u5148\u5f00\u542f\u7f16\u8f91\u6a21\u5f0f\uff0c\u518d\u4fee\u6539 Dock \u9879\u76ee\u3002";
            if (key == "editModeHint") return en ? "Open settings to enable edit mode" : "\u6253\u5f00\u8bbe\u7f6e\u540e\u5f00\u542f\u7f16\u8f91\u6a21\u5f0f";
            if (key == "alwaysOnTop") return en ? "Always on top" : "\u59cb\u7ec8\u7f6e\u9876";
            if (key == "startWithWindows") return en ? "Start with Windows" : "\u5f00\u673a\u542f\u52a8";
            if (key == "addAppFile") return en ? "Add app/file" : "\u6dfb\u52a0\u5e94\u7528/\u6587\u4ef6";
            if (key == "addFolder") return en ? "Add folder" : "\u6dfb\u52a0\u6587\u4ef6\u5939";
            if (key == "moveUp") return en ? "Move up" : "\u4e0a\u79fb";
            if (key == "moveDown") return en ? "Move down" : "\u4e0b\u79fb";
            if (key == "remove") return en ? "Remove" : "\u79fb\u9664";
            if (key == "openConfigFolder") return en ? "Open config folder" : "\u6253\u5f00\u914d\u7f6e\u76ee\u5f55";
            if (key == "openLogFolder") return en ? "Open log folder" : "\u6253\u5f00\u65e5\u5fd7\u76ee\u5f55";
            if (key == "clearIconCache") return en ? "Clear icon cache" : "\u6e05\u7406\u56fe\u6807\u7f13\u5b58";
            if (key == "resetConfig") return en ? "Reset config" : "\u91cd\u7f6e\u914d\u7f6e";
            if (key == "exitApp") return en ? "Exit app" : "\u9000\u51fa\u7a0b\u5e8f";
            if (key == "traySafety") return en ? "When Dock is hidden at startup, keep the tray icon enabled as an entry point." : "\u542f\u52a8\u65f6\u4e0d\u663e\u793a Dock \u65f6\uff0c\u9700\u8981\u4fdd\u7559\u6258\u76d8\u56fe\u6807\u4f5c\u4e3a\u5165\u53e3\u3002";
            if (key == "cacheCleared") return en ? "Icon cache cleared." : "\u56fe\u6807\u7f13\u5b58\u5df2\u6e05\u7406\u3002";
            if (key == "resetConfirm") return en ? "Reset TidyDock configuration? This only resets Dock settings and does not delete user files." : "\u786e\u5b9a\u8981\u91cd\u7f6e TidyDock \u914d\u7f6e\u5417\uff1f\u8fd9\u53ea\u4f1a\u91cd\u7f6e Dock \u914d\u7f6e\uff0c\u4e0d\u4f1a\u5220\u9664\u4efb\u4f55\u7528\u6237\u6587\u4ef6\u3002";
            if (key == "back") return en ? "Back" : "\u8fd4\u56de\u4e0a\u4e00\u7ea7";
            if (key == "loading") return en ? "Loading..." : "\u6b63\u5728\u8bfb\u53d6...";
            if (key == "folderNotFound") return en ? "Folder not found" : "\u6587\u4ef6\u5939\u4e0d\u5b58\u5728";
            if (key == "folderAccessDenied") return en ? "No permission to read this folder" : "\u6ca1\u6709\u6743\u9650\u8bfb\u53d6\u8be5\u6587\u4ef6\u5939";
            if (key == "tooManyItemsPrefix") return en ? "Too many items. Showing the first " : "\u9879\u76ee\u8fc7\u591a\uff0c\u4ec5\u5c55\u793a\u524d ";
            if (key == "tooManyItemsSuffix") return en ? ". Open in Explorer to view all." : " \u4e2a\u3002\u53ef\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u6253\u5f00\u3002";

            return key;
        }
    }
}
