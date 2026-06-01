using Raiqub.NuSpec.Models;

namespace Raiqub.NuSpec.Services;

public interface INuGetPackageMetadataParser
{
    NuGetPackageMetadata? Parse(Stream stream);
}
