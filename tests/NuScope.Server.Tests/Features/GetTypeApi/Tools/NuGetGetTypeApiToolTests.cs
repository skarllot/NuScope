using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.GetTypeApi.Models;
using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Features.GetTypeApi.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Tools;

public sealed class NuGetGetTypeApiToolTests
{
    [Fact]
    public void GetTypeApiReturnsApiResultWhenServiceFindsType()
    {
        var tool = new NuGetGetTypeApiTool(
            new StubTypeApiService(NuGetTypeApiLookup.Found("lib/net8.0/Example.dll", "public class Type { }"))
        );

        var result = tool.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Example.Type");

        var success = Assert.IsType<NuGetTypeApiResult>(result);
        Assert.Equal("lib/net8.0/Example.dll", success.Assembly);
        Assert.Equal("public class Type { }", success.Api);
    }

    [Fact]
    public void GetTypeApiReturnsProblemWhenServiceFails()
    {
        var problem = NuGetProblemDetailsResult.NotFound("Type was not found.");
        var tool = new NuGetGetTypeApiTool(new StubTypeApiService(NuGetTypeApiLookup.FromProblem(problem)));

        var result = tool.GetTypeApi("Example.Package", "1.0.0", "net8.0", "Missing.Type");

        Assert.Same(problem, result);
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
