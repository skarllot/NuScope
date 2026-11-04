using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class ParamsTypeApiReaderTests
{
    private const string FixtureNamespace = "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.";
    private const string Mark = FixtureNamespace + "ApiMarkAttribute";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RendersArraysCollectionsAndIndexers(bool includePrivate)
    {
        var api = Read(typeof(ParamsApiFixture), includePrivate);

        Assert.Contains(
            "protected ParamsApiFixture([" + Mark + "(\"params-constructor\")] params string?[] values);",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "void Array(int first, [" + Mark + "(\"params-array\")] params int[] values);",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "void Collection<T>([" + Mark + "(\"params-collection\")] params IEnumerable<T> values);",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains("params ReadOnlySpan<int> values);", api, StringComparison.Ordinal);
        Assert.Contains("void Ordinary(int[] values);", api, StringComparison.Ordinal);
        Assert.Contains("this[[" + Mark + "(\"params-indexer\")] params string[] arg0]", api, StringComparison.Ordinal);
        Assert.Contains(
            "void Lookalike(["
                + FixtureNamespace
                + "ParamArrayAttribute] ["
                + FixtureNamespace
                + "ParamCollectionAttribute] int[] values);",
            api,
            StringComparison.Ordinal
        );
        Assert.Equal(includePrivate, api.Contains("void Hidden(params int[] values);", StringComparison.Ordinal));
        Assert.DoesNotContain("[ParamArrayAttribute]", api, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "System.Runtime.CompilerServices.ParamCollectionAttribute",
            api,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RendersDelegateAndExtensionParameters()
    {
        Assert.Contains(
            "delegate void ParamsArrayCallback(params string?[] values);",
            Read(typeof(ParamsArrayCallback)),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "delegate void ParamsCollectionCallback<T>(params IEnumerable<T> values);",
            Read(typeof(ParamsCollectionCallback<>)),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "void Combine(this string value, params string[] values);",
            Read(typeof(ParamsExtensionFixture)),
            StringComparison.Ordinal
        );
    }

    private static string Read(Type type, bool includePrivate = false)
    {
        using var stream = File.OpenRead(type.Assembly.Location);
        return Assert.IsType<string>(new NuGetTypeApiReader().ReadTypeApi(stream, type.FullName!, includePrivate));
    }
}
