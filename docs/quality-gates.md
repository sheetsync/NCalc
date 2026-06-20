# Quality Gates

This branch adds the repeatable validation gates for the NCalc modernization stack. It is designed to run after the SDK-style core port and the modern test-project migration.

## Passing Standard

No modernization lane passes below a 95/100 review score. A lane also fails immediately if it breaks a compatibility must-pass item, even if the numeric score would otherwise pass.

Required gates:

- The original behavioral tests pass.
- The expanded characterization tests pass.
- A Cobertura coverage report is produced by `dotnet test`.
- The package builds locally with `dotnet pack`.
- The package assemblies are strong-name signed.
- The package does not expose `Antlr3.Runtime` as a NuGet dependency; the ANTLR runtime surface remains embedded in `NCalc.dll`.
- Public API changes require explicit approval and a written compatibility note.
- Framework compatibility tests pass against both the `net35` asset and the `netstandard2.0` asset.
- Consumer smoke tests pass from a local package feed for modern .NET.
- Windows must run a .NET Framework consumer smoke before any public release that claims Framework compatibility.

## Strong-Name Key Policy

The `NCalc` `1.3.8` package was signed with public key token `973cde3f1cafed03`. The private key for that token is not available, so the modernization release is a major-version package identity break even when source and runtime behavior are preserved.

The new major-version key generated for this release line has public key token `f59bbff0fd43f598`. Private key material must never be committed. The repository ignores `.snk`, `.pfx`, `.p12`, and `.pem` files, and `scripts/validate.sh` expects one of these signing paths:

- local production-token validation: the ignored private key for token `f59bbff0fd43f598` at `build/NCalc.snk`, or `NCalcStrongNameKeyFile=/secure/path/NCalc.snk`;
- local throwaway validation: any ignored `.snk`, plus `NCALC_EXPECTED_PUBLIC_KEY_TOKEN` set to that key's token;
- release CI: a repository secret named `NCALC_STRONG_NAME_KEY_BASE64`, containing the base64-encoded private `.snk`.

Because the public key token changes, already-compiled .NET Framework consumers that reference `NCalc, PublicKeyToken=973cde3f1cafed03` must rebuild against the major-version package. Binding redirects cannot redirect one strong-name public key token to another. The package compatibility gate records this deliberately as a major-version identity break rather than pretending binary compatibility exists.

## Local Validation

After stacking on the core port and modern test project, run:

```sh
scripts/validate.sh
```

The script validates the production package and test project directly:

1. `dotnet restore Evaluant.Calculator/NCalc.csproj`
2. `dotnet restore Evaluant.Calculator.Tests/NCalc.Tests.csproj`
3. `dotnet restore Evaluant.Calculator.FrameworkCompatibility.Tests/NCalc.FrameworkCompatibility.Tests.csproj`
4. `dotnet build Evaluant.Calculator/NCalc.csproj --configuration Release --no-restore`
5. `dotnet build Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-restore`
6. `dotnet build Evaluant.Calculator.FrameworkCompatibility.Tests/NCalc.FrameworkCompatibility.Tests.csproj --configuration Release --no-restore`
7. Compatibility assertions against the `netstandard2.0` asset using `dotnet run`
8. Compatibility assertions against the `net35` asset using `mono` on Unix-like systems or the `.exe` directly on Windows
9. `dotnet test Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-build --collect:"XPlat Code Coverage"`
10. `dotnet pack Evaluant.Calculator/NCalc.csproj --configuration Release --no-build`
11. package compatibility validation for signing, embedded ANTLR, parser source compatibility, and the deliberate major-version identity break
12. SDK package validation with `EnablePackageValidation=true` after hydrating the `NCalc` `1.3.8` baseline package
13. A modern .NET consumer smoke from the packed local package

Coverage output is written to `artifacts/coverage` and must include `coverage.cobertura.xml`.

The Framework compatibility harness intentionally uses a plain console runner rather than MSTest so the same assertions can execute on the `net35` build under Mono and the `netstandard2.0` build under modern .NET. It focuses on culture-sensitive parsing, date and Unicode parsing, decimal/float conversion, unsigned arithmetic quirks, exact legacy exception messages, lazy callbacks, and manual AST evaluation.

Local validation packs `NCalc` as `2.0.0-qualitygates` by default. This avoids NuGet reusing a globally cached `NCalc` `1.3.8` baseline package during consumer smoke tests and reflects the major-version package identity change. Override with `PACKAGE_VERSION=...` only when the replacement version is deliberate.

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
  /p:PackageVersion=2.0.0-qualitygates \
  /p:EnablePackageValidation=true \
  /p:PackageValidationBaselineVersion=1.3.8 \
  /p:NoWarn=PKV006
```

`PKV006` is suppressed because the legacy `NCalc` `1.3.8` package places `NCalc.dll` directly under `lib/`, which current SDK package validation interprets as `.NETFramework,Version=v0.0`. The warning suppression is only for that unsupported legacy TFM shape; API breaking-change diagnostics must remain hard failures.

If the public generated ANTLR parser surface makes package validation too noisy, the failure must be reviewed rather than ignored. Approved changes should be documented with:

- the old public signature,
- the new public signature,
- why compatibility is still acceptable or why a breaking-change version is required.

## Package Compatibility

SDK package validation checks public API compatibility, but it does not prove all package identity facts that matter for this modernization. The additional package gate is:

```sh
scripts/package-compatibility.sh artifacts/packages 2.0.0-qualitygates
```

It verifies:

- the baseline `NCalc` `1.3.8` package has token `973cde3f1cafed03`;
- the current package assemblies are strong-name signed with the configured expected token;
- the current package assemblies use assembly version `2.0.0.0`;
- `Antlr3.Runtime` is not a package dependency;
- the `net35` and `netstandard2.0` assemblies embed public `Antlr.Runtime.*`, `NCalcLexer`, `NCalcParser`, and parser return-scope types;
- a parser/lexer consumer can compile and run against the packed `net35` assembly;
- a consumer compiled against the legacy package references the old token, documenting why a new key is a major-version binary identity break.

## Consumer Smoke

Modern .NET smoke is automated:

```sh
scripts/consumer-smoke.sh artifacts/packages 2.0.0-qualitygates
```

Windows .NET Framework smoke is required before publishing. Minimum release checklist:

1. Pack the branch locally.
2. Create a .NET Framework consumer project on Windows.
3. Restore `NCalc` from the local package feed.
4. Build and run an expression smoke such as `2 * (3 + 5) == 16`.

This Windows gate is intentionally documented rather than scripted here because this macOS worktree cannot run a .NET Framework executable.
