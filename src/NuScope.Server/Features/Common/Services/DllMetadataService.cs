using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.GetTypeApi.Models;
using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Features.ListTypes.Models;
using Raiqub.NuScope.Features.ListTypes.Services;

namespace Raiqub.NuScope.Features.Common.Services;

public sealed class DllMetadataService(
    IFileSystem fileSystem,
    INuGetAssemblyTypeReader assemblyTypeReader,
    INuGetTypeApiReader typeApiReader
) : IDllMetadataService
{
    private static readonly TimeSpan FilterRegexTimeout = TimeSpan.FromMilliseconds(100);

    public NuGetPackageTypesLookup ListTypes(
        string dllPath,
        string? filterRegex = null,
        bool includePrivate = false,
        bool includeExported = false
    )
    {
        Regex? filter;
        try
        {
            filter = string.IsNullOrWhiteSpace(filterRegex)
                ? null
                : new Regex(filterRegex, RegexOptions.CultureInvariant, FilterRegexTimeout);
        }
        catch (ArgumentException exception)
        {
            return NuGetPackageTypesLookup.FromProblem(
                NuGetProblemDetailsResult.BadRequest($"The filterRegex value is invalid: {exception.Message}")
            );
        }

        return ReadFile(
            dllPath,
            (path, stream) =>
            {
                var assembly = assemblyTypeReader.ReadTypes(
                    fileSystem.Path.GetFileName(path),
                    stream,
                    filter,
                    includePrivate,
                    includeExported
                );
                return NuGetPackageTypesLookup.Found(
                    assembly.Types.Count > 0 || assembly.Exported.Count > 0 ? [assembly] : []
                );
            },
            NuGetPackageTypesLookup.FromProblem
        );
    }

    public NuGetTypeApiLookup GetTypeApi(string dllPath, string fullTypeName, bool includePrivate = false)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return NuGetTypeApiLookup.FromProblem(NuGetProblemDetailsResult.BadRequest("Full type name is required."));
        }

        return ReadFile(
            dllPath,
            (path, stream) =>
            {
                var api = typeApiReader.ReadTypeApi(stream, fullTypeName, includePrivate);
                return api is not null
                    ? NuGetTypeApiLookup.Found(fileSystem.Path.GetFileName(path), api)
                    : NuGetTypeApiLookup.FromProblem(
                        NuGetProblemDetailsResult.NotFound(
                            $"Type '{fullTypeName}' was not found in DLL '{path}', or it is not public."
                        )
                    );
            },
            NuGetTypeApiLookup.FromProblem
        );
    }

    private T ReadFile<T>(string dllPath, Func<string, Stream, T> read, Func<NuGetProblemDetailsResult, T> fromProblem)
    {
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            return fromProblem(NuGetProblemDetailsResult.BadRequest("DLL path is required."));
        }

        try
        {
            var path = fileSystem.Path.GetFullPath(dllPath);
            using var stream = fileSystem.File.OpenRead(path);
            return read(path, stream);
        }
        catch (ArgumentException exception)
        {
            return fromProblem(NuGetProblemDetailsResult.BadRequest($"The DLL path is invalid: {exception.Message}"));
        }
        catch (NotSupportedException exception)
        {
            return fromProblem(NuGetProblemDetailsResult.BadRequest($"The DLL path is invalid: {exception.Message}"));
        }
        catch (FileNotFoundException)
        {
            return fromProblem(NuGetProblemDetailsResult.NotFound($"DLL '{dllPath}' was not found."));
        }
        catch (DirectoryNotFoundException)
        {
            return fromProblem(NuGetProblemDetailsResult.NotFound($"DLL '{dllPath}' was not found."));
        }
        catch (UnauthorizedAccessException)
        {
            return fromProblem(NuGetProblemDetailsResult.Forbidden($"Access to DLL '{dllPath}' was denied."));
        }
        catch (BadImageFormatException exception)
        {
            return fromProblem(
                NuGetProblemDetailsResult.BadRequest(
                    $"DLL '{dllPath}' contains no readable metadata: {exception.Message}"
                )
            );
        }
        catch (RegexMatchTimeoutException exception)
        {
            return fromProblem(
                NuGetProblemDetailsResult.BadRequest(
                    $"The filterRegex value timed out after {exception.MatchTimeout.TotalMilliseconds} ms while matching DLL type names."
                )
            );
        }
        catch (IOException exception)
        {
            return fromProblem(
                NuGetProblemDetailsResult.InternalServerError($"DLL '{dllPath}' could not be read: {exception.Message}")
            );
        }
    }
}
