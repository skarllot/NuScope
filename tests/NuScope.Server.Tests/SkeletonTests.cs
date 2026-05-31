using Xunit;

namespace Raiqub.Mcp.NuScope.Tests;

public sealed class SkeletonTests
{
    [Fact]
    public void Server_project_is_configured_as_nuscope_mcp_tool()
    {
        var project = ReadContractFile("NuScope.Server.csproj");

        Assert.Contains("<RootNamespace>Raiqub.Mcp.NuScope</RootNamespace>", project);
        Assert.Contains("<PackAsTool>true</PackAsTool>", project);
        Assert.Contains("<ToolCommandName>nuscope</ToolCommandName>", project);
        Assert.Contains("""<PackageReference Include="ModelContextProtocol" />""", project);
    }

    [Fact]
    public void Server_uses_stdio_mcp_transport_without_project_tools()
    {
        var program = ReadContractFile("Program.cs");

        Assert.Contains(".AddMcpServer()", program);
        Assert.Contains(".WithStdioServerTransport()", program);
        Assert.DoesNotContain("McpServerTool", program);
        Assert.DoesNotContain("WithTools", program);
    }

    private static string ReadContractFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ContractFiles", fileName);
        return File.ReadAllText(path);
    }
}
