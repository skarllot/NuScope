# Contributing to NuScope

Thank you for contributing to NuScope.

## Before you open a pull request

1. Check for an existing issue or open one to describe the proposed change.
2. Keep changes focused and small enough to review safely.
3. Add or update tests when behavior changes.
4. Run the local validation flow:

   ```powershell
   dotnet tool restore
   dotnet restore --locked-mode
   dotnet csharpier check .
   dotnet build --configuration Release --no-restore
   dotnet test --configuration Release --no-build --no-restore
   ```

## Pull request expectations

- Target the default branch unless maintainers ask otherwise.
- Keep warnings at zero; the repository treats warnings as errors.
- Follow the repository formatting and analyzer rules.
- Update documentation when behavior or developer workflow changes.

## Development standards

- Versioning is managed with Nerdbank.GitVersioning through `version.json`.
- .NET analyzers are enabled for the repository.
- CSharpier is the required formatter for source-controlled code.

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md).
