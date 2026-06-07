using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

public interface INuGetPackageMetadataParser
{
    NuGetPackageMetadata? Parse(Stream stream);
}
