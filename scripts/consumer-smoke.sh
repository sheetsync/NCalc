#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly REPOSITORY_ROOT
readonly PACKAGE_SOURCE="${1:-${REPOSITORY_ROOT}/artifacts/packages}"
readonly PACKAGE_VERSION="${2:-${PACKAGE_VERSION:-}}"

if [[ -z "${SMOKE_ROOT:-}" ]]; then
  SMOKE_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/ncalc-consumer-smoke.XXXXXX")"
fi

readonly SMOKE_ROOT
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
using System.IO;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NCalc;

object result = new Expression("2 * (3 + 5)").Evaluate();

if (!Equals(16, result))
{
    throw new InvalidOperationException($"Unexpected NCalc result: {result}");
}

NCalcLexer lexer = new NCalcLexer(new ANTLRStringStream("1 + 2"));
CommonTokenStream tokens = new CommonTokenStream(lexer);
NCalcParser parser = new NCalcParser(tokens);
NCalcParser.ncalcExpression_return parserResult = parser.ncalcExpression();
CommonTree? tree = parserResult.Tree as CommonTree;

if (tree is null)
{
    throw new InvalidOperationException("Expected parser to return a CommonTree.");
}

string filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
try
{
    File.WriteAllText(filePath, "abc", Encoding.UTF8);
    ANTLRFileStream fileStream = new ANTLRFileStream(filePath, Encoding.UTF8);

    if (fileStream.SourceName != filePath || fileStream.Count != 3 || fileStream.Substring(0, 3) != "abc")
    {
        throw new InvalidOperationException("Expected ANTLRFileStream to read a file through the packed NCalc assembly.");
    }
}
finally
{
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
    }
}

Console.WriteLine($"NCalc consumer smoke passed: {result}");
CSHARP
}

run_smoke_program() {
  dotnet run --project "${SMOKE_PROJECT}" --configuration Release --no-restore
}

main "$@"
