# Modernization Release Notes

This branch is the release-infrastructure lane for the NCalc modernization stack. It is intended to land after the SDK-style .NET Standard 2.0 project conversion and the modern test-project conversion.

## Stack Order

1. Port the core library project to SDK style and `netstandard2.0`.
2. Port the test project to a modern SDK-style test runner.
3. Expand characterization coverage while preserving existing behavior.
4. Add numeric edge-case and API-surface coverage.
5. Add this build, package, and release infrastructure.

## Local Validation

After the earlier modernization PRs are applied, maintainers should be able to run:

```sh
dotnet restore Evaluant.Calculator/NCalc.csproj
dotnet restore Evaluant.Calculator.Tests/NCalc.Tests.csproj
dotnet restore Evaluant.Calculator.FrameworkCompatibility.Tests/NCalc.FrameworkCompatibility.Tests.csproj
dotnet build Evaluant.Calculator/NCalc.csproj --configuration Release --no-restore
dotnet build Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-restore
dotnet build Evaluant.Calculator.FrameworkCompatibility.Tests/NCalc.FrameworkCompatibility.Tests.csproj --configuration Release --no-restore
dotnet run --project Evaluant.Calculator.FrameworkCompatibility.Tests/NCalc.FrameworkCompatibility.Tests.csproj --configuration Release --framework net10.0 --no-build
mono Evaluant.Calculator.FrameworkCompatibility.Tests/bin/Release/net35/NCalc.FrameworkCompatibility.Tests.exe
dotnet test Evaluant.Calculator.Tests/NCalc.Tests.csproj --configuration Release --no-build
dotnet pack Evaluant.Calculator/NCalc.csproj --configuration Release --no-build --output artifacts/packages /p:ContinuousIntegrationBuild=true /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg
```

The pack command should emit both `.nupkg` and `.snupkg` files after the SDK-style project conversion. The current legacy project layout still targets .NET Framework 3.5, so these commands depend on the earlier SDK-style conversion PRs.

The GitHub CI workflow runs restore, build, and test on Ubuntu, macOS, and Windows. A dedicated Ubuntu quality-gates job runs `scripts/validate.sh`, including signed package creation, package compatibility, API compatibility, coverage, local-feed consumer smoke, and the compatibility harness against both `net35` under Mono and `netstandard2.0` under modern .NET. CI validates the library and modern test project directly because the full solution still contains the legacy `Evaluant.Calculator.Play` .NET Framework 3.5 sample project. Windows remains part of the required gate because the modernization must preserve compatibility expectations for existing .NET Framework consumers.

## Release Quality Gate

Release infrastructure changes should not be treated as passing until the branch scores at least 95/100 in coordinator or reviewer assessment. The expected evidence is:

- clean repository state after validation;
- successful workflow YAML parsing or equivalent linting;
- successful local validation for commands available on the current stack;
- explicit documentation of any stack dependency that prevents full validation;
- no runtime-source changes in this release-infrastructure lane.

## Package Identity

The shared package metadata preserves the existing NuGet identity as `NCalc` and points package consumers back to `https://github.com/sheetsync/NCalc`.

This lane owns shared release metadata in `Directory.Build.props` so the SDK-style project conversion can stay focused on target frameworks and dependencies. The metadata is conditioned to the `NCalc` project and SDK-only package references are guarded with `$(UsingMicrosoftNETSdk)` so the current legacy project files are not forced into PackageReference behavior.

Packages are configured for deterministic builds, Source Link, repository metadata, and `.snupkg` symbol output once the core project is SDK-style.

No license metadata is declared here because this checkout does not contain a license file or existing license declaration. Add an explicit repository license before publishing a public package version.

## Strong-Name Signing

The old `NCalc` `1.3.8` strong-name private key is unavailable. Its public key token was `973cde3f1cafed03`, so a newly signed modernization package cannot be binary-compatible for already-compiled strong-name consumers. This release line is therefore `2.0.0` or later, uses assembly version `2.0.0.0`, and uses the new public key token `f59bbff0fd43f598`.

Do not commit private signing keys. Local production-token validation should use the ignored private key for token `f59bbff0fd43f598`:

```sh
scripts/validate.sh
```

If the production key is stored elsewhere, set:

```sh
NCalcStrongNameKeyFile=/secure/path/NCalc.snk scripts/validate.sh
```

For temporary non-release validation with a throwaway key, generate an ignored key and set `NCALC_EXPECTED_PUBLIC_KEY_TOKEN` to the generated token. Do not publish a package from a throwaway key.

GitHub release packaging requires `NCALC_STRONG_NAME_KEY_BASE64`, a base64-encoded `.snk` stored as a repository secret. The release workflow decodes that secret to the runner temp directory and fails if the public key token does not match `f59bbff0fd43f598`.

For non-release CI, the quality-gates workflow can generate an ephemeral key when the secret is unavailable. That still proves the package is signable, repacked, and source-compatible, while the release workflow remains the authority for the stable production token.

## Deferred Package Validation And Restore Locking

Package validation settings are intentionally not enabled in this lane yet. They should be added after the SDK-style project exists and a real package baseline/versioning policy is chosen; enabling package validation without a baseline here would either be untestable on this branch or risk noisy validation with no compatibility signal.

Restore lock files are also intentionally deferred. Generate and review `packages.lock.json` after PR1 and PR2 have finalized the SDK-style project references and test dependencies; do not check in a hand-written lock file.

Solution-wide validation should be restored after the sample application is modernized, removed from the solution, or moved behind an explicit Windows/Mono-only gate.

## GitHub Release Flow

The release workflow builds, tests, packs, uploads the `.nupkg` and `.snupkg` artifacts, and publishes them to NuGet.org when either:

- a GitHub Release is published; or
- the manual workflow is run with `publish` enabled.

Publishing requires repository secrets named `NUGET_API_KEY` and `NCALC_STRONG_NAME_KEY_BASE64`.
