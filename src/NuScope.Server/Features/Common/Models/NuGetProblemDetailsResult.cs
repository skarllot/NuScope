namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetProblemDetailsResult : NuGetToolResult
{
    public required string Type { get; init; }

    public required string Title { get; init; }

    public required int Status { get; init; }

    public required string Detail { get; init; }

    public static NuGetProblemDetailsResult NotFound(string detail) =>
        new()
        {
            Type = ProblemTypes.NotFound,
            Title = "Not Found",
            Status = 404,
            Detail = detail,
        };
}
