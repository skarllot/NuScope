using ModelContextProtocol.Protocol;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.GetTypeApi.Models;
using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Features.GetTypeApi.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Tools;

public sealed class NuGetGetTypeApiToolTests
{
    [Fact]
    public void GetTypeApiReturnsEmbeddedCSharpResourceWhenServiceFindsType()
    {
        var tool = new NuGetGetTypeApiTool(
            new StubTypeApiService(NuGetTypeApiLookup.Found("lib/net8.0/Example.dll", "public class Type { }"))
        );

        var result = Assert.IsType<EmbeddedResourceBlock>(
            tool.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Example.Type")
        );

        var resource = Assert.IsType<TextResourceContents>(result.Resource);
        Assert.Equal("nuget://packages/Example.Package/1.0.0/net8.0/Example.Type.cs", resource.Uri);
        Assert.Equal("text/x-csharp", resource.MimeType);
        Assert.Equal($"// Assembly: lib/net8.0/Example.dll{Environment.NewLine}public class Type {{ }}", resource.Text);
    }

    [Fact]
    public void GetTypeApiReturnsProblemDetailsWhenServiceFails()
    {
        var problem = NuGetProblemDetailsResult.NotFound("Type was not found.");
        var tool = new NuGetGetTypeApiTool(new StubTypeApiService(NuGetTypeApiLookup.FromProblem(problem)));

        var result = tool.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Missing.Type");

        Assert.Equal(problem, result);
    }

    private sealed class StubTypeApiService(NuGetTypeApiLookup result) : INuGetPackageTypeApiService
    {
        public NuGetTypeApiLookup GetTypeApi(
            string packageName,
            string version,
            string targetFramework,
            string fullTypeName,
            bool includePrivate = false
        ) => result;
    }
}
