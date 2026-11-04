using System.Reflection;
using System.Text;
using Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class CSharpApiModifiersTests
{
    [Theory]
    [InlineData(MethodAttributes.PrivateScope, "")]
    [InlineData(MethodAttributes.Private, "")]
    [InlineData(MethodAttributes.FamANDAssem, "sealed ")]
    [InlineData(MethodAttributes.Assembly, "sealed ")]
    [InlineData(MethodAttributes.Family, "sealed ")]
    [InlineData(MethodAttributes.FamORAssem, "sealed ")]
    [InlineData(MethodAttributes.Public, "sealed ")]
    public void RendersSealedForNonPrivateInstanceInterfaceMembers(MethodAttributes access, string expected)
    {
        Assert.Equal(expected, Render(access));
    }

    [Theory]
    [InlineData(MethodAttributes.PrivateScope)]
    [InlineData(MethodAttributes.Private)]
    [InlineData(MethodAttributes.FamANDAssem)]
    [InlineData(MethodAttributes.Assembly)]
    [InlineData(MethodAttributes.Family)]
    [InlineData(MethodAttributes.FamORAssem)]
    [InlineData(MethodAttributes.Public)]
    public void DoesNotRenderStaticOrAbstractInterfaceMembersAsSealed(MethodAttributes access)
    {
        Assert.Equal("static ", Render(access | MethodAttributes.Static));
        Assert.Equal("", Render(access | MethodAttributes.Abstract | MethodAttributes.Virtual));
        Assert.Equal(
            "static abstract ",
            Render(access | MethodAttributes.Static | MethodAttributes.Abstract | MethodAttributes.Virtual)
        );
    }

    private static string Render(MethodAttributes attributes)
    {
        var builder = new StringBuilder();
        CSharpApiModifiers.AppendMethodModifiers(builder, attributes, isInterface: true);
        return builder.ToString();
    }
}
