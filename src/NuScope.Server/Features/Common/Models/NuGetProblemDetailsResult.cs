namespace Raiqub.NuSpec.Features.Common.Models;

public sealed record NuGetProblemDetailsResult : NuGetToolResult
{
    public required string Type { get; init; }

    public required string Title { get; init; }

    public required int Status { get; init; }

    public required string Detail { get; init; }

    public static NuGetProblemDetailsResult Problem(string type, string title, int status, string detail) =>
        new()
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
        };

    public static NuGetProblemDetailsResult Forbidden(string detail) =>
        Problem(ProblemTypes.Forbidden, "Forbidden", 403, detail);

    public static NuGetProblemDetailsResult InternalServerError(string detail) =>
        Problem(ProblemTypes.InternalServerError, "Internal Server Error", 500, detail);

    public static NuGetProblemDetailsResult NotFound(string detail) =>
        Problem(ProblemTypes.NotFound, "Not Found", 404, detail);

    public static NuGetProblemDetailsResult ServiceUnavailable(string detail) =>
        Problem(ProblemTypes.ServiceUnavailable, "Service Unavailable", 503, detail);
}
