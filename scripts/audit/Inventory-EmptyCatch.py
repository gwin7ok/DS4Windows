"""
空の catch（本体が空か、コメントのみ）を洗い出し、分類の叩き台を Markdown の表にする（PhaseRV2-0）。

使い方（リポジトリのルートで）:
  python -I scripts/audit/Inventory-EmptyCatch.py > docs-forDIMG/MadeByAgent/PhaseRV2-Catch-Inventory.md

分類（PhaseRV2-Plan.md）: S1 後始末・解放・切断済みの機器への書き込み／S2 古い保存ファイルとの互換（既定値のまま続ける）／
S3 捕まえる型が広すぎる／S4 失敗を診断のログへ残すべき／S5 防御が不要（ログ呼び出しを包むだけ・呼び出し先が投げない）。
ここでの分類は、キーワードによる叩き台。人が見て承認する。
"""
import collections
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = "DS4Windows"
SKIP = {"obj", "bin"}

CATCH = re.compile(r"\bcatch\b\s*(?:\(\s*([^)\s]+)(?:\s+\w+)?\s*\))?\s*(?:when\s*\([^)]*\)\s*)?\{")


def strip_comments(s):
    s = re.sub(r"/\*.*?\*/", "", s, flags=re.S)
    return re.sub(r"//[^\n]*", "", s)


def match_close(t, i):
    depth = 0
    for j in range(i, len(t)):
        c = t[j]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return j
    return -1


def try_body(t, catch_pos):
    k = catch_pos - 1
    while k >= 0 and t[k] in " \t\r\n":
        k -= 1
    if k < 0 or t[k] != "}":
        return ""
    depth = 0
    for j in range(k, -1, -1):
        c = t[j]
        if c == "}":
            depth += 1
        elif c == "{":
            depth -= 1
            if depth == 0:
                return t[j + 1:k]
    return ""


def classify(rel, ctype, body, has_comment):
    b = body
    one = " ".join(b.split())
    if re.fullmatch(r"\s*(AppLogger\.\w+\(.*\);\s*)+", b, flags=re.S) or ("AppLogger." in one and len(one) < 220 and one.count(";") <= 2):
        return "S5", "ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収）"
    if re.search(r"\b(Dispose|Close|Stop|Release|Unplug|Disconnect|Abort|Join|Cancel|Reset|Clear|Remove|Unregister|Shutdown|Kill)\w*\s*\(", one):
        return "S1", "後始末・解放。型は絞らず、理由のコメント（または IOException など限定）"
    if "Core/Persistence" in rel or "Core/Settings" in rel:
        return "S2", "保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない）"
    if re.search(r"\b(File|Directory|Path|Registry|Xml|Process|Stream|Read|Write|Load|Save|Parse|Open)\w*", one):
        return "S3" if ctype in ("", "Exception") else "S1", "ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2）"
    if ctype in ("", "Exception"):
        return "S4", "原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント"
    return "S1", "型は限定済み。理由のコメントがあればよい"


def main():
    rows = []
    for d, dirs, fs in os.walk(ROOT):
        dirs[:] = [x for x in dirs if x not in SKIP]
        for f in fs:
            if not f.endswith(".cs") or f.endswith((".g.cs", ".g.i.cs")):
                continue
            p = os.path.join(d, f)
            rel = p.replace("\\", "/")[len(ROOT) + 1:]
            t = open(p, encoding="utf-8-sig").read().replace("\r\n", "\n")
            for m in CATCH.finditer(t):
                open_i = m.end() - 1
                close_i = match_close(t, open_i)
                if close_i < 0:
                    continue
                inner = t[open_i + 1:close_i]
                if strip_comments(inner).strip() != "":
                    continue
                line = t.count("\n", 0, m.start()) + 1
                ctype = m.group(1) or ""
                body = strip_comments(try_body(t, m.start()))
                has_comment = inner.strip() != ""
                cls, hint = classify(rel, ctype, body, has_comment)
                one = " ".join(body.split())
                rows.append((rel, line, ctype or "(すべて)", one[:70].replace("|", "\\|"), "あり" if has_comment else "なし", cls, hint))
    rows.sort()
    cnt = collections.Counter(r[5] for r in rows)
    print("# PhaseRV2-0 空の catch の分類表（叩き台）\n")
    print("`scripts/audit/Inventory-EmptyCatch.py` が、キーワードで付けた**叩き台**。承認のとき、人が見て直す。\n")
    print(f"- 全 {len(rows)} 件。分類: " + " / ".join(f"{k} {cnt[k]}" for k in sorted(cnt)))
    print(f"- 理由のコメントなし: {sum(1 for r in rows if r[4] == 'なし')} 件、コメントあり: {sum(1 for r in rows if r[4] == 'あり')} 件")
    byf = collections.Counter(r[0] for r in rows)
    print("\n## ファイル別の件数（上位）\n")
    for k, v in byf.most_common(15):
        print(f"- `{k}` {v}")
    print("\n## 一覧\n")
    print("| ファイル:行 | 型 | try の本体（先頭） | コメント | 分類 | 推奨の処置 |")
    print("|---|---|---|---|---|---|")
    for r in rows:
        print(f"| `{r[0]}:{r[1]}` | {r[2]} | {r[3]} | {r[4]} | {r[5]} | {r[6]} |")


main()
