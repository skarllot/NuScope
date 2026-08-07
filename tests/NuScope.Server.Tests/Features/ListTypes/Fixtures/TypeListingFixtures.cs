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

    public static implicit operator string(TypeApiFixture<T> value) => value.Name;

    internal int GetSecret() => secret;
}

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
    public const object? Nothing = null;

    public static readonly int Shared;

    protected internal const int ProtectedValue = 9;

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
        get => index.ToString();
        protected set { }
    }

    public abstract event EventHandler Changed;

    public abstract int Transform<TInput>(in int value, TInput input)
        where TInput : class, new();

    public virtual void Update(out int result, ref string text, int optional = 3)
    {
        result = state;
        text = optional.ToString();
    }

    protected static T[] CreateItems() => [];

    public interface PublicNested;

    protected interface ProtectedNested;

    protected internal interface ProtectedInternalNested;

    private protected interface PrivateProtectedNested;

    internal interface InternalNested;

    private interface PrivateNested;
}

public readonly struct ApiStructFixture(int value) : IEquatable<ApiStructFixture>
{
    public int Value { get; } = value;

    public bool Equals(ApiStructFixture other) => Value == other.Value;
}

public enum ApiEnumFixture : short
{
    None = -1,
    One = 1,
}

public delegate TResult ApiDelegateFixture<in T, out TResult>(T value)
    where T : class
    where TResult : class;

public static class ApiStaticFixture
{
    public static int Value { get; set; }
}
