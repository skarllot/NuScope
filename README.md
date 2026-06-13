# NuScope

[![Build status](https://github.com/skarllot/NuScope/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/skarllot/NuScope/actions)
[![codecov](https://codecov.io/gh/skarllot/NuScope/branch/main/graph/badge.svg)](https://codecov.io/gh/skarllot/NuScope)
[![GitHub license](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](LICENSE)
[![Nuget](https://img.shields.io/nuget/v/Raiqub.NuScope)](https://www.nuget.org/packages/Raiqub.NuScope)
[![Nuget](https://img.shields.io/nuget/dt/Raiqub.NuScope?label=Nuget.org%20Downloads&style=flat-square&color=blue)](https://www.nuget.org/packages/Raiqub.NuScope)

MCP server for exploring NuGet packages, metadata, and source code.

## Configure in AI tools

NuScope runs as a stdio MCP server and can be started directly from NuGet with
[`dnx`](https://learn.microsoft.com/dotnet/core/tools/dnx). `dnx` is included
with the .NET 10 SDK.

Use the NuGet package ID (`Raiqub.NuScope`) in the command arguments:

```json
{
  "servers": {
    "nuscope": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Raiqub.NuScope", "--yes"]
    }
  }
}
```

For prerelease versions, add `--prerelease`. To pin a version, use
`Raiqub.NuScope@x.y.z`.

### Claude Desktop

Add NuScope to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "nuscope": {
      "command": "dnx",
      "args": ["Raiqub.NuScope", "--yes"]
    }
  }
}
```

Common config locations:

- Windows: `%APPDATA%\\Claude\\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

### VS Code / GitHub Copilot

Add NuScope to `.vscode/mcp.json`:

```json
{
  "servers": {
    "nuscope": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Raiqub.NuScope", "--yes"]
    }
  }
}
```

### Cursor

Add NuScope to `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "nuscope": {
      "command": "dnx",
      "args": ["Raiqub.NuScope", "--yes"]
    }
  }
}
```

### Cline / Windsurf

Use the standard MCP server configuration:

```json
{
  "mcpServers": {
    "nuscope": {
      "command": "dnx",
      "args": ["Raiqub.NuScope", "--yes"]
    }
  }
}
```

## Development

```powershell
dotnet tool restore
dotnet restore --locked-mode
dotnet csharpier check .
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --no-restore
```

Run the skeleton MCP server over stdio:

```powershell
dotnet run --project src/NuScope.Server/NuScope.Server.csproj --no-build
```

Run the Aspire AppHost to launch MCP Inspector for the local server:

```powershell
dotnet run --project src/NuScope.AppHost/NuScope.AppHost.csproj
```

Open the `mcp-inspector` endpoint from the Aspire dashboard.

NuScope remains a stdio-only MCP server. The AppHost starts MCP Inspector with:

```powershell
npx -y @modelcontextprotocol/inspector dotnet run --project src/NuScope.Server/NuScope.Server.csproj
```

Open the `mcp-inspector` `client` endpoint from the Aspire dashboard, or browse to:

```text
http://localhost:6284/?MCP_PROXY_PORT=6287&MCP_PROXY_AUTH_TOKEN=<token>
```

The AppHost uses ports `6284` and `6287` to avoid stale MCP Inspector browser storage from the default
`localhost:6274` origin. The Aspire dashboard `client` link includes a per-run proxy authentication token.
