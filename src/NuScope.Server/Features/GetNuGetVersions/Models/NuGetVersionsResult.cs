using System.Runtime.CompilerServices;
using Raiqub.NuScope.Features.Common.Models;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

[CollectionBuilder(typeof(NuGetVersionsResult), nameof(Create))]
public sealed record NuGetVersionsResult : NuGetToolCollectionResult<NuGetVersionItem>
{
    public NuGetVersionsResult(ReadOnlySpan<NuGetVersionItem> items)
        : base(items) { }

    public static NuGetVersionsResult Create(ReadOnlySpan<NuGetVersionItem> values) => new(values);
}
