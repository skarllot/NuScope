namespace Raiqub.NuSpec.Features.Common.Models;

public static class ProblemTypes
{
    public const string Forbidden = "https://www.rfc-editor.org/rfc/rfc9110.html#name-403-forbidden";
    public const string InternalServerError =
        "https://www.rfc-editor.org/rfc/rfc9110.html#name-500-internal-server-error";
    public const string NotFound = "https://www.rfc-editor.org/rfc/rfc9110.html#name-404-not-found";
}
