using Raiqub.NuSpec.Models;

namespace Raiqub.NuSpec.Services;

public interface INuGetPackageMetadataService
{
    NuGetPackageMetadataResult GetNuGetPackageMetadata(string packageName, string version);
}
