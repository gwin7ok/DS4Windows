"""
Nullable の警告を、ビルドの記録からフォルダー別に数える（PhaseRV9）。

使い方（リポジトリのルートで）:
  1. 全体を有効にして測る（設定を変えず、コマンドの引数だけで有効にする）:
       dotnet build DS4Windows/DS4WinWPF.csproj -c Debug /p:platform=x64 -p:Nullable=enable -v:n > nullable-all.log
  2. python -I scripts/audit/Measure-Nullable.py nullable-all.log [--only 先頭のフォルダー]
出力: 層ごとの合計、フォルダー別（警告・ファイル数・上位の ID）、ID 別、上位のファイル。
同じ警告は、重複を除いて数える。
"""
import re
import sys
from collections import Counter, defaultdict

BS = chr(92)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]
        args = [a for a in args if a != only]
    log = args[0]
    pat = re.compile(r"^(?P<path>[A-Za-z]:\x5c[^()]+?\.cs)\((?P<line>\d+),(?P<col>\d+)\): (?P<sev>warning|error) (?P<id>CS\d+):")
    root = None
    seen = set()
    for ln in open(log, encoding="utf-8", errors="ignore"):
        m = pat.match(ln.strip())
        if not m:
            continue
        i = m.group("id")
        if not (i.startswith("CS86") or i.startswith("CS87")):
            continue
        seen.add((m.group("path"), m.group("line"), m.group("col"), i))
    marker = BS + "DS4Windows" + BS
    inroot = set()
    for s in seen:
        if marker in s[0]:
            inroot.add((s[0][s[0].index(marker) + len(marker):],) + s[1:])
    by = Counter()
    files = defaultdict(set)
    ids = defaultdict(Counter)

    def key(rel):
        parts = rel.split(BS)
        if len(parts) == 1:
            return "(root)"
        if len(parts) > 2 and parts[0] in ("Core", "Processing", "Input", "Output", "UI"):
            return parts[0] + "/" + parts[1]
        return parts[0]

    for rel, l, c, i in inroot:
        if only and not rel.startswith(only):
            continue
        k = key(rel)
        by[k] += 1
        files[k].add(rel)
        ids[k][i] += 1
    print("total", sum(by.values()), "files", len({f for s in files.values() for f in s}))
    top = Counter()
    for k, v in by.items():
        top[k.split("/")[0]] += v
    print("-- layers")
    for k, v in sorted(top.items(), key=lambda x: -x[1]):
        print("%-14s %d" % (k, v))
    print("-- folders: warnings / files / top ids")
    for k, v in sorted(by.items(), key=lambda x: -x[1]):
        print("%-24s %5d %4d  %s" % (k, v, len(files[k]), " ".join("%s=%d" % (a, b) for a, b in ids[k].most_common(3))))
    fc = Counter(s[0] for s in inroot if not only or s[0].startswith(only))
    print("-- top files")
    for p, v in fc.most_common(15):
        print(v, p)


if __name__ == "__main__":
    main()
