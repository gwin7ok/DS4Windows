#!/usr/bin/env python3
"""文書の参照（.md のパス）が、実在するファイルを指すかを検査する（PhaseRV8）。

使い方: python -I scripts/audit/Check-DocLinks.py [--fix]
対象: docs-forDIMG（Archive を除く）、.github、scripts、README 類の .md／.py／.json と、DS4Windows・DS4WindowsTests の .cs。
判定: パスを含む参照（`docs-forDIMG/…/X.md`、`MadeByAgent/X.md`、`](X.md)`）は、リポジトリの根、参照元のフォルダー、docs-forDIMG、docs-forDIMG/MadeByAgent のいずれかを基準に実在すること。
      名前だけの参照（`X.md`）は、Archive にだけ実在するときに「移動済み」として報告する。
--fix: 移動済みの参照を Archive 付きのパスへ直す。終了コード: 0 = 切れなし、1 = 切れあり
"""
import os, re, sys
sys.stdout.reconfigure(encoding="utf-8")
ROOT = os.getcwd()
M = "docs-forDIMG/MadeByAgent"
TOKEN = re.compile(r"(?<![A-Za-z0-9_./\x5c-])((?:[A-Za-z0-9_.\-]+[/\x5c])*[A-Za-z0-9_.\-]+\.md)(?![A-Za-z0-9_])")
fix = "--fix" in sys.argv
archived = {f for f in os.listdir(f"{M}/Archive")} if os.path.isdir(f"{M}/Archive") else set()
allmd = {}
for dp, dn, fn in os.walk("."):
    dn[:] = [d for d in dn if d not in (".git", "bin", "obj", "node_modules")]
    for f in fn:
        if f.endswith(".md"):
            allmd.setdefault(f, []).append(os.path.relpath(os.path.join(dp, f), ".").replace("\\", "/"))

def exists(tok, base):
    t = tok.replace("\\", "/")
    for b in (ROOT, base, os.path.join(ROOT, "docs-forDIMG"), os.path.join(ROOT, M)):
        if os.path.exists(os.path.join(b, t)):
            return True
    return False

bad = 0
fixed = 0
targets = []
for top in ("docs-forDIMG", ".github", "scripts", "DS4Windows", "DS4WindowsTests"):
    for dp, dn, fn in os.walk(top):
        dn[:] = [d for d in dn if d not in ("bin", "obj", "Archive", "out", "__pycache__", "imageshot")]
        for f in fn:
            if f.endswith((".md", ".py", ".json", ".cs", ".csproj", ".yml")) and f != "Check-DocLinks.py":
                targets.append(os.path.join(dp, f))
for p in targets:
    try:
        raw = open(p, "rb").read(); bom = raw.startswith(b"\xef\xbb\xbf"); t = (raw[3:] if bom else raw).decode("utf-8")
    except Exception:
        continue
    base = os.path.dirname(os.path.abspath(p))
    def rep(m):
        global bad, fixed
        tok = m.group(1); name = os.path.basename(tok.replace("\\", "/"))
        has_path = ("/" in tok) or ("\\" in tok)
        if exists(tok, base):
            return tok
        if name in archived and (not has_path or "Archive" not in tok):
            bad += 1
            if fix:
                fixed += 1
                prefix = tok[: len(tok) - len(name)]
                if prefix.replace(chr(92), "/").rstrip("/").endswith("MadeByAgent"):
                    new = prefix + "Archive/" + name
                elif prefix:
                    new = M + "/Archive/" + name
                elif os.path.abspath(base) == os.path.abspath(M):
                    new = "Archive/" + name
                else:
                    new = M + "/Archive/" + name
                return new
            print(f"移動済み: {p}: {tok}")
            return tok
        if has_path and name not in allmd:
            bad += 1; print(f"切れ: {p}: {tok}")
        return tok
    t2 = TOKEN.sub(rep, t)
    if fix and t2 != t:
        open(p, "wb").write((b"\xef\xbb\xbf" if bom else b"") + t2.encode("utf-8"))
print(f"問題 {bad}、修正 {fixed}")
sys.exit(1 if (bad and not fix) else 0)
