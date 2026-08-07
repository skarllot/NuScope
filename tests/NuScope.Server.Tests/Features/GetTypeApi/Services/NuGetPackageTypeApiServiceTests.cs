using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Features.ListTypes.Services;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class NuGetPackageTypeApiServiceTests
{
    [Fact]
    public void GetTypeApiReturnsMatchingAssemblyAndApi()
    {
        var resolver = new StubAssetResolver(
            NuGetPackageAssetsLookup.Found(
                [new NuGetPackageAsset("lib/net8.0/Example.dll", () => new MemoryStream([1]))],
                NuGetPackageAssetSource.Local
            )
        );
        var service = new NuGetPackageTypeApiService(resolver, new StubTypeApiReader("public class Example { }"));

        var result = service.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Example.Type");

        Assert.Null(result.Problem);
        Assert.Equal("lib/net8.0/Example.dll", result.Result!.Assembly);
        Assert.Equal("public class Example { }", result.Result.Api);
    }

    [Theory]
    [InlineData("", "1.0.0", "net8.0", "Example.Type")]
    [InlineData("Example.Package", "", "net8.0", "Example.Type")]
    [InlineData("Example.Package", "1.0.0", "", "Example.Type")]
    [InlineData("Example.Package", "1.0.0", "net8.0", "")]
    public void GetTypeApiRejectsMissingInputs(
        string packageName,
        string version,
        string targetFramework,
        string fullTypeName
    )
    {
        var service = new NuGetPackageTypeApiService(
            new StubAssetResolver(NuGetPackageAssetsLookup.Found([], NuGetPackageAssetSource.Local)),
            new StubTypeApiReader(null)
        );

        var result = service.GetTypeApi(packageName, version, targetFramework, fullTypeName);

        Assert.Equal(400, result.Problem!.Status);
    }

    [Fact]
    public void GetTypeApiReturnsNotFoundWhenNoAssemblyContainsType()
    {
        var resolver = new StubAssetResolver(
            NuGetPackageAssetsLookup.Found(
                [new NuGetPackageAsset("lib/net8.0/Example.dll", () => new MemoryStream([1]))],
                NuGetPackageAssetSource.Local
            )
        );
        var service = new NuGetPackageTypeApiService(resolver, new StubTypeApiReader(null));

        var result = service.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Missing.Type");

        Assert.Equal(404, result.Problem!.Status);
    }

    private sealed class StubAssetResolver(NuGetPackageAssetsLookup result) : INuGetPackageAssetResolver
    {
        public NuGetPackageAssetsLookup GetAssets(string packageName, string version, string targetFramework) => result;
    }

    private sealed class StubTypeApiReader(string? result) : INuGetTypeApiReader
    {
        public string? ReadTypeApi(Stream stream, string fullTypeName, bool includePrivate) => result;
    }
}
