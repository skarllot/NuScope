using System.Globalization;

namespace Raiqub.NuScope.Tests.Features.ListTypes.Fixtures;

public sealed class PublicClassFixture
{
    public sealed class PublicNested { }

    private sealed class PrivateNested { }
}

public readonly struct PublicStructFixture;

public interface IPublicInterfaceFixture;

public enum PublicEnumFixture
{
    Value,
}

public delegate void PublicDelegateFixture();

internal sealed class InternalTypeFixture;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1046:Do not overload operator equals on reference types")]
public class TypeApiFixture<T>
    where T : class, new()
{
    public const int Answer = 42;

    private int secret;

    static TypeApiFixture() { }

    public TypeApiFixture() { }

    protected string Name { get; private set; } = string.Empty;

    public event EventHandler? Changed;

    public void Transform(ref int value, string text = "value")
    {
        Changed?.Invoke(this, EventArgs.Empty);
        secret = value;
    }

    public static TypeApiFixture<T> operator +(TypeApiFixture<T> left, TypeApiFixture<T> right) => left;

    public static TypeApiFixture<T> operator -(TypeApiFixture<T> left, TypeApiFixture<T> right) => left;

    public static TypeApiFixture<T> operator *(TypeApiFixture<T> left, TypeApiFixture<T> right) => left;

    public static TypeApiFixture<T> operator /(TypeApiFixture<T> left, TypeApiFixture<T> right) => left;

    public static implicit operator string(TypeApiFixture<T> value) => value.Name;

    public static explicit operator int(TypeApiFixture<T> value) => value.secret;

    public static bool operator ==(TypeApiFixture<T>? left, TypeApiFixture<T>? right) => ReferenceEquals(left, right);

    public static bool operator !=(TypeApiFixture<T>? left, TypeApiFixture<T>? right) => !(left == right);

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => base.GetHashCode();

    internal int GetSecret() => secret;
}

public class ApiMethodModifierBaseFixture
{
    public virtual void VirtualMethod() { }

    public virtual int OverrideMethod() => 1;
}

public class ApiMethodModifierDerivedFixture : ApiMethodModifierBaseFixture
{
    public sealed override void VirtualMethod() { }

    public override int OverrideMethod() => 2;
}

public abstract class ApiAbstractClassFixture;

public sealed class ApiSealedClassFixture;

public abstract record ApiAbstractRecordFixture;

public sealed record ApiSealedRecordFixture;

public abstract class ApiBaseFixture;

public interface IApiContractFixture<T>
    where T : struct
{
    T Value { get; set; }

    event EventHandler Changed;

    T Transform<TInput>(in T value, TInput input)
        where TInput : class, new();
}

public abstract class ApiShapeFixture<T> : ApiBaseFixture, IApiContractFixture<int>
    where T : class, new()
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
    public const float Real32Value = 1.5F;
    public const double Real64Value = 2.5D;
    public const string Text = "a\\\"b";
    public const string Multiline = "a\r\nb";
    public const object? Nothing = null;

    public static readonly int Shared;

    protected internal const int ProtectedValue = 9;

    protected const int ProtectedOnly = 10;

    internal static int Mutable = 1;

    private readonly T storedItem;

    private protected int state;

    static ApiShapeFixture() { }

    protected ApiShapeFixture(T item)
    {
        storedItem = item;
    }

    private ApiShapeFixture()
    {
        storedItem = default!;
    }

    public abstract int Value { get; set; }

    public virtual string this[int index]
    {
        get => index.ToString(CultureInfo.InvariantCulture);
        protected set { }
    }

    private int HiddenValue { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1044:Properties should not be write only")]
    public int WriteOnly
    {
        set => state = value;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1814:Prefer jagged arrays over multidimensional"
    )]
    public int[,] Matrix { get; } = new int[1, 1];

    public nint NativeInt { get; }

    public nuint NativeUInt { get; }

    public unsafe int* Address { get; }

    public unsafe delegate* <int, void> Callback { get; }

    public Environment.SpecialFolder SpecialFolder { get; }

    protected internal int ProtectedInternalProperty { get; private protected set; }

    internal int InternalProperty { get; private set; }

    public abstract event EventHandler Changed;

    private event EventHandler Hidden
    {
        add => state++;
        remove => state--;
    }

    public abstract int Transform<TInput>(in int value, TInput input)
        where TInput : class, new();

    public virtual void Update(out int result, ref string text, int count = 3)
    {
        result = state;
        text = count.ToString(CultureInfo.InvariantCulture);
    }

    protected static T[] CreateItems() => [];

    protected internal void ProtectedInternalMethod() => state++;

    private protected void PrivateProtectedMethod() => state--;

    private void PrivateMethod() => state = 0;
}

public readonly struct ApiStructFixture(int value)
{
    public int Value { get; } = value;
}

public enum ApiEnumFixture : short
{
    None = -1,
    One = 1,
}

public delegate TResult ApiDelegateFixture<in T, out TResult>(T value)
    where T : class
    where TResult : class, IDisposable;

public static class ApiStaticFixture
{
    public static int Value { get; set; }
}

public class ApiNestedFixture
{
    public ApiNestedFixture() { }

    public interface IPublicNested;

    protected interface IProtectedNested;

    protected internal interface IProtectedInternalNested;

    private protected interface IPrivateProtectedNested;

    internal interface IInternalNested;

    private interface IHiddenNested;
}
