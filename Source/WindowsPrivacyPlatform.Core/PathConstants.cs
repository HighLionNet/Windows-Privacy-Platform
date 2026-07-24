namespace WindowsPrivacyPlatform.Core;

public static class PathConstants
{
    public static readonly string Root =
        @"C:\ProgramData\WindowsPrivacyPlatform";

    public static readonly string KnowledgeBase =
        Path.Combine(Root, "KnowledgeBase");

    public static readonly string Logs =
        Path.Combine(Root, "Logs");

    public static readonly string Artifacts =
        Path.Combine(Root, "Artifacts");

    public static readonly string Evidence =
        Path.Combine(Root, "Evidence");

    public static readonly string Snapshots =
        Path.Combine(Root, "Snapshots");

    public static readonly string Backups =
        Path.Combine(Root, "Backups");

    public static readonly string Configuration =
        Path.Combine(Root, "Configuration");

    public static readonly string Quarantine =
        Path.Combine(Root, "Quarantine");

    public static readonly string History =
        Path.Combine(Root, "History");
}
