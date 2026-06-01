# NuScope

[![Build status](https://github.com/skarllot/NuScope/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/skarllot/NuScope/actions)
[![codecov](https://codecov.io/gh/skarllot/NuScope/branch/main/graph/badge.svg)](https://codecov.io/gh/skarllot/NuScope)
[![GitHub license](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](LICENSE)
[![Nuget](https://img.shields.io/nuget/v/Raiqub.NuSpec)](https://www.nuget.org/packages/Raiqub.NuSpec)
[![Nuget](https://img.shields.io/nuget/dt/Raiqub.NuSpec?label=Nuget.org%20Downloads&style=flat-square&color=blue)](https://www.nuget.org/packages/Raiqub.NuSpec)

MCP server for exploring NuGet packages, metadata, and source code.

## Configure in AI tools

NuScope runs as a stdio MCP server and can be started directly from NuGet with
[`dnx`](https://learn.microsoft.com/dotnet/core/tools/dnx). `dnx` is included
with the .NET 10 SDK.

Use the NuGet package ID (`Raiqub.NuSpec`) in the command arguments:

```json
{
  "servers": {
    "nuscope": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Raiqub.NuSpec", "--yes"]
    }
  }
}
```

For prerelease versions, add `--prerelease`. To pin a version, use
`Raiqub.NuSpec@x.y.z`.

### Claude Desktop

Add NuScope to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "nuscope": {
      "command": "dnx",
      "args": ["Raiqub.NuSpec", "--yes"]
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
      "args": ["Raiqub.NuSpec", "--yes"]
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
      "args": ["Raiqub.NuSpec", "--yes"]
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
      "args": ["Raiqub.NuSpec", "--yes"]
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
