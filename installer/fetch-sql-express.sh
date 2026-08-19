#!/usr/bin/env bash
set -euo pipefail

url='https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SQLEXPR_x64_ENU.exe'
out="$(dirname "$0")/SQLEXPR_x64_ENU.exe"
expected_size=261082544
expected_sha256='bea033e778048748eb1c87bf57597f7f5449b6a15bac55ddc08263c57f7a1ca8'

curl -fL --retry 3 --retry-delay 2 "$url" -o "$out"
actual_size="$(stat -c '%s' "$out")"
actual_sha256="$(sha256sum "$out" | awk '{print $1}')"

if [[ "$actual_size" != "$expected_size" || "$actual_sha256" != "$expected_sha256" ]]; then
  echo "SQL Server Express media validation failed" >&2
  echo "size=$actual_size sha256=$actual_sha256" >&2
  rm -f "$out"
  exit 1
fi

echo "Validated: $out"
echo "size=$actual_size sha256=$actual_sha256"
