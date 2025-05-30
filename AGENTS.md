# Agents Instructions

The CI workflow uses static checks that do not require Resonite assemblies.

- Formatting is enforced with `csharpier`.
- Run `dotnet tool restore` once per session if `dotnet dotnet-csharpier` is unavailable.
- Before committing, run `dotnet dotnet-csharpier --check .` to verify formatting.
- Use `dotnet dotnet-csharpier .` to apply formatting fixes.
- TODO: Explore additional static checks such as `dotnet format` with stub assemblies.
