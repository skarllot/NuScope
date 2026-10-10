using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class ExtensionTypeApiReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RendersExtensionReceiversAndPreservesParameterModifiers(bool includePrivate)
    {
        using var stream = File.OpenRead(typeof(ExtensionApiFixture).Assembly.Location);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, typeof(ExtensionApiFixture).FullName!, includePrivate);

        Assert.Contains(
            "public static string? Echo(this string? value, string suffix = \"\");",
            api,
            StringComparison.Ordinal
        );
        Assert.Contains("public static int CountItems<T>(this IEnumerable<T> values);", api, StringComparison.Ordinal);
        Assert.Contains("public static void Increment(this ref int value);", api, StringComparison.Ordinal);
        Assert.Contains("public static int Read(this in int value);", api, StringComparison.Ordinal);
        Assert.Contains("public static int Ordinary(string value);", api, StringComparison.Ordinal);
        Assert.DoesNotContain("this string suffix", api, StringComparison.Ordinal);
        Assert.Equal(
            includePrivate,
            api!.Contains("internal static int Hidden(this string value);", StringComparison.Ordinal)
        );
    }
}
