using System.IO.Abstractions;
using System.IO.Compression;
using System.Net;
using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed class NuGetPackageAssetResolver(IFileSystem fileSystem, HttpClient httpClient)
    : INuGetPackageAssetResolver
{
    private const string NuGetFlatContainerBaseAddress = "https://api.nuget.org/v3-flatcontainer/";

    public NuGetPackageAssetsLookup GetAssets(string packageName, string version, string targetFramework)
    {
        var packageDirectory = GetPackageDirectory(packageName, version);
        return fileSystem.Directory.Exists(packageDirectory)
            ? GetLocalAssets(packageName, version, targetFramework, packageDirectory)
            : GetRemoteAssets(packageName, version, targetFramework);
    }

    private NuGetPackageAssetsLookup GetLocalAssets(
        string packageName,
        string version,
        string targetFramework,
        string packageDirectory
    )
    {
        try
        {
            var assetFolders = NuGetPackageAssetSelector.SelectCompatibleAssetFolders(
                targetFramework,
                NuGetPackageAssetSelector
                    .AssetRoots.SelectMany(root =>
                    {
                        var assetRootDirectory = fileSystem.Path.Combine(packageDirectory, root);
                        return fileSystem.Directory.Exists(assetRootDirectory)
                            ? fileSystem
                                .Directory.EnumerateDirectories(assetRootDirectory)
                                .Select(path => new NuGetPackageAssetFolder(
                                    root,
                                    fileSystem.Path.GetFileName(path)!,
                                    path
                                ))
                            : [];
                    })
                    .ToArray()
            );
            if (assetFolders.Length == 0)
            {
                return LocalProblem(
                    NuGetProblemDetailsResult.NotFound(
                        $"Package '{packageName}' version '{version}' has no compatible lib or ref assets for '{targetFramework}'."
                    )
                );
            }

            var assets = assetFolders
                .SelectMany(assetFolder =>
                    fileSystem
                        .Directory.EnumerateFiles(assetFolder.Path, "*.dll")
                        .Select(path => new NuGetPackageAsset(
                            $"{assetFolder.Root}/{fileSystem.Path.GetFileName(path)!}",
                            () => fileSystem.File.OpenRead(path)
                        ))
                )
                .OrderBy(asset => GetAssetRoot(asset.Label), NuGetPackageAssetRootComparer.Instance)
                .ThenBy(asset => GetAssetFileName(asset.Label), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (assets.Length == 0)
            {
                return LocalProblem(
                    NuGetProblemDetailsResult.NotFound(
                        $"Package '{packageName}' version '{version}' has no DLL assets for {NuGetPackageAssetSelector.DescribeAssetFolders(assetFolders)}."
                    )
                );
            }

            return NuGetPackageAssetsLookup.Found(assets, NuGetPackageAssetSource.Local);
        }
        catch (UnauthorizedAccessException)
        {
            return LocalProblem(
                NuGetProblemDetailsResult.Forbidden(
                    $"Access to package '{packageName}' version '{version}' in the local NuGet cache was denied."
                )
            );
        }
        catch (IOException)
        {
            return LocalProblem(
                NuGetProblemDetailsResult.InternalServerError(
                    $"An I/O error occurred while reading package '{packageName}' version '{version}' from the local NuGet cache."
                )
            );
        }
    }

    private NuGetPackageAssetsLookup GetRemoteAssets(string packageName, string version, string targetFramework)
    {
        var packageId = NuGetPackageId.Normalize(packageName);
        var normalizedVersion = version.Trim().ToLowerInvariant();
        var requestUri = new Uri(
            $"{NuGetFlatContainerBaseAddress}{packageId}/{normalizedVersion}/{packageId}.{normalizedVersion}.nupkg",
            UriKind.Absolute
        );

        try
        {
            using var response = Send(requestUri);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return RemoteProblem(
                    NuGetProblemDetailsResult.NotFound(
                        $"Package '{packageName}' version '{version}' was not found in the local NuGet cache or on nuget.org."
                    )
                );
            }

            if (!response.IsSuccessStatusCode)
            {
                return RemoteProblem(
                    NuGetProblemDetailsResult.ServiceUnavailable(
                        $"nuget.org returned {(int)response.StatusCode} while reading package '{packageName}' version '{version}'."
                    )
                );
            }

            using var packageStream = response.Content.ReadAsStream();
            return GetRemoteAssets(packageName, version, targetFramework, packageStream);
        }
        catch (ArgumentException exception)
        {
            return RemoteProblem(NuGetProblemDetailsResult.BadRequest(exception.Message));
        }
        catch (HttpRequestException exception)
        {
            return RemoteProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org could not be reached while reading package '{packageName}' version '{version}': {exception.Message}"
                )
            );
        }
        catch (IOException exception)
        {
            return RemoteProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"A network I/O error occurred while reading package '{packageName}' version '{version}' from nuget.org: {exception.Message}"
                )
            );
        }
        catch (InvalidDataException exception)
        {
            return RemoteProblem(
                NuGetProblemDetailsResult.ServiceUnavailable(
                    $"nuget.org returned an invalid package archive for '{packageName}' version '{version}': {exception.Message}"
                )
            );
        }
    }

    private static NuGetPackageAssetsLookup GetRemoteAssets(
        string packageName,
        string version,
        string targetFramework,
        Stream packageStream
    )
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
        var assetFolders = archive
            .Entries.Where(entry =>
                NuGetPackageAssetSelector.AssetRoots.Any(root =>
                    entry.FullName.StartsWith($"{root}/", StringComparison.OrdinalIgnoreCase)
                )
            )
            .Select(entry => new NuGetPackageAssetFolder(
                GetZipAssetRoot(entry.FullName),
                GetZipAssetFramework(entry.FullName),
                GetZipAssetFramework(entry.FullName)
            ))
            .Where(asset => !string.IsNullOrWhiteSpace(asset.Name))
            .DistinctBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var compatibleAssetFolders = NuGetPackageAssetSelector.SelectCompatibleAssetFolders(
            targetFramework,
            assetFolders
        );
        if (compatibleAssetFolders.Length == 0)
        {
            return RemoteProblem(
                NuGetProblemDetailsResult.NotFound(
                    $"Package '{packageName}' version '{version}' has no compatible lib or ref assets for '{targetFramework}'."
                )
            );
        }

        var assets = compatibleAssetFolders
            .SelectMany(assetFolder =>
                archive
                    .Entries.Where(entry =>
                        entry.FullName.StartsWith(
                            $"{assetFolder.Root}/{assetFolder.Name}/",
                            StringComparison.OrdinalIgnoreCase
                        ) && entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(entry =>
                    {
                        var bytes = ReadAllBytes(entry);
                        return new NuGetPackageAsset(
                            $"{assetFolder.Root}/{GetZipFileName(entry.FullName)}",
                            () => new MemoryStream(bytes, writable: false)
                        );
                    })
            )
            .OrderBy(asset => GetAssetRoot(asset.Label), NuGetPackageAssetRootComparer.Instance)
            .ThenBy(asset => GetAssetFileName(asset.Label), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (assets.Length == 0)
        {
            return RemoteProblem(
                NuGetProblemDetailsResult.NotFound(
                    $"Package '{packageName}' version '{version}' has no DLL assets for {NuGetPackageAssetSelector.DescribeAssetFolders(compatibleAssetFolders)}."
                )
            );
        }

        return NuGetPackageAssetsLookup.Found(assets, NuGetPackageAssetSource.Remote);
    }

    private string GetPackageDirectory(string packageName, string version)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return fileSystem.Path.Combine(
            userProfile,
            ".nuget",
            "packages",
            packageName.ToLowerInvariant(),
            version.ToLowerInvariant()
        );
    }

    private HttpResponseMessage Send(Uri requestUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return httpClient.SendAsync(request).GetAwaiter().GetResult();
    }

    private static byte[] ReadAllBytes(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var memoryStream = new MemoryStream();
        entryStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static NuGetPackageAssetsLookup LocalProblem(NuGetProblemDetailsResult problem) =>
        NuGetPackageAssetsLookup.FromProblem(problem, NuGetPackageAssetSource.Local);

    private static NuGetPackageAssetsLookup RemoteProblem(NuGetProblemDetailsResult problem) =>
        NuGetPackageAssetsLookup.FromProblem(problem, NuGetPackageAssetSource.Remote);

    private static string GetZipAssetFramework(string entryPath)
    {
        var segments = entryPath.Split('/');
        return segments.Length >= 3 ? segments[1] : string.Empty;
    }

    private static string GetZipAssetRoot(string entryPath)
    {
        var segments = entryPath.Split('/');
        return segments.Length >= 1 ? segments[0] : string.Empty;
    }

    private static string GetZipFileName(string entryPath) => entryPath[(entryPath.LastIndexOf('/') + 1)..];

    private static string GetAssetRoot(string label) => label[..label.IndexOf('/')];

    private static string GetAssetFileName(string label) => label[(label.IndexOf('/') + 1)..];
}
