#!/usr/bin/env bash

# Validates package-level compatibility facts that SDK API compatibility does not
# prove: strong-name identity, embedded ANTLR surface, NuGet dependencies, and
# the deliberate major-version binary identity break from the legacy key.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
readonly REPOSITORY_ROOT
readonly PACKAGE_SOURCE="${1:-${REPOSITORY_ROOT}/artifacts/packages}"
readonly PACKAGE_VERSION="${2:-${PACKAGE_VERSION:-}}"
readonly BASELINE_VERSION="${API_COMPAT_BASELINE_VERSION:-1.3.8}"
readonly NUGET_PACKAGE_SOURCE="${NUGET_PACKAGE_SOURCE:-https://api.nuget.org/v3/index.json}"
readonly EXPECTED_PUBLIC_KEY_TOKEN="${NCALC_EXPECTED_PUBLIC_KEY_TOKEN:-f59bbff0fd43f598}"
readonly EXPECTED_ASSEMBLY_VERSION="${NCALC_EXPECTED_ASSEMBLY_VERSION:-2.0.0.0}"
readonly EXPECTED_LICENSE_EXPRESSION="${NCALC_EXPECTED_LICENSE_EXPRESSION:-MIT}"
readonly LEGACY_PUBLIC_KEY_TOKEN="${NCALC_LEGACY_PUBLIC_KEY_TOKEN:-973cde3f1cafed03}"
readonly PACKAGE_IDENTITY_MODE="${NCALC_PACKAGE_IDENTITY_MODE:-major}"
readonly MINIMUM_MAJOR_PACKAGE_VERSION="${NCALC_MINIMUM_MAJOR_PACKAGE_VERSION:-2}"
if [[ -z "${PACKAGE_COMPATIBILITY_ROOT:-}" ]]; then
  WORK_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/ncalc-package-compatibility.XXXXXX")"
else
  WORK_ROOT="${PACKAGE_COMPATIBILITY_ROOT}"
fi

readonly WORK_ROOT
readonly CURRENT_EXTRACT_ROOT="${WORK_ROOT}/current"
readonly BASELINE_EXTRACT_ROOT="${WORK_ROOT}/baseline"

main() {
  require_command dotnet
  require_command monodis
  require_command sn
  require_command unzip
  require_command mcs
  require_command mono
  require_identity_mode

  mkdir -p "${CURRENT_EXTRACT_ROOT}" "${BASELINE_EXTRACT_ROOT}"

  local current_package
  local baseline_package
  current_package="$(resolve_current_package)"
  baseline_package="$(resolve_baseline_package)"

  extract_package "${current_package}" "${CURRENT_EXTRACT_ROOT}"
  extract_package "${baseline_package}" "${BASELINE_EXTRACT_ROOT}"

  validate_baseline_identity
  validate_current_identity
  validate_current_package_shape
  run_net35_parser_source_smoke
  run_legacy_binary_identity_probe

  echo "Package compatibility checks passed for ${current_package}."
}

require_command() {
  local command_name="$1"

  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "Required command not found: ${command_name}" >&2
    exit 127
  fi
}

require_identity_mode() {
  case "${PACKAGE_IDENTITY_MODE}" in
    major|legacy)
      ;;
    *)
      echo "Unsupported NCALC_PACKAGE_IDENTITY_MODE: ${PACKAGE_IDENTITY_MODE}. Expected major or legacy." >&2
      exit 2
      ;;
  esac
}

resolve_current_package() {
  if [[ -n "${PACKAGE_VERSION}" ]]; then
    find "${PACKAGE_SOURCE}" -maxdepth 1 -name "NCalc.${PACKAGE_VERSION}.nupkg" -print -quit
    return
  fi

  find "${PACKAGE_SOURCE}" -maxdepth 1 -name 'NCalc.*.nupkg' ! -name '*.symbols.nupkg' -print -quit
}

