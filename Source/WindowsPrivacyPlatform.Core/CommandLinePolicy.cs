namespace WindowsPrivacyPlatform.Core;

public sealed record StartupArguments(
    bool AuthorizedAdminRelaunch,
    string? InitiatingSid,
    bool ViewOnlyRelaunch,
    bool SuppressShortcutOffer);

/// <summary>Closed command-line grammar. Unknown or incomplete switches are rejected.</summary>
public static class CommandLinePolicy
{
    public static bool TryParse(IReadOnlyList<string> arguments, out StartupArguments parsed, out string error)
    {
        parsed = new StartupArguments(false, null, false, false);
        error = string.Empty;
        if (arguments is null || arguments.Count > 5)
        {
            error = "Too many command-line arguments.";
            return false;
        }

        var admin = false;
        var viewOnly = false;
        var suppressShortcut = false;
        string? sid = null;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.Equals("--authorize-modify", StringComparison.OrdinalIgnoreCase))
            {
                if (admin) return Fail("Duplicate Administrator authorization marker.", out parsed, out error);
                admin = true;
            }
            else if (argument.Equals("--initiating-sid", StringComparison.OrdinalIgnoreCase))
            {
                if (sid is not null || index + 1 >= arguments.Count)
                    return Fail("Invalid initiating identity marker.", out parsed, out error);
                sid = arguments[++index];
                if (sid.Length is < 5 or > 184 || !sid.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase) ||
                    sid.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-'))
                    return Fail("Invalid initiating identity marker.", out parsed, out error);
            }
            else if (argument.Equals("--inspect", StringComparison.OrdinalIgnoreCase))
            {
                if (viewOnly) return Fail("Duplicate View-only marker.", out parsed, out error);
                viewOnly = true;
            }
            else if (argument.Equals("--no-shortcut-offer", StringComparison.OrdinalIgnoreCase))
            {
                if (suppressShortcut) return Fail("Duplicate shortcut marker.", out parsed, out error);
                suppressShortcut = true;
            }
            else
            {
                return Fail("Unknown command-line argument.", out parsed, out error);
            }
        }

        if (admin != (sid is not null) || (admin && viewOnly))
            return Fail("Incomplete or conflicting session marker.", out parsed, out error);

        parsed = new StartupArguments(admin, sid, viewOnly, suppressShortcut);
        return true;
    }

    private static bool Fail(string message, out StartupArguments parsed, out string error)
    {
        parsed = new StartupArguments(false, null, false, false);
        error = message;
        return false;
    }
}
