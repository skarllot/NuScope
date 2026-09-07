using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class NullableTypeApiReaderTests
{
    [Theory]
    [InlineData("where T : class?")]
    [InlineData("string? Name { get; set; }")]
    [InlineData("string Required { get; }")]
    [InlineData(
        "System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string?[]?>?>? Transform(string? input, ref string? output, T? value);"
    )]
    [InlineData("System.ValueTuple<int, string?, System.Collections.Generic.List<object?>> Tuple")]
    [InlineData("System.Nullable<System.Collections.Generic.KeyValuePair<int, string?>> Value")]
    [InlineData("string?[] Elements")]
    [InlineData("string[]? Array")]
    [InlineData("string?[,]? Matrix")]
    [InlineData("string? this[string? arg0]")]
    [InlineData("event System.EventHandler<string?>? Changed;")]
    [InlineData("TResult? Convert<TResult>(T? input) where TResult : notnull;")]
    [InlineData("where TItem : System.Collections.Generic.IEnumerable<string?>")]
    [InlineData("void Accept(out string? value, in string? input);")]
    [InlineData("string? AssignedName { set; }")]
    public void RendersNullableSignatureTrees(string expected)
    {
        Assert.Contains(expected, Read(typeof(INullableApiFixture<>)), StringComparison.Ordinal);
    }

    [Fact]
    public void RendersNullableDelegate()
    {
        Assert.Contains(
            "delegate string? NullableApiCallback(string? value);",
            Read(typeof(NullableApiCallback)),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RendersFieldsAndInheritedContexts()
    {
        var api = Read(typeof(NullableApiFields));
        Assert.Contains("readonly string? Optional;", api, StringComparison.Ordinal);
        Assert.Contains("readonly string Required;", api, StringComparison.Ordinal);
        Assert.Contains("System.Collections.Generic.Dictionary<string, object?>? Map;", api, StringComparison.Ordinal);
        Assert.Contains("string? Echo(string? value);", api, StringComparison.Ordinal);
        Assert.Contains("string Legacy(string value);", api, StringComparison.Ordinal);
        Assert.Contains("string Echo(string value);", api, StringComparison.Ordinal);
        Assert.Contains(
            "string? Echo(string? value);",
            Read(typeof(NullableApiFields.Nested)),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "string Echo(string value);",
            Read(typeof(NullableApiFields.LegacyNested)),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RendersNullableBaseTypeArguments()
    {
        Assert.Contains(
            "System.Collections.Generic.IEnumerable<string?>",
            Read(typeof(INullableBaseFixture)),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "System.Collections.Generic.List<string?>",
            Read(typeof(NullableDerivedFixture)),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void LeavesLegacyMetadataUnannotated()
    {
        var api = Read(typeof(ILegacyApiFixture));
        Assert.DoesNotContain("?", api, StringComparison.Ordinal);
        Assert.Contains("string Echo(string value);", api, StringComparison.Ordinal);
        Assert.Contains("System.Collections.Generic.Dictionary<string, object[]> Map", api, StringComparison.Ordinal);
    }

    private static string Read(Type type)
    {
        using var stream = File.OpenRead(type.Assembly.Location);
        return Assert.IsType<string>(new NuGetTypeApiReader().ReadTypeApi(stream, type.FullName!, false));
    }
}
