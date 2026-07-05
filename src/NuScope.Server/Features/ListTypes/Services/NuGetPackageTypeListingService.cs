using System.Globalization;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed class NuGetPackageTypeListingService : INuGetPackageTypeListingService
{
    private static readonly TimeSpan FilterRegexTimeout = TimeSpan.FromMilliseconds(100);

    private readonly INuGetAssemblyTypeReader assemblyTypeReader;
    private readonly INuGetPackageAssetResolver assetResolver;

    public NuGetPackageTypeListingService(
        INuGetPackageAssetResolver assetResolver,
        INuGetAssemblyTypeReader assemblyTypeReader
    )
    {
        this.assetResolver = assetResolver;
        this.assemblyTypeReader = assemblyTypeReader;
    }

    public NuGetPackageTypesLookup ListTypes(
        string packageName,
        string version,
        string targetFramework,
        string? filterRegex = null,
        bool includePrivate = false,
        bool includeExported = false
    )
    {
        var validationProblem = ValidateInputs(packageName, version, targetFramework, filterRegex, out var filter);
        if (validationProblem is not null)
        {
            return NuGetPackageTypesLookup.FromProblem(validationProblem);
        }

        var assetsLookup = assetResolver.GetAssets(packageName, version, targetFramework);
        if (assetsLookup.Problem is not null)
        {
            return NuGetPackageTypesLookup.FromProblem(assetsLookup.Problem);
        }

        var assemblies = new List<NuGetTypeAssemblyResult>(assetsLookup.Assets!.Count);
        var invalidAssemblyMessages = new List<string>();
        var readableAssemblyCount = 0;
        foreach (var asset in assetsLookup.Assets)
        {
            try
            {
                using var stream = asset.OpenRead();
                var assembly = assemblyTypeReader.ReadTypes(
                    asset.Label,
                    stream,
                    filter,
                    includePrivate,
                    includeExported
                );
                readableAssemblyCount++;
                if (assembly.Types.Count > 0 || assembly.Exported.Count > 0)
                {
                    assemblies.Add(assembly);
                }
            }
            catch (BadImageFormatException exception)
            {
                invalidAssemblyMessages.Add($"{asset.Label}: {exception.Message}");
            }
            catch (RegexMatchTimeoutException exception)
            {
                return NuGetPackageTypesLookup.FromProblem(
                    NuGetProblemDetailsResult.BadRequest(
                        $"The filterRegex value timed out after {exception.MatchTimeout.TotalMilliseconds} ms while matching package type names."
                    )
                );
            }
        }

        if (readableAssemblyCount == 0)
        {
            return NuGetPackageTypesLookup.FromProblem(
                assetsLookup.Source == NuGetPackageAssetSource.Remote
                    ? NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned a package with no readable DLL metadata for '{packageName}' version '{version}'. {DescribeInvalidAssemblies(invalidAssemblyMessages)}"
                    )
                    : NuGetProblemDetailsResult.InternalServerError(
                        $"Package '{packageName}' version '{version}' contains no readable DLL metadata. {DescribeInvalidAssemblies(invalidAssemblyMessages)}"
                    )
            );
        }

        return NuGetPackageTypesLookup.Found(assemblies);
    }

    private static NuGetProblemDetailsResult? ValidateInputs(
        string packageName,
        string version,
        string targetFramework,
        string? filterRegex,
        out Regex? filter
    )
    {
        filter = null;

        if (string.IsNullOrWhiteSpace(packageName))
        {
            return NuGetProblemDetailsResult.BadRequest("Package name is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            return NuGetProblemDetailsResult.BadRequest("Package version is required.");
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return NuGetProblemDetailsResult.BadRequest("Target framework is required.");
        }

        try
        {
            _ = NuGetPackageId.Normalize(packageName);
        }
        catch (ArgumentException exception)
        {
            return NuGetProblemDetailsResult.BadRequest(exception.Message);
        }

        try
        {
            filter = string.IsNullOrWhiteSpace(filterRegex)
                ? null
                : new Regex(filterRegex, RegexOptions.CultureInvariant, FilterRegexTimeout);
        }
        catch (ArgumentException exception)
        {
            return NuGetProblemDetailsResult.BadRequest($"The filterRegex value is invalid: {exception.Message}");
        }

        return null;
    }

    private static string DescribeInvalidAssemblies(List<string> invalidAssemblyMessages)
    {
        return invalidAssemblyMessages.Count == 0
            ? string.Empty
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Unreadable DLLs: {string.Join("; ", invalidAssemblyMessages)}"
            );
    }
}
