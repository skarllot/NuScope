using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.Common.Services;

public interface INuGetPackageMetadataParser
{
    NuGetPackageMetadata? Parse(Stream stream);
}
