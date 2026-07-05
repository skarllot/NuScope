namespace Raiqub.NuScope.Features.ListTypes.Models;

public sealed record NuGetTypeAssemblyResult
{
    public required string Assembly { get; init; }

    public required IReadOnlyList<string> Exported { get; init; }

    public required IReadOnlyList<string> Types { get; init; }
}
