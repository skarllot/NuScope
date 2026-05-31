# NuScope
MCP server for exploring NuGet packages, metadata, and source code.

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