resolve_baseline_package() {
  local package_path
  package_path="${NUGET_PACKAGES:-${HOME}/.nuget/packages}/ncalc/${BASELINE_VERSION}/ncalc.${BASELINE_VERSION}.nupkg"

  if [[ -f "${package_path}" ]]; then
    printf '%s\n' "${package_path}"
    return
  fi

  hydrate_baseline_package

  if [[ -f "${package_path}" ]]; then
    printf '%s\n' "${package_path}"
    return
  fi

  echo "Could not hydrate NCalc ${BASELINE_VERSION} baseline package at ${package_path}." >&2
  exit 2
}

hydrate_baseline_package() {
  local baseline_restore_root
  baseline_restore_root="${WORK_ROOT}/baseline-restore"
  mkdir -p "${baseline_restore_root}"

  cat >"${baseline_restore_root}/NCalc.ApiCompatBaseline.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NCalc" Version="${BASELINE_VERSION}" />
  </ItemGroup>
</Project>
XML

  dotnet restore "${baseline_restore_root}/NCalc.ApiCompatBaseline.csproj" \
    --source "${NUGET_PACKAGE_SOURCE}" \
    >/dev/null
}

extract_package() {
  local package_path="$1"
  local extract_root="$2"

  if [[ -z "${package_path}" || ! -f "${package_path}" ]]; then
    echo "Package not found: ${package_path:-<empty>}" >&2
    exit 2
  fi

  rm -rf "${extract_root}"
  mkdir -p "${extract_root}"
  unzip -q "${package_path}" -d "${extract_root}"
}

validate_baseline_identity() {
  local baseline_assembly
  baseline_assembly="${BASELINE_EXTRACT_ROOT}/lib/NCalc.dll"

  require_file "${baseline_assembly}"
  assert_public_key_token "${baseline_assembly}" "${LEGACY_PUBLIC_KEY_TOKEN}" "legacy baseline"
}

validate_current_identity() {
  local expected_token
  expected_token="${EXPECTED_PUBLIC_KEY_TOKEN}"

  if [[ "${PACKAGE_IDENTITY_MODE}" == "legacy" ]]; then
    expected_token="${LEGACY_PUBLIC_KEY_TOKEN}"
  fi

  while IFS= read -r assembly_path; do
    assert_strong_named "${assembly_path}"
    assert_public_key_token "${assembly_path}" "${expected_token}" "current package"
    assert_assembly_version "${assembly_path}" "${EXPECTED_ASSEMBLY_VERSION}"
  done < <(current_assemblies)
}

validate_current_package_shape() {
  local nuspec_path
  nuspec_path="$(find "${CURRENT_EXTRACT_ROOT}" -maxdepth 1 -name '*.nuspec' -print -quit)"
  require_file "${nuspec_path}"
  assert_package_version_metadata "${nuspec_path}"
  assert_license_metadata "${nuspec_path}"

  if grep -q 'Antlr3.Runtime' "${nuspec_path}"; then
    echo "Current package must not expose Antlr3.Runtime as a NuGet dependency." >&2
    exit 1
  fi

  if find "${CURRENT_EXTRACT_ROOT}" -name 'Antlr3.Runtime.dll' -print -quit | grep -q .; then
    echo "Current package must not include Antlr3.Runtime.dll as a separate asset." >&2
    exit 1
  fi

  while IFS= read -r assembly_path; do
    assert_no_antlr_assembly_reference "${assembly_path}"
    assert_embedded_type "${assembly_path}" "NCalcLexer"
    assert_embedded_type "${assembly_path}" "NCalcParser"
    assert_embedded_type "${assembly_path}" "NCalcParser/ncalcExpression_return"
    assert_embedded_type "${assembly_path}" "Antlr.Runtime.ParserRuleReturnScope"
    assert_embedded_type "${assembly_path}" 'Antlr.Runtime.ParserRuleReturnScope`1'
    assert_embedded_type "${assembly_path}" "Antlr.Runtime.IAstRuleReturnScope"
    assert_embedded_type "${assembly_path}" "Antlr.Runtime.Tree.CommonTree"
  done < <(current_assemblies)
}

