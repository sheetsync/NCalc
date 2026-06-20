# Quality Gates

This branch adds the repeatable validation gates for the NCalc modernization stack. It is designed to run after the SDK-style core port and the modern test-project migration.

## Passing Standard

No modernization lane passes below a 95/100 review score. A lane also fails immediately if it breaks a compatibility must-pass item, even if the numeric score would otherwise pass.

Required gates:

- The original behavioral tests pass.
- The expanded characterization tests pass.
- A Cobertura coverage report is produced by `dotnet test`.
- The package builds locally with `dotnet pack`.
- Public API changes require explicit approval and a written compatibility note.
- Consumer smoke tests pass from a local package feed for modern .NET.
- Windows must run a .NET Framework consumer smoke before any public release that claims Framework compatibility.

## Local Validation

After stacking on the core port and modern test project, run:

```sh
scripts/validate.sh
```

The script validates the production package and test project directly:

1. `dotnet restore Evaluant.Calculator/NCalc.csproj`
2. `dotnet restore Evaluant.Calculator.Tests/NCalc.Tests.csproj`
3. `dotnet build Evaluant.Calculator/NCalc.csproj --configuration Release --no-restore`
4. `dotnet build Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-restore`
5. `dotnet test Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-build --collect:"XPlat Code Coverage"`
6. `dotnet pack Evaluant.Calculator/NCalc.csproj --configuration Release --no-build`
7. SDK package validation with `EnablePackageValidation=true` after hydrating the `NCalc` `1.3.8` baseline package
8. A modern .NET consumer smoke from the packed local package

Coverage output is written to `artifacts/coverage` and must include `coverage.cobertura.xml`.

Local validation packs `NCalc` as `1.3.8-qualitygates` by default. This avoids NuGet reusing a globally cached `NCalc` `1.3.8` baseline package during consumer smoke tests. Override with `PACKAGE_VERSION=...` only when the replacement version is deliberate.

The full legacy `NCalc.sln` still contains `Evaluant.Calculator.Play`, a .NET Framework 3.5 sample project. Solution-wide validation should be re-enabled after that sample project is either modernized, removed from the solution, or gated to Windows/Mono explicitly.

## API Compatibility

The validation script first hydrates the NuGet global-packages cache with the approved baseline package, then uses SDK package validation:

```sh
baseline_root="$(mktemp -d "${TMPDIR:-/tmp}/ncalc-api-baseline.XXXXXX")"
cat >"${baseline_root}/NCalc.ApiCompatBaseline.csproj" <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NCalc" Version="1.3.8" />
  </ItemGroup>
</Project>
XML

dotnet restore "${baseline_root}/NCalc.ApiCompatBaseline.csproj" \
  --source https://api.nuget.org/v3/index.json

dotnet pack Evaluant.Calculator/NCalc.csproj \
  --configuration Release \
  --no-build \
  --output artifacts/packages/api-compat \
  /p:ContinuousIntegrationBuild=true \
  /p:PackageVersion=1.3.8-qualitygates \
  /p:EnablePackageValidation=true \
  /p:PackageValidationBaselineVersion=1.3.8 \
  /p:NoWarn=PKV006
```

`PKV006` is suppressed because the legacy `NCalc` `1.3.8` package places `NCalc.dll` directly under `lib/`, which current SDK package validation interprets as `.NETFramework,Version=v0.0`. The warning suppression is only for that unsupported legacy TFM shape; API breaking-change diagnostics must remain hard failures.

If the public generated ANTLR parser surface makes package validation too noisy, the failure must be reviewed rather than ignored. Approved changes should be documented with:

- the old public signature,
- the new public signature,
- why compatibility is still acceptable or why a breaking-change version is required.

## Consumer Smoke

Modern .NET smoke is automated:

```sh
scripts/consumer-smoke.sh artifacts/packages 1.3.8-qualitygates
```

Windows .NET Framework smoke is required before publishing. Minimum release checklist:

1. Pack the branch locally.
2. Create a .NET Framework consumer project on Windows.
3. Restore `NCalc` from the local package feed.
4. Build and run an expression smoke such as `2 * (3 + 5) == 16`.

This Windows gate is intentionally documented rather than scripted here because this macOS worktree cannot run a .NET Framework executable.
