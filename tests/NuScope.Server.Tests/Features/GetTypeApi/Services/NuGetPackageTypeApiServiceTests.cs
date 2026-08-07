using Raiqub.NuScope.Features.Common.Models;
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

    [Fact]
    public void GetTypeApiReturnsAssetResolutionProblem()
    {
        var problem = NuGetProblemDetailsResult.Forbidden("Package access was denied.");
        var service = new NuGetPackageTypeApiService(
            new StubAssetResolver(NuGetPackageAssetsLookup.FromProblem(problem, NuGetPackageAssetSource.Remote)),
            new StubTypeApiReader(null)
        );

        var result = service.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Example.Type");

        Assert.Same(problem, result.Problem);
    }

    [Fact]
    public void GetTypeApiRejectsInvalidPackageName()
    {
        var service = new NuGetPackageTypeApiService(
            new StubAssetResolver(NuGetPackageAssetsLookup.Found([], NuGetPackageAssetSource.Local)),
            new StubTypeApiReader(null)
        );

        var result = service.GetTypeApi("Package/Name", "1.0.0", "net8.0", "Example.Type");

        Assert.Equal(400, result.Problem!.Status);
        Assert.Contains("NuGet package ID", result.Problem.Detail);
    }

    [Fact]
    public void GetTypeApiSkipsUnreadableAssembliesAndForwardsPrivateFlag()
    {
        var resolver = new StubAssetResolver(
            NuGetPackageAssetsLookup.Found(
                [
                    new NuGetPackageAsset("lib/net8.0/Native.dll", () => new MemoryStream([0])),
                    new NuGetPackageAsset("lib/net8.0/Managed.dll", () => new MemoryStream([1])),
                ],
                NuGetPackageAssetSource.Local
            )
        );
        var reader = new StubTypeApiReader(
            (stream, fullTypeName, includePrivate) =>
            {
                Assert.Equal("Example.Type", fullTypeName);
                Assert.True(includePrivate);
                if (stream.ReadByte() == 0)
                {
                    throw new BadImageFormatException();
                }

                return "public class Example { }";
            }
        );
        var service = new NuGetPackageTypeApiService(resolver, reader);

        var result = service.GetTypeApi(
            "Example.Package",
            "1.0.0",
            "net8.0",
            "Example.Type",
            includePrivate: true
        );

        Assert.Null(result.Problem);
        Assert.Equal("lib/net8.0/Managed.dll", result.Result!.Assembly);
    }

    [Theory]
    [InlineData(NuGetPackageAssetSource.Local, 500)]
    [InlineData(NuGetPackageAssetSource.Remote, 503)]
    public void GetTypeApiReturnsProblemWhenNoAssemblyIsReadable(NuGetPackageAssetSource source, int expectedStatus)
    {
        var resolver = new StubAssetResolver(
            NuGetPackageAssetsLookup.Found(
                [new NuGetPackageAsset("lib/net8.0/Invalid.dll", () => new MemoryStream([0]))],
                source
            )
        );
        var service = new NuGetPackageTypeApiService(
            resolver,
            new StubTypeApiReader((_, _, _) => throw new BadImageFormatException())
        );

        var result = service.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Example.Type");

        Assert.Equal(expectedStatus, result.Problem!.Status);
    }

    private sealed class StubAssetResolver(NuGetPackageAssetsLookup result) : INuGetPackageAssetResolver
    {
        public NuGetPackageAssetsLookup GetAssets(string packageName, string version, string targetFramework) => result;
    }

    private sealed class StubTypeApiReader : INuGetTypeApiReader
    {
        private readonly Func<Stream, string, bool, string?> readTypeApi;

        public StubTypeApiReader(string? result)
            : this((_, _, _) => result) { }

        public StubTypeApiReader(Func<Stream, string, bool, string?> readTypeApi)
        {
            this.readTypeApi = readTypeApi;
        }

        public string? ReadTypeApi(Stream stream, string fullTypeName, bool includePrivate) =>
            readTypeApi(stream, fullTypeName, includePrivate);
    }
}
