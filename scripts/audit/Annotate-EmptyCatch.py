"""
理由のコメントがない空の catch に、分類（Inventory-EmptyCatch.py）に応じた理由のコメントを足す（PhaseRV2-3〜5）。動きは変えない。
使い方（リポジトリのルートで）: python -I scripts/audit/Annotate-EmptyCatch.py [--dry]
対象外: Core/Persistence/ProfileSections（RV5 で宣言化するまで触らない）。
"""
import os
import sys
import importlib.util

sys.stdout.reconfigure(encoding="utf-8")
spec = importlib.util.spec_from_file_location("inv", os.path.join(os.path.dirname(os.path.abspath(__file__)), "Inventory-EmptyCatch.py"))
inv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inv)

REASON = {
    "S1": "後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける）",
    "S2": "欠けた・壊れた値は、既定値のまま続ける",
    "S3": "ファイル・プロセス・レジストリの失敗は無視して続ける",
    "S4": "失敗しても処理を続ける（呼び出し元に影響させない）",
    "S5": "診断の出力の失敗は無視してよい",
}
dry = "--dry" in sys.argv
total = 0
for d, dirs, fs in os.walk(inv.ROOT):
    dirs[:] = [x for x in dirs if x not in inv.SKIP]
    for f in fs:
        if not f.endswith(".cs") or f.endswith((".g.cs", ".g.i.cs")):
            continue
        p = os.path.join(d, f)
        rel = p.replace("\\", "/")[len(inv.ROOT) + 1:]
        if rel.startswith("Core/Persistence/ProfileSections/"):
            continue
        raw = open(p, "rb").read()
        bom = raw.startswith(b"\xef\xbb\xbf")
        t = (raw[3:] if bom else raw).decode("utf-8")
        crlf = "\r\n" in t
        t = t.replace("\r\n", "\n")
        edits = []
        for m in inv.CATCH.finditer(t):
            open_i = m.end() - 1
            close_i = inv.match_close(t, open_i)
            if close_i < 0:
                continue
            inner = t[open_i + 1:close_i]
            if inner.strip() != "":
                continue
            ctype = m.group(1) or ""
            body = inv.strip_comments(inv.try_body(t, m.start()))
            cls, _ = inv.classify(rel, ctype, body, False)
            if "\n" in inner:
                ls = t.rfind("\n", 0, m.start()) + 1
                indent = t[ls:m.start()]
                indent = indent[:len(indent) - len(indent.lstrip())]
                new = "\n" + indent + "    // " + REASON[cls] + "\n" + indent
            else:
                new = " /* " + REASON[cls] + " */ "
            edits.append((open_i + 1, close_i, new))
        if not edits:
            continue
        for a, b, new in sorted(edits, reverse=True):
            t = t[:a] + new + t[b:]
        total += len(edits)
        if not dry:
            if crlf:
                t = t.replace("\n", "\r\n")
            out = t.encode("utf-8")
            open(p, "wb").write((b"\xef\xbb\xbf" + out) if bom else out)
print("annotated", total)
