namespace Raiqub.NuScope.Features.ListTypes.Services;

public static class NuGetPackageId
{
    public static string Normalize(string packageName)
    {
        var normalizedPackageName = packageName.Trim();
        if (
            normalizedPackageName.Contains("://", StringComparison.Ordinal)
            || normalizedPackageName.StartsWith('/')
            || normalizedPackageName.StartsWith('\\')
            || Uri.TryCreate(normalizedPackageName, UriKind.Absolute, out _)
        )
        {
            throw new ArgumentException("Package name must be a relative NuGet package ID.", nameof(packageName));
        }

        if (
            normalizedPackageName.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_'
            )
        )
        {
            throw new ArgumentException(
                "Package name contains characters that are not valid in a NuGet package ID.",
                nameof(packageName)
            );
        }

        if (normalizedPackageName.Split('.').Any(segment => segment.Length == 0))
        {
            throw new ArgumentException(
                "Package name contains path segments that are not valid in a NuGet package ID.",
                nameof(packageName)
            );
        }

        return normalizedPackageName.ToLowerInvariant();
    }
}
