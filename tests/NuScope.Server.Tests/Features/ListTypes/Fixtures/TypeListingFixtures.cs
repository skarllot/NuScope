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
