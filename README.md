# NuScope

[![Build status](https://github.com/skarllot/NuScope/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/skarllot/NuScope/actions)
[![codecov](https://codecov.io/gh/skarllot/NuScope/branch/main/graph/badge.svg)](https://codecov.io/gh/skarllot/NuScope)
[![GitHub license](https://img.shields.io/badge/license-Apache%202.0-blue.svg?style=flat-square)](LICENSE)
[![Nuget](https://img.shields.io/nuget/v/Raiqub.NuSpec)](https://www.nuget.org/packages/Raiqub.NuSpec)
[![Nuget](https://img.shields.io/nuget/dt/Raiqub.NuSpec?label=Nuget.org%20Downloads&style=flat-square&color=blue)](https://www.nuget.org/packages/Raiqub.NuSpec)

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
