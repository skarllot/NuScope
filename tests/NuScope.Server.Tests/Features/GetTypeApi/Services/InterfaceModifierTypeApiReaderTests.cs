using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class InterfaceModifierTypeApiReaderTests
{
    [Theory]
    [InlineData("void AbstractMethod();")]
    [InlineData("virtual void VirtualMethod();")]
    [InlineData("sealed void SealedMethod();")]
    [InlineData("static abstract void StaticAbstractMethod();")]
    [InlineData("static virtual void StaticVirtualMethod();")]
    [InlineData("static void StaticMethod();")]
    [InlineData("int AbstractProperty { get; set; }")]
    [InlineData("virtual int VirtualProperty { get; set; }")]
    [InlineData("sealed int SealedProperty { get; set; }")]
    [InlineData("static abstract int StaticAbstractProperty { get; set; }")]
    [InlineData("static virtual int StaticVirtualProperty { get; set; }")]
    [InlineData("static int StaticProperty { get; set; }")]
    [InlineData("int this[int arg0] { get; }")]
    [InlineData("virtual int this[string arg0] { get; }")]
    [InlineData("sealed int this[long arg0] { get; }")]
    [InlineData("event EventHandler AbstractEvent;")]
    [InlineData("virtual event EventHandler VirtualEvent;")]
    [InlineData("sealed event EventHandler SealedEvent;")]
    [InlineData("static abstract event EventHandler StaticAbstractEvent;")]
    [InlineData("static virtual event EventHandler StaticVirtualEvent;")]
    [InlineData("static event EventHandler StaticEvent;")]
    public void RendersInterfaceMemberModifiers(string declaration)
    {
        var api = ReadApi(includePrivate: false).ReplaceLineEndings("\n");

        Assert.Contains("\n    " + declaration + "\n", api, StringComparison.Ordinal);
        Assert.DoesNotContain("override ", api, StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotRenderPrivateHelpersAsSealed()
    {
        var api = ReadApi(includePrivate: true).ReplaceLineEndings("\n");

        Assert.Contains("\n    void PrivateMethod();\n", api, StringComparison.Ordinal);
        Assert.Contains("\n    static void PrivateStaticMethod();\n", api, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcludesPrivateHelpersByDefault()
    {
        var api = ReadApi(includePrivate: false);

        Assert.DoesNotContain("PrivateMethod", api, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateStaticMethod", api, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("sealed void ProtectedSealedMethod();")]
    [InlineData("sealed void InternalSealedMethod();")]
    [InlineData("sealed void ProtectedInternalSealedMethod();")]
    [InlineData("sealed void PrivateProtectedSealedMethod();")]
    [InlineData("sealed int ProtectedSealedProperty { get; }")]
    [InlineData("sealed int this[short arg0] { get; }")]
    [InlineData("sealed event EventHandler InternalSealedEvent;")]
    public void RendersNonPublicSealedInterfaceMembers(string declaration)
    {
        var api = ReadApi(includePrivate: true).ReplaceLineEndings("\n");

        Assert.Contains("\n    " + declaration + "\n", api, StringComparison.Ordinal);
    }

    private static string ReadApi(bool includePrivate)
    {
        using var stream = File.OpenRead(typeof(IInterfaceModifierApiFixture).Assembly.Location);
        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            typeof(IInterfaceModifierApiFixture).FullName!,
            includePrivate
        );

        Assert.NotNull(api);
        return api;
    }
}
