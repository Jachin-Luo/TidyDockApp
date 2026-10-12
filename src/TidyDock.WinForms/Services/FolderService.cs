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
            var entries = new List<FolderEntry>(maxItems + 1);
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(path))
            {
                var entry = CreateEntry(path, entryPath);
                if (entry is null || (!settings.ShowHiddenFiles && entry.IsHidden))
                {
                    continue;
                }

                entries.Add(entry);
                if (entries.Count > maxItems)
                {
                    break;
                }
            }

            var orderedEntries = entries
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            result.TotalCount = entries.Count;
            result.IsTruncated = entries.Count > maxItems;
            result.Entries = orderedEntries.Take(maxItems).ToList();
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

    public static string CreateEntryKey(string rootPath, string entryPath, bool isDirectory)
    {
        var relative = Path.GetRelativePath(rootPath, entryPath).Replace('\\', '/').TrimStart('/');
        return isDirectory && !relative.EndsWith("/", StringComparison.Ordinal) ? relative + "/" : relative;
    }

    private static FolderEntry? CreateEntry(string rootPath, string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            var isDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
            var isShortcut = !isDirectory && string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase);
            return new FolderEntry
            {
                Path = path,
                EntryKey = CreateEntryKey(rootPath, path, isDirectory),
                IsDirectory = isDirectory,
                IsShortcut = isShortcut,
                IsHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden,
                LastWriteTimeUtcTicks = File.GetLastWriteTimeUtc(path).Ticks,
                Length = isDirectory ? 0 : new FileInfo(path).Length,
                Name = isShortcut ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path)
            };
        }
        catch
        {
            return null;
        }
    }

}
