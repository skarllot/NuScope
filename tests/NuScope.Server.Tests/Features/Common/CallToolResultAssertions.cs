using System.Text.Json;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common;

internal static class CallToolResultAssertions
{
    public static T DeserializeSuccessfulContent<T>(CallToolResult result)
    {
        Assert.True(result.IsError != true);
        Assert.Null(result.StructuredContent);
        var content = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        return Assert.IsType<T>(JsonSerializer.Deserialize<T>(content.Text));
    }
}
