"""
Nullable の警告のうち、機械的に直せるものを直す（PhaseRV9）。ビルドの記録の位置から、型に `?` を足すだけ。

  CS8622  イベントハンドラーの `object sender` → `object? sender`（メッセージのメソッド名から、同じファイルの宣言を探す）
  CS8612  `event PropertyChangedEventHandler X;` → `event PropertyChangedEventHandler? X;`（位置の行の `event 型 名` の型へ `?`）

使い方（リポジトリのルートで）:
  python -I scripts/audit/Fix-NullableMechanical.py nullable.log [--only DS4Windows/Input]
BOM・改行を保つ。直せなかった警告は、件数だけ表示する（手で直す）。
"""
import re
import sys

BS = chr(92)
PAT = re.compile(r"^(?P<path>[A-Za-z]:\x5c[^()]+?\.cs)\((?P<line>\d+),(?P<col>\d+)\): warning (?P<id>CS86\d\d): (?P<msg>.*)$")
METHOD = re.compile(r"'[^']*? (?:[\w.<>]+\.)?(?P<name>\w+)\((?P<args>[^']*)\)'")


def read(path):
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = (raw[3:] if bom else raw).decode("utf-8")
    return text, bom


def write(path, text, bom):
    out = text.encode("utf-8")
    if bom:
        out = b"\xef\xbb\xbf" + out
    open(path, "wb").write(out)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].replace("/", BS)
        args = [a for a in args if a.replace("/", BS) != only]
    log = args[0]
    items = {}
    for ln in open(log, encoding="utf-8", errors="ignore"):
        m = PAT.match(ln.strip())
        if not m or m.group("id") not in ("CS8622", "CS8612"):
            continue
        if only and (BS + only) not in m.group("path"):
            continue
        items[(m.group("path"), int(m.group("line")), m.group("id"), m.group("msg"))] = m
    files = {}
    fixed = 0
    skipped = 0
    for (path, line, wid, msg), m in sorted(items.items()):
        if path not in files:
            files[path] = read(path)
        text, bom = files[path]
        if wid == "CS8612":
            lines = text.split("\n")
            idx = line - 1
            new = re.sub(r"(\bevent\s+[\w.]+)(\s+\w+\s*[;{])", r"\1?\2", lines[idx], count=1) if "?" not in lines[idx].split("event", 1)[-1].split()[0:1] else lines[idx]
            if new != lines[idx]:
                lines[idx] = new
                files[path] = ("\n".join(lines), bom)
                fixed += 1
            else:
                skipped += 1
        else:
            mm = METHOD.search(msg)
            if not mm:
                skipped += 1
                continue
            name = mm.group("name")
            pat = re.compile(r"(\b" + re.escape(name) + r"\s*\(\s*)object(\s+sender\b)")
            new_text, n = pat.subn(r"\1object?\2", text)
            if n:
                files[path] = (new_text, bom)
                fixed += n
            else:
                skipped += 1
    for path, (text, bom) in files.items():
        write(path, text, bom)
    print("fixed", fixed, "skipped", skipped, "files", len(files))


if __name__ == "__main__":
    main()
