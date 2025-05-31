# Agents Instructions

The CI workflow uses static checks that do not require Resonite assemblies.

- Formatting is enforced with `csharpier`.
- Before committing, run `dotnet csharpier check .` to verify formatting.
- Use `dotnet csharpier format .` to apply formatting fixes.
