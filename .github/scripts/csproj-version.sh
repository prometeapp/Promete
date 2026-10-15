#!/usr/bin/env bash
# csproj の <Version> を標準出力へ書き出す。ファイルが無い、または <Version> が無ければ空を返す。
# 使い方: csproj-version.sh <csproj のパス> [リビジョン]
#   リビジョンを省略すると作業ツリーのファイルを読む。
#   "0000000..." (push の before が無い場合) も、ファイルが無いものとして扱う。
set -uo pipefail
path="$1"
rev="${2:-}"

if [ -z "$rev" ]; then
  content=$(cat "$path" 2>/dev/null) || exit 0
elif [[ "$rev" =~ ^0+$ ]]; then
  exit 0
else
  content=$(git show "$rev:$path" 2>/dev/null) || exit 0
fi

printf '%s\n' "$content" | sed -n 's#.*<Version>\([^<]*\)</Version>.*#\1#p' | head -n 1
