using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.ListTypes.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class NuGetTypeApiReaderTests
{
    private const string TypeName = "Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture`1";

    [Fact]
    public void ReadTypeApiReturnsPublicApiInCSharpDeclarationFormat()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, TypeName, includePrivate: false);

        Assert.NotNull(api);
        Assert.Contains("namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures", api);
        Assert.Contains("public class TypeApiFixture<T> where T : class, new()", api);
        Assert.Contains("public const int Answer = 42;", api);
        Assert.Contains("protected string Name { get; }", api);
        Assert.Contains("public void Transform(ref int value, string text = \"value\") { }", api);
        Assert.DoesNotContain("secret", api);
        Assert.DoesNotContain("GetSecret", api);
    }

    [Fact]
    public void ReadTypeApiIncludesPrivateAndInternalMembersWhenRequested()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, TypeName, includePrivate: true);

        Assert.NotNull(api);
        Assert.Contains("private int secret;", api);
        Assert.Contains("internal int GetSecret() { }", api);
        Assert.Contains("protected string Name { get; private set; }", api);
    }

    [Fact]
    public void ReadTypeApiReturnsNullWhenTypeDoesNotExist()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, "Missing.Type", includePrivate: false);

        Assert.Null(api);
    }
}
