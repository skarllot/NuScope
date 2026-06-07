using System.Globalization;
using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var solutionDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
const int clientPort = 6284;
const int proxyPort = 6287;
var proxyToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
var proxyAddress = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{proxyPort}");
var inspectorUrl = string.Create(
    CultureInfo.InvariantCulture,
    $"/?MCP_PROXY_PORT={proxyPort}&MCP_PROXY_AUTH_TOKEN={proxyToken}"
);
var allowedOrigins = string.Create(
    CultureInfo.InvariantCulture,
    $"http://localhost:{clientPort},http://127.0.0.1:{clientPort}"
);

builder
    .AddExecutable(
        "mcp-inspector",
        "npx",
        solutionDirectory,
        "-y",
        "@modelcontextprotocol/inspector",
        "dotnet",
        "run",
        "--project",
        "src/NuScope.Server/NuScope.Server.csproj"
    )
    .WithHttpEndpoint(port: clientPort, targetPort: clientPort, name: "client", env: "CLIENT_PORT", isProxied: false)
    .WithHttpEndpoint(port: proxyPort, targetPort: proxyPort, name: "proxy", env: "SERVER_PORT", isProxied: false)
    .WithEnvironment("HOST", "127.0.0.1")
    .WithEnvironment("MCP_AUTO_OPEN_ENABLED", "false")
    .WithEnvironment("MCP_PROXY_AUTH_TOKEN", proxyToken)
    .WithEnvironment("MCP_PROXY_FULL_ADDRESS", proxyAddress)
    .WithEnvironment("ALLOWED_ORIGINS", allowedOrigins)
    .WithUrlForEndpoint("client", url => url.Url = inspectorUrl);

builder.Build().Run();
