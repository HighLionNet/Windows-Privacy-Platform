using System.Security.Cryptography;
using System.Text;

namespace WindowsPrivacyPlatform.Models;

public static class AuthorizationTableIntegrity
{
    public static string Compute(IEnumerable<ManagedObject> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var lines = definitions
            .Where(item => item.WritableTarget is { IsComplete: true })
            .OrderBy(item => item.ObjectId, StringComparer.OrdinalIgnoreCase)
            .Select(item => Canonical(item.ObjectId, item.WritableTarget!));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
    }

    public static bool Matches(string expectedHash, IEnumerable<ManagedObject> definitions) =>
        !string.IsNullOrWhiteSpace(expectedHash) &&
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expectedHash),
            Convert.FromHexString(Compute(definitions)));

    private static string Canonical(string id, WritableTarget target) => string.Join('|',
        id.ToUpperInvariant(), target.Kind, target.Hive.ToUpperInvariant(), target.View,
        target.SubKey.ToUpperInvariant(), target.ValueName.ToUpperInvariant(), target.ValueKind,
        target.SupportsDeletion, target.RequiresElevation,
        string.Join(',', target.SupportedRawValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(value => value.ToUpperInvariant())));
}
