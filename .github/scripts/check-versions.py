#!/usr/bin/env python3
"""master への PR で、パッケージの Version を検査する。

- 差分があるのに Version が変わっていないパッケージ: ::warning:: (エラーにはしない)
- Version が変わったパッケージで、次のいずれかならエラー
    - 変更前 (base) の Version 以下
    - nuget.org に公開済みの Version と同じ

使い方: check-versions.py <base の sha> <head の sha> [--no-nuget]
"""
import fnmatch
import json
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

PACKAGES_TSV = Path(__file__).with_name("packages.tsv")

# 全パッケージの成果物に影響しうる共通ファイル。Version を上げるかどうかの判断材料にする。
SHARED_FILES = ["Directory.Build.props", "Directory.Build.targets"]

# 差分の判定から外すもの。ドキュメントと生成物は、パッケージの中身を変えない。
IGNORED_PATTERNS = ["*.md", "*/bin/*", "*/obj/*"]


def git(*args):
    return subprocess.run(
        ["git", *args], check=True, capture_output=True, text=True, encoding="utf-8"
    ).stdout


def read_version(rev, csproj):
    try:
        text = git("show", f"{rev}:{csproj}")
    except subprocess.CalledProcessError:
        return None
    import re

    m = re.search(r"<Version>([^<]+)</Version>", text)
    return m.group(1).strip() if m else None


def parse_semver(v):
    core, _, pre = v.partition("-")
    nums = tuple(int(x) for x in core.split("+")[0].split("."))
    return nums, pre.split("+")[0] if pre else None


def compare(a, b):
    """a < b なら負、等しければ 0、a > b なら正。プレリリースは同じ本体より小さい。"""
    (an, ap), (bn, bp) = parse_semver(a), parse_semver(b)
    if an != bn:
        return -1 if an < bn else 1
    if ap == bp:
        return 0
    if ap is None:
        return 1
    if bp is None:
        return -1
    for x, y in zip(ap.split("."), bp.split(".")):
        if x == y:
            continue
        if x.isdigit() and y.isdigit():
            return -1 if int(x) < int(y) else 1
        if x.isdigit() != y.isdigit():
            return -1 if x.isdigit() else 1
        return -1 if x < y else 1
    return (len(ap.split(".")) > len(bp.split("."))) - (len(ap.split(".")) < len(bp.split(".")))


def published_versions(package_id):
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=30) as res:
            return [v.lower() for v in json.load(res)["versions"]]
    except urllib.error.HTTPError as e:
        if e.code == 404:
            return []
        raise


def is_ignored(path):
    return any(fnmatch.fnmatch(path, p) for p in IGNORED_PATTERNS)


def main():
    base, head = sys.argv[1], sys.argv[2]
    use_nuget = "--no-nuget" not in sys.argv
    changed = [p for p in git("diff", "--name-only", f"{base}...{head}").splitlines() if not is_ignored(p)]
    shared_changed = [p for p in changed if p in SHARED_FILES]
    errors = 0

    for line in PACKAGES_TSV.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        prefix, csproj, package_id, dirs = line.split("\t")
        dirs = dirs.split()
        files = [p for p in changed if any(p.startswith(d + "/") for d in dirs)]
        old = read_version(base, csproj)
        new = read_version(head, csproj)

        if new is None:
            print(f"::error file={csproj}::{csproj} から <Version> が読めません")
            errors += 1
            continue

        if old != new:
            print(f"{package_id}: {old} -> {new}")
            if old is not None and compare(new, old) <= 0:
                print(f"::error file={csproj}::{package_id} の Version ({new}) が変更前 ({old}) より大きくありません")
                errors += 1
            if use_nuget and new.lower() in published_versions(package_id):
                print(f"::error file={csproj}::{package_id} {new} は nuget.org に公開済みです。別のバージョンにしてください")
                errors += 1
            continue

        affected = files + shared_changed
        if affected:
            shown = ", ".join(affected[:3]) + (" ほか" if len(affected) > 3 else "")
            print(
                f"::warning file={csproj}::{package_id} に変更がありますが Version が {new} のままです"
                f" ({shown})。公開が必要なら Version を上げてください"
            )
        else:
            print(f"{package_id}: 変更なし ({new})")

    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
