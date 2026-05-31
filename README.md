# NuScope
MCP server for exploring NuGet packages, metadata, and source code.

## Development

```powershell
dotnet restore --locked-mode
dotnet build --no-restore
dotnet test --no-build --no-restore
```

Run the skeleton MCP server over stdio:

```powershell
dotnet run --project src/NuScope.Server/NuScope.Server.csproj --no-build
```
