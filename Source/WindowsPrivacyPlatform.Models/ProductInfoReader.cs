using System.Reflection;

namespace WindowsPrivacyPlatform.Models;

public sealed record ProductInfo(
    string Name,
    string Version,
    string BuildIdentifier,
    string Company,
    string Copyright,
    string Description,
    string RepositoryUrl)
{
    public string Mark => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(3)
        .Select(word => char.ToUpperInvariant(word[0])));
}

public static class ProductInfoReader
{
    public static ProductInfo Read(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? assembly.GetName().Version?.ToString(3)
                            ?? "Unknown";
        var version = informational.Split('+')[0];
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .GroupBy(attribute => attribute.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var build = metadata.GetValueOrDefault("BuildIdentifier");
        if (string.IsNullOrWhiteSpace(build) && informational.Contains('+'))
            build = informational[(informational.IndexOf('+') + 1)..];
        if (string.IsNullOrWhiteSpace(build))
            build = assembly.ManifestModule.ModuleVersionId.ToString("N")[..12];
        if (build.Length > 12)
            build = build[..12];

        return new ProductInfo(
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
                ?? assembly.GetName().Name
                ?? "Application",
            version,
            build,
            assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown",
            assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty,
            assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty,
            metadata.GetValueOrDefault("RepositoryUrl") ?? string.Empty);
    }
}
