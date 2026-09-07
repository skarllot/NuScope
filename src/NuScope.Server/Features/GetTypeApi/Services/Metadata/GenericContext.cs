using System.Collections.Immutable;

namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

internal readonly record struct GenericContext(
    ImmutableArray<string> TypeParameters,
    ImmutableArray<string> MethodParameters
)
{
    public static GenericContext Empty { get; } = new([], []);
}
