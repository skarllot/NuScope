using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetTypeApi.Models;

public sealed record NuGetTypeApiResult : NuGetToolResult
{
    public required string Assembly { get; init; }

    public required string Api { get; init; }
}
