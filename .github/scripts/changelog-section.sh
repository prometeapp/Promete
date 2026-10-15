#!/usr/bin/env bash
# CHANGELOG の指定パッケージ・バージョンの節の本文を標準出力へ書き出す。節が無ければ何も出力しない。
# 使い方: changelog-section.sh <CHANGELOG のパス> <パッケージ ID> <バージョン>
# 構成: "## <パッケージ ID> <バージョン>" がバージョンの節 ("## Unreleased" は対象外)。
set -euo pipefail
file="$1"
id="$2"
version="$3"

[ -f "$file" ] || exit 0
awk -v id="$id" -v version="$version" '
  /^## / { in_ver = ($2 == id && $3 == version); next }
  in_ver { print }
' "$file" | sed -e '/./,$!d'
