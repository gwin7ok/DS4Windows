#!/usr/bin/env python3
"""経緯のコメントの候補を出す（PhaseRV8。読むだけで、ファイルは変えない）。

使い方: python -I scripts/audit/Find-HistoryComments.py [--summary] [--long N] フォルダー...
分類（PhaseRV8-Comment-Rules.md）:
  c  Phase／Step の参照（Phase12、Step4b、PhaseRV3 など）
  d  旧実装の説明・削除の記録（「旧:」「旧実装」「…を削除した」「廃止した」）
  e  長すぎるコメント（連続する // または /// の行が N 行以上。既定 12）
  keep  TODO／FIXME／HACK を含む行（原則4。触らない）
出力: パス:行:分類:本文（--summary は、層・分類ごとの件数のみ）
"""
import argparse, collections, os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

PHASE = re.compile(r"Phase\s?[0-9A-Za-z]+(?:-[A-Za-z0-9]+)*|PhaseRV\d|Step\s?\d+[a-z]?|Step[0-9]+[a-z]?-")
OLD = re.compile(r"旧(?:実装|版|[:：])|旧[A-Za-z]|を削除した|を廃止した|削除済み|廃止済み|撤去した")
KEEP = re.compile(r"TODO|FIXME|HACK")
LINE = re.compile(r"^\s*(//+|<!--)(.*)$")


def comment_text(line: str):
    m = LINE.match(line)
    if m:
        return m.group(2)
    i = line.find(" //")
    if i >= 0 and line.count('"', 0, i) % 2 == 0:
        return line[i + 3:]
    return None


def scan(path, long_n, rows):
    try:
        lines = open(path, encoding="utf-8-sig").read().splitlines()
    except Exception:
        return
    run = 0
    for no, line in enumerate(lines, 1):
        t = comment_text(line)
        full = LINE.match(line) is not None
        run = run + 1 if (t is not None and full) else 0
        if run == long_n:
            rows.append((path, no - long_n + 1, "e", f"{long_n} 行以上のコメントの塊"))
        if t is None:
            continue
        if KEEP.search(t):
            rows.append((path, no, "keep", t.strip()))
            continue
        if PHASE.search(t):
            rows.append((path, no, "c", t.strip()))
        if OLD.search(t):
            rows.append((path, no, "d", t.strip()))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--summary", action="store_true")
    ap.add_argument("--long", type=int, default=12)
    ap.add_argument("roots", nargs="+")
    a = ap.parse_args()
    rows = []
    for root in a.roots:
        for dp, dn, fn in os.walk(root):
            dn[:] = [d for d in dn if d not in ("bin", "obj", ".git")]
            for f in fn:
                if f.endswith((".cs", ".xaml")):
                    scan(os.path.join(dp, f), a.long, rows)
    if a.summary:
        cnt = collections.Counter((r[2]) for r in rows)
        files = collections.defaultdict(set)
        for r in rows:
            files[r[2]].add(r[0])
        for k in sorted(cnt):
            print(f"{k}: 行 {cnt[k]}、ファイル {len(files[k])}")
    else:
        for p, n, k, t in rows:
            print(f"{p}:{n}:{k}:{t}")


if __name__ == "__main__":
    sys.exit(main())
