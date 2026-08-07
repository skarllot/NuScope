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
