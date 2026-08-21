using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Raiqub.NuScope.Features.Common.Models;

public static class NuGetToolResultExtensions
{
    public static CallToolResult ToCallToolResult(this NuGetToolResult result)
    {
        if (result is NuGetProblemDetailsResult problem)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = problem.Detail }],
                StructuredContent = JsonSerializer.SerializeToElement(problem),
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result, result.GetType()) }],
        };
    }
}
