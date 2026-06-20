#!/usr/bin/env bash

set -euo pipefail

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly PACKAGE_SOURCE="${1:-${REPOSITORY_ROOT}/artifacts/packages}"
readonly PACKAGE_VERSION="${2:-${PACKAGE_VERSION:-}}"
readonly SMOKE_ROOT="${SMOKE_ROOT:-$(mktemp -d "${TMPDIR:-/tmp}/ncalc-consumer-smoke.XXXXXX")}"
readonly SMOKE_PROJECT="${SMOKE_ROOT}/NCalc.ConsumerSmoke/NCalc.ConsumerSmoke.csproj"
readonly SMOKE_PACKAGES_DIR="${SMOKE_ROOT}/packages"

main() {
  require_command dotnet
  require_package_source
  create_consumer_project
  add_local_package
  write_smoke_program
  run_smoke_program
}

require_command() {
  local command_name="$1"

  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "Required command not found: ${command_name}" >&2
    exit 127
  fi
}

require_package_source() {
  if [[ -n "${PACKAGE_VERSION}" ]]; then
    if ! find "${PACKAGE_SOURCE}" -maxdepth 1 -name "NCalc.${PACKAGE_VERSION}.nupkg" -print -quit | grep -q .; then
      echo "NCalc ${PACKAGE_VERSION} package not found in ${PACKAGE_SOURCE}" >&2
      exit 2
    fi

    return
  fi

  if ! find "${PACKAGE_SOURCE}" -maxdepth 1 -name 'NCalc.*.nupkg' -print -quit | grep -q .; then
    echo "No .nupkg files found in ${PACKAGE_SOURCE}" >&2
    exit 2
  fi
}

create_consumer_project() {
  dotnet new console \
    --framework net10.0 \
    --name NCalc.ConsumerSmoke \
    --output "${SMOKE_ROOT}/NCalc.ConsumerSmoke" \
    >/dev/null
}

add_local_package() {
  local package_arguments=(NCalc --source "${PACKAGE_SOURCE}" --no-restore)

  if [[ -n "${PACKAGE_VERSION}" ]]; then
    package_arguments+=(--version "${PACKAGE_VERSION}")
  fi

  dotnet add "${SMOKE_PROJECT}" package "${package_arguments[@]}" >/dev/null

  dotnet restore "${SMOKE_PROJECT}" \
    --source "${PACKAGE_SOURCE}" \
    --source "https://api.nuget.org/v3/index.json" \
    /p:RestorePackagesPath="${SMOKE_PACKAGES_DIR}" \
    >/dev/null
}

write_smoke_program() {
  cat >"${SMOKE_ROOT}/NCalc.ConsumerSmoke/Program.cs" <<'CSHARP'
using System;
using NCalc;

object result = new Expression("2 * (3 + 5)").Evaluate();

if (!Equals(16, result))
{
    throw new InvalidOperationException($"Unexpected NCalc result: {result}");
}

Console.WriteLine($"NCalc consumer smoke passed: {result}");
CSHARP
}

run_smoke_program() {
  dotnet run --project "${SMOKE_PROJECT}" --configuration Release --no-restore
}

main "$@"