assert_package_version_metadata() {
  local nuspec_path="$1"
  local nuspec_version
  local package_major_version

  nuspec_version="$(grep -Eo '<version>[^<]+</version>' "${nuspec_path}" | sed -E 's#</?version>##g' | head -n 1)"

  if [[ -n "${PACKAGE_VERSION}" && "${nuspec_version}" != "${PACKAGE_VERSION}" ]]; then
    echo "Expected NuGet package version ${PACKAGE_VERSION} in ${nuspec_path}, got ${nuspec_version:-<none>}." >&2
    exit 1
  fi

  package_major_version="$(extract_package_major_version "${nuspec_version}")"

  if [[ "${PACKAGE_IDENTITY_MODE}" == "major" && "${package_major_version}" -lt "${MINIMUM_MAJOR_PACKAGE_VERSION}" ]]; then
    echo "Package version ${nuspec_version} is invalid for a new-key major release. Expected major version ${MINIMUM_MAJOR_PACKAGE_VERSION} or later." >&2
    exit 1
  fi
}

extract_package_major_version() {
  local package_version="$1"

  if [[ "${package_version}" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)([-+][0-9A-Za-z.-]+)?$ ]]; then
    printf '%s\n' "${BASH_REMATCH[1]}"
    return
  fi

  echo "Unsupported package version format: ${package_version}" >&2
  exit 2
}

assert_license_metadata() {
  local nuspec_path="$1"

  if ! grep -Eq "<license[[:space:]][^>]*type=\"expression\"[^>]*>${EXPECTED_LICENSE_EXPRESSION}</license>" "${nuspec_path}"; then
    echo "Expected NuGet license expression ${EXPECTED_LICENSE_EXPRESSION} in ${nuspec_path}." >&2
    exit 1
  fi
}

current_assemblies() {
  printf '%s\n' \
    "${CURRENT_EXTRACT_ROOT}/lib/net35/NCalc.dll" \
    "${CURRENT_EXTRACT_ROOT}/lib/netstandard2.0/NCalc.dll"
}

require_file() {
  local file_path="$1"

  if [[ ! -f "${file_path}" ]]; then
    echo "Required file not found: ${file_path}" >&2
    exit 2
  fi
}

assert_strong_named() {
  local assembly_path="$1"

  require_file "${assembly_path}"

  if ! sn -vf "${assembly_path}" >/dev/null; then
    echo "Assembly is not strong-named: ${assembly_path}" >&2
    exit 1
  fi
}

assert_public_key_token() {
  local assembly_path="$1"
  local expected_token="$2"
  local label="$3"
  local actual_token

  actual_token="$(sn -T "${assembly_path}" | awk '/Public Key Token/ { print $4 }')"

  if [[ "${actual_token}" != "${expected_token}" ]]; then
    echo "${label} public key token mismatch for ${assembly_path}: expected ${expected_token}, got ${actual_token:-<none>}." >&2
    exit 1
  fi
}

assert_assembly_version() {
  local assembly_path="$1"
  local expected_version="$2"
  local actual_version

  actual_version="$(monodis --assembly "${assembly_path}" | awk '/^Version:/ { print $2; exit }')"

  if [[ "${actual_version}" != "${expected_version}" ]]; then
    echo "Assembly version mismatch for ${assembly_path}: expected ${expected_version}, got ${actual_version:-<none>}." >&2
    exit 1
  fi
}

assert_no_antlr_assembly_reference() {
  local assembly_path="$1"

  if monodis --assemblyref "${assembly_path}" | grep -q 'Antlr3.Runtime'; then
    echo "Assembly must embed ANTLR instead of referencing Antlr3.Runtime: ${assembly_path}" >&2
    exit 1
  fi
}

assert_embedded_type() {
  local assembly_path="$1"
  local type_name="$2"

  if ! monodis --typedef "${assembly_path}" | awk -v type_name="${type_name}" '$2 == type_name { found = 1 } END { exit(found ? 0 : 1) }'; then
    echo "Expected type ${type_name} in ${assembly_path}." >&2
    exit 1
  fi
}

