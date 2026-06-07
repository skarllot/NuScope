# Repository Notes

## Scope and shape
- .NET SDK is pinned to `10.0.300` in `global.json`; `dnx` usage in docs assumes .NET 10.
- The solution has only two projects: `src/NuScope.Server` and `tests/NuScope.Server.Tests`.
- `src/NuScope.Server` is a stdio MCP server packaged as NuGet tool/package `Raiqub.NuSpec`; tool command is `nuscope`.
- Real runtime wiring is in `src/NuScope.Server/Program.cs`: generic host, console logs to stderr, stdio
  MCP transport, tools from `NuGetPackageMetadataTools`.

## Commands
- First-time setup: `dotnet tool restore` then `dotnet restore --locked-mode`.
- CI-equivalent local check order: `dotnet csharpier check .` ->
  `dotnet build --configuration Release --no-restore` ->
  `dotnet test --configuration Release --no-build --no-restore`.
- Coverage command used by CI: `dotnet test --configuration Release --no-build --no-restore --verbosity normal /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`.
- Run the MCP server locally after building: `dotnet run --project src/NuScope.Server/NuScope.Server.csproj --no-build`.

## Build and packaging gotchas
- Restore lock files are committed; prefer `dotnet restore --locked-mode` and do not casually update `packages.lock.json`.
- Warnings are errors, NET analyzers are enabled, and NuGet audit is configured for low+all in `Directory.Build.props`.
- Package output goes to `artifacts/`; `bin/`, `obj/`, `artifacts/`, and coverage outputs are generated.
- Release packaging uses Nerdbank.GitVersioning and NuGet OIDC in GitHub Actions; avoid adding long-lived
  NuGet API key flows.

## GitHub workflow
- Use Conventional Commits for commit messages and pull request titles.
- Write pull request descriptions in Markdown.

## Style and tests
- Formatting is CSharpier via the restored local tool; CI runs `dotnet csharpier check .`.
- `.editorconfig` uses 4-space C# indentation, 2-space JSON/YAML/XML/MSBuild indentation, final newline,
  trimmed trailing whitespace, max line length 120.
- Tests are xUnit v3 with `System.IO.Abstractions.TestingHelpers.MockFileSystem`; keep filesystem-dependent
  behavior mockable through abstractions.
- Existing tests encode package-version selection edge cases (stable vs prerelease ordering, malformed
  versions/nuspecs, duplicate nuspecs); update them when changing metadata resolution.
