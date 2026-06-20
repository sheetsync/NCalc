#!/usr/bin/env bash

set -euo pipefail

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly CONFIGURATION="${CONFIGURATION:-Release}"
readonly COVERAGE_RESULTS_DIR="${COVERAGE_RESULTS_DIR:-${REPOSITORY_ROOT}/artifacts/coverage}"
readonly PACKAGE_OUTPUT_DIR="${PACKAGE_OUTPUT_DIR:-${REPOSITORY_ROOT}/artifacts/packages}"
readonly PACKAGE_VERSION="${PACKAGE_VERSION:-1.3.8-qualitygates}"
readonly API_COMPAT_BASELINE_VERSION="${API_COMPAT_BASELINE_VERSION:-1.3.8}"
readonly NUGET_PACKAGE_SOURCE="${NUGET_PACKAGE_SOURCE:-https://api.nuget.org/v3/index.json}"
readonly CORE_PROJECT="${CORE_PROJECT:-Evaluant.Calculator/NCalc.csproj}"
readonly TEST_PROJECT="${TEST_PROJECT:-Evaluant.Calculator.Tests/NCalc.Tests.csproj}"

main() {
  cd "${REPOSITORY_ROOT}"

  require_command dotnet
  require_sdk_style_core_project

  restore_projects
  build_projects
  run_tests_with_coverage
  pack_package
  run_api_compatibility_check
  run_modern_consumer_smoke

  print_section "Validation complete"
  echo "Quality gates passed locally."
}

require_command() {
  local command_name="$1"

  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "Required command not found: ${command_name}" >&2
    exit 127
  fi
}

require_sdk_style_core_project() {
  print_section "Checking stack dependencies"

  if ! grep -q '<Project Sdk=' Evaluant.Calculator/NCalc.csproj; then
    cat >&2 <<'MESSAGE'
This quality-gates branch must be stacked after the SDK-style core port.
Expected Evaluant.Calculator/NCalc.csproj to be an SDK-style project.
Apply/rebase onto codex/ncalc-netstandard-core before running this script.
MESSAGE
    exit 2
  fi

  echo "SDK-style core project detected."
}

restore_projects() {
  print_section "Restoring projects"
  dotnet restore "${CORE_PROJECT}"
  dotnet restore "${TEST_PROJECT}"
}

build_projects() {
  print_section "Building projects"
  dotnet build "${CORE_PROJECT}" --configuration "${CONFIGURATION}" --no-restore
  dotnet build "${TEST_PROJECT}" --configuration "${CONFIGURATION}" --no-restore
}

run_tests_with_coverage() {
  print_section "Running tests with coverage"
  rm -rf "${COVERAGE_RESULTS_DIR}"
  mkdir -p "${COVERAGE_RESULTS_DIR}"

  dotnet test "${TEST_PROJECT}" \
    --configuration "${CONFIGURATION}" \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory "${COVERAGE_RESULTS_DIR}" \
    --logger "trx;LogFileName=test-results.trx" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

  find "${COVERAGE_RESULTS_DIR}" -name 'coverage.cobertura.xml' -print -quit | grep -q .
  echo "Coverage report produced under ${COVERAGE_RESULTS_DIR}."
}

pack_package() {
  print_section "Packing package"
  rm -rf "${PACKAGE_OUTPUT_DIR}"
  mkdir -p "${PACKAGE_OUTPUT_DIR}"

  dotnet pack "${CORE_PROJECT}" \
    --configuration "${CONFIGURATION}" \
    --no-build \
    --output "${PACKAGE_OUTPUT_DIR}" \
    /p:ContinuousIntegrationBuild=true \
    /p:PackageVersion="${PACKAGE_VERSION}" \
    /p:IncludeSymbols=true \
    /p:SymbolPackageFormat=snupkg

  find "${PACKAGE_OUTPUT_DIR}" -name '*.nupkg' -print -quit | grep -q .
  echo "Package produced under ${PACKAGE_OUTPUT_DIR}."
}

run_api_compatibility_check() {
  print_section "Running API compatibility/package validation"
  hydrate_api_compatibility_baseline

  dotnet pack "${CORE_PROJECT}" \
    --configuration "${CONFIGURATION}" \
    --no-build \
    --output "${PACKAGE_OUTPUT_DIR}/api-compat" \
    /p:ContinuousIntegrationBuild=true \
    /p:PackageVersion="${PACKAGE_VERSION}" \
    /p:EnablePackageValidation=true \
    /p:PackageValidationBaselineVersion="${API_COMPAT_BASELINE_VERSION}" \
    /p:NoWarn=PKV006
}

hydrate_api_compatibility_baseline() {
  local baseline_restore_root

  baseline_restore_root="$(mktemp -d "${TMPDIR:-/tmp}/ncalc-api-baseline.XXXXXX")"

  (
    trap 'rm -rf "${baseline_restore_root}"' EXIT

    cat >"${baseline_restore_root}/NCalc.ApiCompatBaseline.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NCalc" Version="${API_COMPAT_BASELINE_VERSION}" />
  </ItemGroup>
</Project>
XML

    dotnet restore "${baseline_restore_root}/NCalc.ApiCompatBaseline.csproj" \
      --source "${NUGET_PACKAGE_SOURCE}" \
      >/dev/null
  )
}

run_modern_consumer_smoke() {
  print_section "Running modern consumer smoke"

  "${SCRIPT_DIR}/consumer-smoke.sh" "${PACKAGE_OUTPUT_DIR}" "${PACKAGE_VERSION}"
}

print_section() {
  local title="$1"
  printf '\n==> %s\n' "${title}"
}

main "$@"
