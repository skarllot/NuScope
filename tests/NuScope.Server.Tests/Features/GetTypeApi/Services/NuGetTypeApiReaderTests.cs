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
    public void ReadTypeApiRendersSupportedClassMembers()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public abstract class ApiShapeFixture<T> : Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.ApiBaseFixture, Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.IApiContractFixture<int> where T : class, new()
                {
                    public const bool Boolean = true;
                    public const char Character = 'A';
                    public const sbyte SignedByte = -1;
                    public const byte Byte = 2;
                    public const short Signed16Value = -3;
                    public const ushort UnsignedShort = 4;
                    public const int Signed32Value = -5;
                    public const uint UnsignedInteger = 6;
                    public const long Signed64Value = -7;
                    public const ulong UnsignedLong = 8;
                    public const float Real32Value = 1.5;
                    public const double Real64Value = 2.5;
                    public const string Text = "a\\\"b";
                    public const object Nothing = null;
                    public static readonly int Shared;
                    protected internal const int ProtectedValue = 9;
                    static ApiShapeFixture() { }
                    protected ApiShapeFixture(T item) { }
                    public abstract int Transform<TInput>(in int value, TInput input) where TInput : class, new() { }
                    public virtual void Update(out int result, ref string text, int optional = 3) { }
                    protected static T[] CreateItems() { }
                    public abstract int Value { get; set; }
                    public virtual string this[int arg0] { get; protected set; }
                    public abstract event System.EventHandler Changed;
                    public interface PublicNested
                    {
                    }
                    protected interface ProtectedNested
                    {
                    }
                    protected internal interface ProtectedInternalNested
                    {
                    }
                }
            }
            """;

        AssertTypeApi(typeof(ApiShapeFixture<>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersPrivateClassMembers()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public abstract class ApiShapeFixture<T> : Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.ApiBaseFixture, Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.IApiContractFixture<int> where T : class, new()
                {
                    public const bool Boolean = true;
                    public const char Character = 'A';
                    public const sbyte SignedByte = -1;
                    public const byte Byte = 2;
                    public const short Signed16Value = -3;
                    public const ushort UnsignedShort = 4;
                    public const int Signed32Value = -5;
                    public const uint UnsignedInteger = 6;
                    public const long Signed64Value = -7;
                    public const ulong UnsignedLong = 8;
                    public const float Real32Value = 1.5;
                    public const double Real64Value = 2.5;
                    public const string Text = "a\\\"b";
                    public const object Nothing = null;
                    public static readonly int Shared;
                    protected internal const int ProtectedValue = 9;
                    private readonly T storedItem;
                    private protected int state;
                    static ApiShapeFixture() { }
                    protected ApiShapeFixture(T item) { }
                    private ApiShapeFixture() { }
                    public abstract int Transform<TInput>(in int value, TInput input) where TInput : class, new() { }
                    public virtual void Update(out int result, ref string text, int optional = 3) { }
                    protected static T[] CreateItems() { }
                    public abstract int Value { get; set; }
                    public virtual string this[int arg0] { get; protected set; }
                    public abstract event System.EventHandler Changed;
                    public interface PublicNested
                    {
                    }
                    protected interface ProtectedNested
                    {
                    }
                    protected internal interface ProtectedInternalNested
                    {
                    }
                    private protected interface PrivateProtectedNested
                    {
                    }
                    internal interface InternalNested
                    {
                    }
                    private interface PrivateNested
                    {
                    }
                }
            }
            """;

        AssertTypeApi(typeof(ApiShapeFixture<>), expected, includePrivate: true);
    }

    [Fact]
    public void ReadTypeApiRendersInterface()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public interface IApiContractFixture<T> where T : struct
                {
                    T Transform<TInput>(in T value, TInput input) where TInput : class, new() { }
                    T Value { get; set; }
                    event System.EventHandler Changed;
                }
            }
            """;

        AssertTypeApi(typeof(IApiContractFixture<>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersStruct()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public struct ApiStructFixture : System.IEquatable<Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.ApiStructFixture>
                {
                    public ApiStructFixture(int value) { }
                    public bool Equals(Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.ApiStructFixture other) { }
                    public int Value { get; }
                }
            }
            """;

        AssertTypeApi(typeof(ApiStructFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersEnum()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public enum ApiEnumFixture
                {
                    None = -1,
                    One = 1,
                }
            }
            """;

        AssertTypeApi(typeof(ApiEnumFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersDelegate()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public delegate TResult ApiDelegateFixture<T, TResult>(T value) where T : class where TResult : class;
            }
            """;

        AssertTypeApi(typeof(ApiDelegateFixture<,>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersStaticClass()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures
            {
                public static class ApiStaticFixture
                {
                    public static int Value { get; set; }
                }
            }
            """;

        AssertTypeApi(typeof(ApiStaticFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiReturnsNullWhenTypeDoesNotExist()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, "Missing.Type", includePrivate: false);

        Assert.Null(api);
    }

    private static void AssertTypeApi(Type type, string expected, bool includePrivate)
    {
        using var stream = File.OpenRead(type.Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, type.FullName!, includePrivate);

        Assert.NotNull(api);
        Assert.Equal(NormalizeLineBreaks(expected + Environment.NewLine), NormalizeLineBreaks(api));
    }

    private static string NormalizeLineBreaks(string value) => value.ReplaceLineEndings("\n");
}
