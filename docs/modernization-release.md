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
dotnet restore NCalc.sln
dotnet build NCalc.sln --configuration Release --no-restore
dotnet test NCalc.sln --configuration Release --no-build
dotnet pack Evaluant.Calculator/NCalc.csproj --configuration Release --no-build --output artifacts/packages
```

The current legacy project layout still targets .NET Framework 3.5, so these commands depend on the earlier SDK-style conversion PRs.

## Package Identity

The shared package metadata preserves the existing NuGet identity as `NCalc` and points package consumers back to `https://github.com/sheetsync/NCalc`.

No license metadata is declared here because this checkout does not contain a license file or existing license declaration. Add an explicit repository license before publishing a public package version.

## GitHub Release Flow

The release workflow builds, tests, packs, uploads the `.nupkg` artifact, and publishes to NuGet.org when either:

- a GitHub Release is published; or
- the manual workflow is run with `publish` enabled.

Publishing requires a repository secret named `NUGET_API_KEY`.
