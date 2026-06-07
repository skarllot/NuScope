using System.Globalization;

namespace Raiqub.NuSpec.Features.Common.Services;

internal sealed record NuGetPackageVersion(int[] Release, string? Prerelease) : IComparable<NuGetPackageVersion>
{
    public static NuGetPackageVersion? TryParse(string version)
    {
        var versionWithoutMetadata = version.Split('+', 2)[0];
        var parts = versionWithoutMetadata.Split('-', 2);
        var releaseParts = parts[0].Split('.', StringSplitOptions.TrimEntries);
        if (releaseParts.Length == 0)
        {
            return null;
        }

        var release = new int[releaseParts.Length];
        for (var index = 0; index < releaseParts.Length; index++)
        {
            var part = releaseParts[index];
            if (
                part.Length == 0
                || !part.All(char.IsAsciiDigit)
                || !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out release[index])
            )
            {
                return null;
            }
        }

        return new NuGetPackageVersion(release, parts.Length > 1 ? parts[1] : null);
    }

    public int CompareTo(NuGetPackageVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var releaseLength = Math.Max(Release.Length, other.Release.Length);
        for (var index = 0; index < releaseLength; index++)
        {
            var comparison = GetReleasePart(index).CompareTo(other.GetReleasePart(index));
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return ComparePrerelease(other);
    }

    private int ComparePrerelease(NuGetPackageVersion other)
    {
        return (Prerelease, other.Prerelease) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => ComparePrereleaseIdentifiers(Prerelease, other.Prerelease),
        };
    }

    private int GetReleasePart(int index) => index < Release.Length ? Release[index] : 0;

    private static int ComparePrereleaseIdentifiers(string prerelease, string otherPrerelease)
    {
        var identifiers = prerelease.Split('.');
        var otherIdentifiers = otherPrerelease.Split('.');
        var identifierLength = Math.Max(identifiers.Length, otherIdentifiers.Length);

        for (var index = 0; index < identifierLength; index++)
        {
            if (index >= identifiers.Length)
            {
                return -1;
            }

            if (index >= otherIdentifiers.Length)
            {
                return 1;
            }

            var identifier = identifiers[index];
            var otherIdentifier = otherIdentifiers[index];
            var identifierIsNumeric = IsNumericIdentifier(identifier);
            var otherIdentifierIsNumeric = IsNumericIdentifier(otherIdentifier);

            var comparison = (identifierIsNumeric, otherIdentifierIsNumeric) switch
            {
                (true, false) => -1,
                (false, true) => 1,
                _ => CultureInfo.InvariantCulture.CompareInfo.Compare(
                    identifier,
                    otherIdentifier,
                    CompareOptions.IgnoreCase | CompareOptions.NumericOrdering
                ),
            };

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static bool IsNumericIdentifier(string identifier) =>
        identifier.Length > 0 && identifier.All(char.IsAsciiDigit);
}
