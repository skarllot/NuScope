namespace Raiqub.NuScope.Features.ListTypes.Services;

public sealed record NuGetPackageAsset(string Label, Func<Stream> OpenRead);
