"""
空の catch のうち S3（型が広すぎる）・S4（診断ログを足すべき）の候補に、処置の提案（型の限定／Debug ログ／そのまま）を付けて一覧にする（PhaseRV2。D1・D2）。
使い方: python -I scripts/audit/Propose-CatchActions.py > docs-forDIMG/MadeByAgent/PhaseRV2-Catch-Proposals.md
提案の規則:
  - 毎フレームの経路・読み取りスレッド・タイマーの本体・最上位の安全弁（HOT）は、型を絞らず、ログも足さない（D2）。
  - HOT でない S3 は、try の本体の呼び出しから、想定する例外へ限定する。
  - HOT でない S4 は、AppLogger.LogDebug（ログファイルだけ）を足す。
"""
import collections
import importlib.util
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")
here = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("inv", os.path.join(here, "Inventory-EmptyCatch.py"))
inv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inv)

HOT_PREFIX = (
    "Input/Devices/", "Processing/Mapping", "Processing/Synthetic/", "Processing/Touchpad/", "Processing/Gyro/",
    "Processing/Macro/", "Processing/Services/Conditioning/", "Processing/RepeatHelper", "Processing/Actions/",
    "Output/Udp/", "Output/Devices/", "UI/Services/GuardedTimer",
)


def narrowing(body):
    one = " ".join(body.split())
    kinds = []
    if re.search(r"\b(File|Directory|Path|Stream|Reader|Writer|Copy|Delete|Move|Create)\w*\s*[.(]", one) or "File." in one or "Directory." in one:
        kinds += ["IOException", "UnauthorizedAccessException"]
    if re.search(r"Registry|RegistryKey|OpenSubKey", one):
        kinds += ["SecurityException", "IOException", "UnauthorizedAccessException"]
    if re.search(r"Process\.|\.Start\(|ProcessStartInfo", one):
        kinds += ["Win32Exception", "InvalidOperationException"]
    if re.search(r"\bXml|\.Load\(|\.Save\(", one):
        kinds += ["XmlException", "IOException"]
    if re.search(r"Parse|Convert\.|\(int\)|\(byte\)", one):
        kinds += ["FormatException", "OverflowException"]
    return sorted(set(kinds), key=kinds.index) if kinds else []


def main():
    rows = []
    for d, dirs, fs in os.walk(inv.ROOT):
        dirs[:] = [x for x in dirs if x not in inv.SKIP]
        for f in fs:
            if not f.endswith(".cs") or f.endswith((".g.cs", ".g.i.cs")):
                continue
            p = os.path.join(d, f)
            rel = p.replace("\\", "/")[len(inv.ROOT) + 1:]
            if rel.startswith("Core/Persistence/ProfileSections/"):
                continue
            t = open(p, encoding="utf-8-sig").read().replace("\r\n", "\n")
            for m in inv.CATCH.finditer(t):
                open_i = m.end() - 1
                close_i = inv.match_close(t, open_i)
                if close_i < 0:
                    continue
                inner = t[open_i + 1:close_i]
                if inv.strip_comments(inner).strip() != "":
                    continue
                ctype = m.group(1) or ""
                body = inv.strip_comments(inv.try_body(t, m.start()))
                cls, _ = inv.classify(rel, ctype, body, True)
                if cls not in ("S3", "S4"):
                    continue
                line = t.count("\n", 0, m.start()) + 1
                hot = rel.startswith(HOT_PREFIX)
                one = " ".join(body.split())[:60].replace("|", "\\|")
                if hot:
                    act = "そのまま（毎フレーム・安全弁。型は絞らない）"
                elif cls == "S3":
                    k = narrowing(body)
                    act = ("型を限定: " + "・".join(k)) if k and ctype in ("", "Exception") else "そのまま（想定する例外が決められない）"
                else:
                    act = "LogDebug を足す（ファイルだけ）"
                rows.append((rel, line, ctype or "(すべて)", one, cls, hot, act))
    rows.sort()
    byf = collections.OrderedDict()
    for r in rows:
        byf.setdefault(r[0], []).append(r)
    n_narrow = sum(1 for r in rows if r[6].startswith("型を限定"))
    n_log = sum(1 for r in rows if r[6].startswith("LogDebug"))
    n_keep = len(rows) - n_narrow - n_log
    print("# PhaseRV2 S3・S4 の処置の提案（承認用）\n")
    print("`scripts/audit/Propose-CatchActions.py` が、規則（D1・D2）から作った**提案**。承認のとき、人が見て直す。\n")
    print(f"- 対象 {len(rows)} 件（S3・S4。`ProfileSections` は RV5 まで対象外）")
    print(f"- 提案: 型を限定 {n_narrow} 件 / LogDebug を足す {n_log} 件 / そのまま {n_keep} 件")
    print("- 毎フレームの経路・読み取りスレッド・タイマー・最上位の安全弁（`Input/Devices`、`Processing` の入力の加工・マッピング・アクション、`Output/Udp`・`Output/Devices` など）は、型を絞らず、ログも足さない（D2。Phase29 の教訓）。")
    print("- 型を限定すると、想定外の例外が上へ伝わる。限定した箇所は、実機確認の項目（Device-Check-Checklist §9c）へ足す。\n")
    print("## ファイル別\n")
    print("| ファイル | 件数 | 提案 |")
    print("|---|---|---|")
    for f, rs in byf.items():
        c = collections.Counter(r[6].split(":")[0] for r in rs)
        print(f"| `{f}` | {len(rs)} | " + " / ".join(f"{k} {v}" for k, v in c.items()) + " |")
    print("\n## 一覧\n")
    print("| ファイル:行 | 型 | try の本体（先頭） | 分類 | 提案 |")
    print("|---|---|---|---|---|")
    for r in rows:
        print(f"| `{r[0]}:{r[1]}` | {r[2]} | {r[3]} | {r[4]} | {r[6]} |")


if __name__ == "__main__":
    main()
