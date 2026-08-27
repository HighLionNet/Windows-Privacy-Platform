using System.Globalization;

namespace WindowsPrivacyPlatform.Core;

/// <summary>Atomic replacement for small application-owned state beneath one explicit root.</summary>
public static class AtomicLocalFile
{
    public static void WriteAllLines(string allowedRoot, string destination, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        WriteText(allowedRoot, destination, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    public static void WriteText(string allowedRoot, string destination, string value)
    {
        var (root, path) = Validate(allowedRoot, destination);
        Directory.CreateDirectory(root);
        var parent = Path.GetDirectoryName(path) ?? root;
        Directory.CreateDirectory(parent);
        var temp = Path.Combine(parent, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");
        try
        {
            File.WriteAllText(temp, value ?? string.Empty, new System.Text.UTF8Encoding(false));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    public static string[] ReadAllLines(string allowedRoot, string source, int maxBytes = 64 * 1024)
    {
        var (_, path) = Validate(allowedRoot, source);
        if (!File.Exists(path)) return [];
        var info = new FileInfo(path);
        if (info.Length < 0 || info.Length > maxBytes)
            throw new InvalidDataException("Local state file exceeds the allowed size.");
        return File.ReadAllLines(path);
    }

    private static (string Root, string Path) Validate(string allowedRoot, string path)
    {
        if (string.IsNullOrWhiteSpace(allowedRoot) || string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("An explicit root and destination are required.");
        var root = Path.GetFullPath(allowedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(path);
        var prefix = root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Refusing local-state access outside the application data root.");
        return (root, fullPath);
    }
}
