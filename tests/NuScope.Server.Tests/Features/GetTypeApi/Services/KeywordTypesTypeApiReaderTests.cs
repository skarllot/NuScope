using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class KeywordTypesTypeApiReaderTests
{
    [Fact]
    public void UsesEveryAvailableKeywordInSerializedAttributeTypes()
    {
        using var stream = File.OpenRead(typeof(KeywordTypesApiFixture).Assembly.Location);
        var api = new NuGetTypeApiReader().ReadTypeApi(stream, typeof(KeywordTypesApiFixture).FullName!, false);

        Assert.NotNull(api);
        foreach (
            var keyword in new[]
            {
                "bool",
                "byte",
                "sbyte",
                "char",
                "decimal",
                "double",
                "float",
                "short",
                "ushort",
                "int",
                "uint",
                "long",
                "ulong",
                "object",
                "string",
                "void",
                "nint",
                "nuint",
            }
        )
        {
            Assert.Contains("typeof(" + keyword + ")", api, StringComparison.Ordinal);
        }

        Assert.Contains("typeof(Dictionary<string, int[]>)", api, StringComparison.Ordinal);
        Assert.Contains("public abstract decimal? Value", api, StringComparison.Ordinal);
        Assert.Contains("Text = \"System.Int32 and System.String\"", api, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(Int32)", api, StringComparison.Ordinal);
        Assert.DoesNotContain("typeof(String)", api, StringComparison.Ordinal);
    }
}
