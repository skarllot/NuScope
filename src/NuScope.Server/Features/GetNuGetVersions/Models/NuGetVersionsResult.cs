using System.Runtime.CompilerServices;
using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

[CollectionBuilder(typeof(NuGetVersionsResult), nameof(Create))]
public sealed record NuGetVersionsResult : NuGetToolCollectionResult<string>
{
    public NuGetVersionsResult(ReadOnlySpan<string> items)
        : base(items) { }

    public static NuGetVersionsResult Create(ReadOnlySpan<string> values) => new(values);
}