assert_assembly_reference_token() {
  local assembly_path="$1"
  local reference_name="$2"
  local expected_token="$3"
  local reference_block

  reference_block="$(
    monodis --assemblyref "${assembly_path}" |
      awk -v reference_name="Name=${reference_name}" '
        /^[0-9]+:/ { in_reference = 0 }
        index($0, reference_name) { in_reference = 1 }
        in_reference { print }
      '
  )"

  if ! printf '%s' "${reference_block}" | tr -d '[:space:]' | grep -qi "${expected_token}"; then
    echo "Expected ${assembly_path} to reference ${reference_name} with public key token ${expected_token}." >&2
    exit 1
  fi
}

run_net35_parser_source_smoke() {
  local smoke_root
  smoke_root="${WORK_ROOT}/net35-parser-source-smoke"
  mkdir -p "${smoke_root}"

  cp "${CURRENT_EXTRACT_ROOT}/lib/net35/NCalc.dll" "${smoke_root}/NCalc.dll"
  write_parser_smoke_program "${smoke_root}/ParserSmoke.cs"

  mcs \
    -sdk:4 \
    -r:"${smoke_root}/NCalc.dll" \
    -out:"${smoke_root}/ParserSmoke.exe" \
    "${smoke_root}/ParserSmoke.cs" \
    >/dev/null

  mono "${smoke_root}/ParserSmoke.exe" >/dev/null
}

run_legacy_binary_identity_probe() {
  local smoke_root
  smoke_root="${WORK_ROOT}/legacy-binary-identity-probe"
  mkdir -p "${smoke_root}"

  cp "${BASELINE_EXTRACT_ROOT}/lib/NCalc.dll" "${smoke_root}/NCalc.dll"
  write_parser_smoke_program "${smoke_root}/ParserSmoke.cs"

  mcs \
    -sdk:4 \
    -r:"${smoke_root}/NCalc.dll" \
    -out:"${smoke_root}/ParserSmoke.exe" \
    "${smoke_root}/ParserSmoke.cs" \
    >/dev/null

  cp "${CURRENT_EXTRACT_ROOT}/lib/net35/NCalc.dll" "${smoke_root}/NCalc.dll"
  assert_assembly_reference_token "${smoke_root}/ParserSmoke.exe" "NCalc" "${LEGACY_PUBLIC_KEY_TOKEN}"

  if [[ "${PACKAGE_IDENTITY_MODE}" == "legacy" ]]; then
    mono "${smoke_root}/ParserSmoke.exe" >/dev/null
    return
  fi

  echo "Legacy binary probe references ${LEGACY_PUBLIC_KEY_TOKEN}; current package token ${EXPECTED_PUBLIC_KEY_TOKEN} is a deliberate major-version identity break."
}

write_parser_smoke_program() {
  local program_path="$1"

  cat >"${program_path}" <<'CSHARP'
using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NCalc;

internal static class ParserSmoke
{
    private static int Main()
    {
        object expressionResult = new Expression("2 * (3 + 5)").Evaluate();
        if (!object.Equals(16, expressionResult))
        {
            throw new InvalidOperationException("Unexpected expression result: " + expressionResult);
        }

        NCalcLexer lexer = new NCalcLexer(new ANTLRStringStream("1 + 2"));
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        NCalcParser parser = new NCalcParser(tokens);
        ParserRuleReturnScope parserResult = parser.ncalcExpression();
        NCalcParser.ncalcExpression_return typedResult = (NCalcParser.ncalcExpression_return)parserResult;
        CommonTree tree = typedResult.Tree as CommonTree;

        if (tree == null)
        {
            throw new InvalidOperationException("Expected parser to return a CommonTree.");
        }

        if (parserResult.Start == null || parserResult.Stop == null)
        {
            throw new InvalidOperationException("Expected parser return scope to expose token boundaries.");
        }

        Console.WriteLine("Parser smoke passed.");
        return 0;
    }
}
CSHARP
}

main "$@"
