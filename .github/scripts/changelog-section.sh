#!/usr/bin/env bash
# CHANGELOG の指定バージョンの節の本文を標準出力へ書き出す。節が無ければ何も出力しない。
# 使い方: changelog-section.sh <CHANGELOG のパス> <バージョン>
# 構成: "## <バージョン>" がバージョンの節 ("## Unreleased" は対象外)。
set -euo pipefail
file="$1"
version="$2"

[ -f "$file" ] || exit 0
awk -v version="$version" '
  /^## / { in_ver = ($2 == version); next }
  in_ver { print }
' "$file" | sed -e '/./,$!d'
