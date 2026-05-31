using System.Xml.Linq;
using Xunit;

namespace Raiqub.Mcp.NuScope.Tests;

public sealed class SkeletonTests
{
    [Fact]
    public void Server_project_is_configured_as_nuscope_mcp_tool()
    {
        var project = XDocument.Parse(ReadContractFile("NuScope.Server.csproj"));
        var propertyGroup = Assert.Single(project.Root!.Elements("PropertyGroup"));
        var packageReference = Assert.Single(
            project.Root.Elements("ItemGroup")
                .Elements("PackageReference"),
            reference => (string?)reference.Attribute("Include") == "ModelContextProtocol");

        Assert.Equal("Raiqub.Mcp.NuScope", propertyGroup.Element("RootNamespace")?.Value);
        Assert.Equal("true", propertyGroup.Element("PackAsTool")?.Value);
        Assert.Equal("nuscope", propertyGroup.Element("ToolCommandName")?.Value);
        Assert.Equal("ModelContextProtocol", (string?)packageReference.Attribute("Include"));
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
