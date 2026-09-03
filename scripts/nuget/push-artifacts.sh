#!/usr/bin/env bash
# Push nupkg + paired snupkg (snupkg only when nupkg newly published).
set -euo pipefail

GLOB="${1:?usage: push-artifacts.sh 'artifacts/AIGuiders.*.nupkg'}"
key="${NUGET_API_KEY:?NUGET_API_KEY required}"
src="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

shopt -s nullglob

push_nupkg() {
  local pkg="$1"
  local log
  local rc=0
  log=$(dotnet nuget push "$pkg" --api-key "$key" --source "$src" --skip-duplicate 2>&1) || rc=$?
  echo "$log"
  if [[ $rc -eq 0 ]]; then
    return 0
  fi
  if echo "$log" | grep -qiE 'already exists|Conflict'; then
    return 0
  fi
  return 1
}

files=( $GLOB )
if [[ ${#files[@]} -eq 0 ]]; then
  echo "No packages match: $GLOB" >&2
  exit 1
fi

for f in "${files[@]}"; do
  echo "=== $f ==="
  if ! push_nupkg "$f"; then
    exit 1
  fi
done
