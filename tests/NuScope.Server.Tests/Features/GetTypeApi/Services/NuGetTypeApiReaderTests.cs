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
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public class TypeApiFixture<T> where T : class, new()
                {
                    public const int Answer = 42;
                    static TypeApiFixture() { }
                    public TypeApiFixture() { }
                    public void Transform(ref int value, string text = "value") { }
                    public static Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> operator +(Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> right) { }
                    public static implicit operator string(Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> value) { }
                    protected string Name { get; }
                    public event System.EventHandler Changed;
                }
            }
            """;

        Assert.Equal(NormalizeLineBreaks(expected + Environment.NewLine), NormalizeLineBreaks(api));
    }

    [Fact]
    public void ReadTypeApiIncludesPrivateAndInternalMembersWhenRequested()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, TypeName, includePrivate: true);

        Assert.NotNull(api);
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public class TypeApiFixture<T> where T : class, new()
                {
                    public const int Answer = 42;
                    private int secret;
                    static TypeApiFixture() { }
                    public TypeApiFixture() { }
                    public void Transform(ref int value, string text = "value") { }
                    public static Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> operator +(Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> right) { }
                    public static implicit operator string(Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.TypeApiFixture<T> value) { }
                    internal int GetSecret() { }
                    protected string Name { get; private set; }
                    public event System.EventHandler Changed;
                }
            }
            """;

        Assert.Equal(NormalizeLineBreaks(expected + Environment.NewLine), NormalizeLineBreaks(api));
    }

    [Fact]
    public void ReadTypeApiReturnsNullWhenTypeDoesNotExist()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, "Missing.Type", includePrivate: false);

        Assert.Null(api);
    }

    private static string NormalizeLineBreaks(string value) => value.ReplaceLineEndings("\n");
}
