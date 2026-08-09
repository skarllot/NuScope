using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class NuGetTypeApiReaderTests
{
    private const string TypeName = "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture`1";

    [Fact]
    public void ReadTypeApiReturnsPublicApiInCSharpDeclarationFormat()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, TypeName, includePrivate: false);

        Assert.NotNull(api);
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class TypeApiFixture<T> where T : class, new()
                {
                    public const int Answer = 42;
                    static TypeApiFixture();
                    public TypeApiFixture();
                    public void Transform(ref int value, string text = "value");
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator +(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator -(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator *(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator /(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static implicit operator string(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> value);
                    public static explicit operator int(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> value);
                    public static bool operator ==(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static bool operator !=(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public override bool Equals(object obj);
                    public override int GetHashCode();
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
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class TypeApiFixture<T> where T : class, new()
                {
                    public const int Answer = 42;
                    private int secret;
                    static TypeApiFixture();
                    public TypeApiFixture();
                    public void Transform(ref int value, string text = "value");
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator +(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator -(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator *(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> operator /(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static implicit operator string(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> value);
                    public static explicit operator int(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> value);
                    public static bool operator ==(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public static bool operator !=(Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> left, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiFixture<T> right);
                    public override bool Equals(object obj);
                    public override int GetHashCode();
                    internal int GetSecret();
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
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public abstract class ApiShapeFixture<T> : Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.ApiBaseFixture, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.IApiContractFixture<int> where T : class, new()
                {
                    public const bool Boolean = true;
                    public const char Character = 'A';
                    public const char Apostrophe = '\'';
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
                    public const string Multiline = "a\r\nb";
                    public const object Nothing = null;
                    public static readonly int Shared;
                    protected internal const int ProtectedValue = 9;
                    protected const int ProtectedOnly = 10;
                    static ApiShapeFixture();
                    protected ApiShapeFixture(T item);
                    public abstract int Transform<TInput>(in int value, TInput input) where TInput : class, new();
                    public virtual void Update(out int result, ref string text, int count = 3);
                    protected static T[] CreateItems();
                    protected internal void ProtectedInternalMethod();
                    public abstract int Value { get; set; }
                    public virtual string this[int arg0] { get; protected set; }
                    public int WriteOnly { set; }
                    public int[,] Matrix { get; }
                    public nint NativeInt { get; }
                    public nuint NativeUInt { get; }
                    public int* Address { get; }
                    public delegate* Callback { get; }
                    public System.Environment.SpecialFolder SpecialFolder { get; }
                    protected internal int ProtectedInternalProperty { get; }
                    public abstract event System.EventHandler Changed;
                }
            }
            """;

        AssertTypeApi(typeof(ApiShapeFixture<>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersPrivateClassMembers()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public abstract class ApiShapeFixture<T> : Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.ApiBaseFixture, Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.IApiContractFixture<int> where T : class, new()
                {
                    public const bool Boolean = true;
                    public const char Character = 'A';
                    public const char Apostrophe = '\'';
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
                    public const string Multiline = "a\r\nb";
                    public const object Nothing = null;
                    public static readonly int Shared;
                    protected internal const int ProtectedValue = 9;
                    protected const int ProtectedOnly = 10;
                    internal static int Mutable;
                    private readonly T storedItem;
                    private protected int state;
                    static ApiShapeFixture();
                    protected ApiShapeFixture(T item);
                    private ApiShapeFixture();
                    public abstract int Transform<TInput>(in int value, TInput input) where TInput : class, new();
                    public virtual void Update(out int result, ref string text, int count = 3);
                    protected static T[] CreateItems();
                    protected internal void ProtectedInternalMethod();
                    private protected void PrivateProtectedMethod();
                    private void PrivateMethod();
                    public abstract int Value { get; set; }
                    public virtual string this[int arg0] { get; protected set; }
                    private int HiddenValue { get; set; }
                    public int WriteOnly { set; }
                    public int[,] Matrix { get; }
                    public nint NativeInt { get; }
                    public nuint NativeUInt { get; }
                    public int* Address { get; }
                    public delegate* Callback { get; }
                    public System.Environment.SpecialFolder SpecialFolder { get; }
                    protected internal int ProtectedInternalProperty { get; private protected set; }
                    internal int InternalProperty { get; private set; }
                    public abstract event System.EventHandler Changed;
                    private event System.EventHandler Hidden;
                }
            }
            """;

        AssertTypeApi(typeof(ApiShapeFixture<>), expected, includePrivate: true);
    }

    [Fact]
    public void ReadTypeApiRendersInterface()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public interface IApiContractFixture<T> where T : struct
                {
                    T Transform<TInput>(in T value, TInput input) where TInput : class, new();
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
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public struct ApiStructFixture
                {
                    public ApiStructFixture(int value);
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
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public enum ApiEnumFixture : short
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
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public delegate TResult ApiDelegateFixture<T, TResult>(T value) where T : class where TResult : class, System.IDisposable;
            }
            """;

        AssertTypeApi(typeof(ApiDelegateFixture<,>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersStaticClass()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
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
    public void ReadTypeApiRendersTypeWithoutNamespace()
    {
        var expected = """
            public sealed class GlobalApiFixture
            {
                public GlobalApiFixture();
            }
            """;

        AssertTypeApi(typeof(GlobalApiFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersSealedClassAndNestedClass()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public sealed class TypeApiPublicSample
                {
                    public TypeApiPublicSample();
                    public sealed class PublicNested
                    {
                        public PublicNested();
                    }
                }
            }
            """;

        AssertTypeApi(typeof(TypeApiPublicSample), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiEscapesKeywordIdentifiers()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class @event<@class> where @class : class
                {
                    public int @namespace;
                    public @event();
                    public void @lock<@for>(@class @base, @for @while) where @for : @class;
                    public int @return { get; set; }
                    public event System.EventHandler @delegate;
                }
            }
            """;

        AssertTypeApi(typeof(@event<>), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersVirtualOverrideAndSealedOverrideMethods()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class ApiMethodModifierDerivedFixture : Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.ApiMethodModifierBaseFixture
                {
                    public sealed override void VirtualMethod();
                    public override int OverrideMethod();
                    public ApiMethodModifierDerivedFixture();
                }
            }
            """;

        AssertTypeApi(typeof(ApiMethodModifierDerivedFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersAbstractClass()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public abstract class ApiAbstractClassFixture
                {
                    protected ApiAbstractClassFixture();
                }
            }
            """;

        AssertTypeApi(typeof(ApiAbstractClassFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersSealedClass()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public sealed class ApiSealedClassFixture
                {
                    public ApiSealedClassFixture();
                }
            }
            """;

        AssertTypeApi(typeof(ApiSealedClassFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersAbstractRecordDeclaration()
    {
        using var stream = File.OpenRead(typeof(ApiAbstractRecordFixture).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            typeof(ApiAbstractRecordFixture).FullName!,
            includePrivate: false
        );

        Assert.NotNull(api);
        Assert.Contains("public abstract record ApiAbstractRecordFixture", api, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadTypeApiRendersSealedRecordDeclaration()
    {
        using var stream = File.OpenRead(typeof(ApiSealedRecordFixture).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            typeof(ApiSealedRecordFixture).FullName!,
            includePrivate: false
        );

        Assert.NotNull(api);
        Assert.Contains("public sealed record ApiSealedRecordFixture", api, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadTypeApiHidesInternalTopLevelTypeByDefault()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiHiddenSample",
            includePrivate: false
        );

        Assert.Null(api);
    }

    [Fact]
    public void ReadTypeApiRendersInternalTopLevelTypeWhenRequested()
    {
        using var stream = File.OpenRead(typeof(TypeApiFixture<>).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.TypeApiHiddenSample",
            includePrivate: true
        );

        Assert.NotNull(api);
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                internal sealed class TypeApiHiddenSample
                {
                    public TypeApiHiddenSample();
                }
            }
            """;

        Assert.Equal(NormalizeLineBreaks(expected + Environment.NewLine), NormalizeLineBreaks(api));
    }

    [Fact]
    public void ReadTypeApiCopiesNonSeekableStreamBeforeReading()
    {
        using var file = File.OpenRead(typeof(ApiStructFixture).Assembly.Location);
        using var stream = new NonSeekableReadStream(file);

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            typeof(ApiStructFixture).FullName!,
            includePrivate: false
        );

        Assert.NotNull(api);
    }

    [Fact]
    public void ReadTypeApiKeepsSeekableInputStreamOpen()
    {
        using var stream = File.OpenRead(typeof(ApiStructFixture).Assembly.Location);
        var position = stream.Position;

        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            typeof(ApiStructFixture).FullName!,
            includePrivate: false
        );

        Assert.NotNull(api);
        Assert.True(stream.CanRead);
        stream.Position = position;
        Assert.NotEqual(-1, stream.ReadByte());
    }

    [Fact]
    public void ReadTypeApiRendersPublicNestedTypes()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class ApiNestedFixture
                {
                    public ApiNestedFixture();
                    public interface IPublicNested
                    {
                    }
                    protected interface IProtectedNested
                    {
                    }
                    protected internal interface IProtectedInternalNested
                    {
                    }
                }
            }
            """;

        AssertTypeApi(typeof(ApiNestedFixture), expected, includePrivate: false);
    }

    [Fact]
    public void ReadTypeApiRendersPrivateNestedTypes()
    {
        var expected = """
            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures
            {
                public class ApiNestedFixture
                {
                    public ApiNestedFixture();
                    public interface IPublicNested
                    {
                    }
                    protected interface IProtectedNested
                    {
                    }
                    protected internal interface IProtectedInternalNested
                    {
                    }
                    private protected interface IPrivateProtectedNested
                    {
                    }
                    internal interface IInternalNested
                    {
                    }
                    private interface IHiddenNested
                    {
                    }
                }
            }
            """;

        AssertTypeApi(typeof(ApiNestedFixture), expected, includePrivate: true);
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

    private sealed class NonSeekableReadStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
