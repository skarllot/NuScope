using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class AttributeTypeApiReaderTests
{
    private const string Mark = "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.ApiMarkAttribute";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RendersAttributesOnEveryDeclarationTarget(bool includePrivate)
    {
        var api = Read(typeof(AttributeApiFixture<>), includePrivate);
        foreach (
            var label in new[]
            {
                "type",
                "field",
                "constructor",
                "constructor-parameter",
                "property",
                "getter",
                "setter",
                "event",
                "event-method",
                "method",
                "parameter",
                "nested-type",
            }
        )
        {
            Assert.Contains(Mark + "(\"" + label + "\"", api, StringComparison.Ordinal);
        }

        Assert.Contains("AttributeApiFixture<[" + Mark + "(\"type-parameter\")] T>", api, StringComparison.Ordinal);
        Assert.Contains("Convert<[" + Mark + "(\"method-parameter\")] TResult>", api, StringComparison.Ordinal);
        Assert.Contains("[return: " + Mark + "(\"return\")]", api, StringComparison.Ordinal);
        Assert.Contains("[return: System.Diagnostics.CodeAnalysis.MaybeNullAttribute]", api, StringComparison.Ordinal);
        Assert.Contains("[field: " + Mark + "(\"property-field\")]", api, StringComparison.Ordinal);
        Assert.Contains("[field: " + Mark + "(\"event-field\")]", api, StringComparison.Ordinal);
        Assert.Contains("[return: " + Mark + "(\"getter-return\")]", api, StringComparison.Ordinal);
        Assert.Contains("[param: " + Mark + "(\"setter-value\")]", api, StringComparison.Ordinal);
        Assert.Contains("this[[" + Mark + "(\"indexer-parameter\")] string arg0]", api, StringComparison.Ordinal);
        Assert.Contains(
            "[System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? value",
            api,
            StringComparison.Ordinal
        );
        Assert.Equal(includePrivate, api.Contains(Mark + "(\"hidden\")", StringComparison.Ordinal));
        Assert.DoesNotContain("CompilerGeneratedAttribute", api, StringComparison.Ordinal);
        Assert.DoesNotContain("NullableAttribute", api, StringComparison.Ordinal);
        Assert.DoesNotContain("NullableContextAttribute", api, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultMemberAttribute", api, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersAttributeValuesAsCSharpExpressions()
    {
        var api = Read(typeof(AttributeApiFixture<>));
        Assert.Contains(
            "Kind = Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.AttributeFixtureKind.Two",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains("Type = typeof(Dictionary<string, List<int[]>>)", api, StringComparison.Ordinal);
        Assert.Contains("Values = new int[] { 1, 2 }", api, StringComparison.Ordinal);
        Assert.Contains("Box = (byte)7", api, StringComparison.Ordinal);
        Assert.Contains("Amount = 1.5F", api, StringComparison.Ordinal);
        Assert.Contains("Text = \"a\\n\\\"b\"", api, StringComparison.Ordinal);
        Assert.Contains("Enabled = true", api, StringComparison.Ordinal);
        Assert.Contains(Mark + "(\"method\", 3)", api, StringComparison.Ordinal);
        var extensions = Read(typeof(AttributeExtensionFixture));
        Assert.Contains("typeof(Dictionary<,>)", extensions, StringComparison.Ordinal);
        Assert.Contains("Box = null", extensions, StringComparison.Ordinal);
        Assert.Contains("Values = null", extensions, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersDelegateAndEnumAttributes()
    {
        var api = Read(typeof(AttributeApiCallback));
        Assert.Contains("[" + Mark + "(\"delegate\")]", api, StringComparison.Ordinal);
        Assert.Contains("[return: " + Mark + "(\"delegate-return\")]", api, StringComparison.Ordinal);
        Assert.Contains("[" + Mark + "(\"delegate-parameter\")] string? value", api, StringComparison.Ordinal);
        Assert.Contains("[" + Mark + "(\"enum-value\")]", Read(typeof(AttributeFixtureKind)), StringComparison.Ordinal);
    }

    [Fact]
    public void EmitsReceiverAttributesBeforeTheThisModifier()
    {
        var api = Read(typeof(AttributeExtensionFixture));
        Assert.Contains("Length([" + Mark + "(\"receiver\")] this string value)", api, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtensionAttribute", api, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneratedApiMarkerAttribute", api, StringComparison.Ordinal);
        Assert.Contains(
            "[Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.NeverInstantiateAttribute]",
            api,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RendersFrameworkEnumMembersInConstructorArguments()
    {
        var api = Read(typeof(AttributeExtensionFixture));

        Assert.Contains(
            "[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]",
            api,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("(System.ComponentModel.EditorBrowsableState)1", api, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersFlagsSignedAndUnsignedMembersAndPreservesUnnamedValues()
    {
        var api = Read(typeof(AttributeExtensionFixture));
        const string fixtureNamespace = "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.";

        Assert.Contains(
            "Options = "
                + fixtureNamespace
                + "AttributeFixtureOptions.Write | "
                + fixtureNamespace
                + "AttributeFixtureOptions.Read",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Options = " + fixtureNamespace + "AttributeFixtureOptions.None",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Options = " + fixtureNamespace + "AttributeFixtureOptions.High",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "SignedKind = " + fixtureNamespace + "AttributeFixtureSignedKind.MinusOne",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains("Kind = (" + fixtureNamespace + "AttributeFixtureKind)99", api, StringComparison.Ordinal);
        Assert.Contains(
            "Options = (" + fixtureNamespace + "AttributeFixtureOptions)4UL",
            api,
            StringComparison.Ordinal
        );
    }

    private static string Read(Type type, bool includePrivate = false)
    {
        using var stream = File.OpenRead(type.Assembly.Location);
        return Assert.IsType<string>(new NuGetTypeApiReader().ReadTypeApi(stream, type.FullName!, includePrivate));
    }
}
