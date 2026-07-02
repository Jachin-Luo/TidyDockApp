using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class FolderService
{
    public FolderReadResult Read(string path, FolderPanelSettings settings)
    {
        var result = new FolderReadResult();
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                result.ErrorMessage = "\u6587\u4ef6\u5939\u4e0d\u5b58\u5728";
                return result;
            }

            var maxItems = Math.Clamp(settings.MaxItems, 20, 1000);
            var entries = Directory.EnumerateFileSystemEntries(path)
                .Select(CreateEntry)
                .Where(entry => entry != null)
                .Cast<FolderEntry>()
                .Where(entry => settings.ShowHiddenFiles || !IsHidden(entry.Path))
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            result.TotalCount = entries.Count;
            result.IsTruncated = entries.Count > maxItems;
            result.Entries = entries.Take(maxItems).ToList();
            return result;
        }
        catch (UnauthorizedAccessException)
        {
            result.ErrorMessage = "\u6ca1\u6709\u6743\u9650\u8bfb\u53d6\u8be5\u6587\u4ef6\u5939";
            return result;
        }
        catch (DirectoryNotFoundException)
        {
            result.ErrorMessage = "\u6587\u4ef6\u5939\u4e0d\u5b58\u5728";
            return result;
        }
        catch (IOException ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private static FolderEntry? CreateEntry(string path)
    {
        try
        {
            var isDirectory = Directory.Exists(path);
            var isShortcut = File.Exists(path) && string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase);
            return new FolderEntry
            {
                Path = path,
                IsDirectory = isDirectory,
                IsShortcut = isShortcut,
                Name = isShortcut ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path)
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool IsHidden(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.Hidden) == FileAttributes.Hidden;
        }
        catch
        {
            return false;
        }
    }
}
