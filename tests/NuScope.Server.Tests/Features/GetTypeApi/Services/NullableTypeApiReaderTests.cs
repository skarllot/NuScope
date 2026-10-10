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
    [InlineData("Dictionary<string, List<string?[]?>?>? Transform(string? input, ref string? output, T? value);")]
    [InlineData("ValueTuple<int, string?, List<object?>> Tuple")]
    [InlineData("KeyValuePair<int, string?>? Value")]
    [InlineData("string?[] Elements")]
    [InlineData("string[]? Array")]
    [InlineData("string?[,]? Matrix")]
    [InlineData("string? this[string? arg0]")]
    [InlineData("event EventHandler<string?>? Changed;")]
    [InlineData("TResult? Convert<TResult>(T? input) where TResult : notnull;")]
    [InlineData("where TItem : IEnumerable<string?>")]
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
        Assert.Contains("Dictionary<string, object?>? Map;", api, StringComparison.Ordinal);
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
        Assert.Contains("IEnumerable<string?>", Read(typeof(INullableBaseFixture)), StringComparison.Ordinal);
        Assert.Contains("List<string?>", Read(typeof(NullableDerivedFixture)), StringComparison.Ordinal);
    }

    [Fact]
    public void LeavesLegacyMetadataUnannotated()
    {
        var api = Read(typeof(ILegacyApiFixture));
        Assert.DoesNotContain("?", api, StringComparison.Ordinal);
        Assert.Contains("string Echo(string value);", api, StringComparison.Ordinal);
        Assert.Contains("Dictionary<string, object[]> Map", api, StringComparison.Ordinal);
    }

    [Fact]
    public void PreservesNullableTypesOnInitOnlyProperties()
    {
        var api = Read(typeof(NullableInitFixture<>));

        Assert.Contains("string?[]? Names { get; init; }", api, StringComparison.Ordinal);
        Assert.Contains("List<T?>? Items { get; init; }", api, StringComparison.Ordinal);
        Assert.Contains("string?[,]? Grid { get; init; }", api, StringComparison.Ordinal);
        Assert.Contains("T? Value { get; init; }", api, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersNullableValueTypesWithQuestionMarkSyntax()
    {
        var api = Read(typeof(NullableValueFixture<>));

        Assert.DoesNotContain("System.Nullable", api, StringComparison.Ordinal);
        Assert.DoesNotContain("T??", api, StringComparison.Ordinal);
        Assert.Contains("readonly int? Number;", api, StringComparison.Ordinal);
        Assert.Contains("T? Value { get; init; }", api, StringComparison.Ordinal);
        Assert.Contains("int?[]? Numbers { get; init; }", api, StringComparison.Ordinal);
        Assert.Contains("Dictionary<string, KeyValuePair<int, string?>?>? Map", api, StringComparison.Ordinal);
        Assert.Contains("int? Echo(int? value = null);", api, StringComparison.Ordinal);
        Assert.Contains("T? EchoValue(T? value);", api, StringComparison.Ordinal);
        Assert.Contains("TItem? EchoGeneric<TItem>(TItem? value) where TItem : struct;", api, StringComparison.Ordinal);
        Assert.Contains("void Update(ref int? value, in T? input, out T? output);", api, StringComparison.Ordinal);
        Assert.Contains(
            "delegate int? NullableValueCallback(int? value);",
            Read(typeof(NullableValueCallback)),
            StringComparison.Ordinal
        );
    }

    private static string Read(Type type)
    {
        using var stream = File.OpenRead(type.Assembly.Location);
        return Assert.IsType<string>(new NuGetTypeApiReader().ReadTypeApi(stream, type.FullName!, false));
    }
}
