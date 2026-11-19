"""
Nullable の警告のうち、機械的に直せるものを直す（PhaseRV9）。ビルドの記録の位置から、宣言の型へ `?` を足すだけ（実行時の動きは変わらない）。

  CS8622  イベントハンドラーの `object sender` → `object? sender`（メッセージのメソッド名から、同じファイルの宣言を探す）
  CS8612  `event 型 名;` → `event 型? 名;`
  CS8618  メッセージの名前（フィールド・プロパティ・イベント）の宣言の型へ `?`
  CS8600  `型 名 = 式;`（`using (` `foreach (` `out 型 名` を含む）の型へ `?`
  CS8625  `型 名 = null`（既定値・初期化）の型へ `?`、`名 = null;`（代入）は名前の宣言の型へ `?`
  CS8603  `return …` の警告は、囲むメソッド・プロパティの戻り値の型へ `?`（Task<…> と値型は対象外）

直せなかった警告と、`?` で増える連鎖の警告（CS8602・CS8604 など）は、手で直す。
使い方（リポジトリのルートで）:
  python -I scripts/audit/Fix-NullableMechanical.py nullable.log [--only Input] [--kinds CS8618,CS8600]
BOM・改行を保つ。結果は必ず git diff で確かめ、ビルドで連鎖を確かめる。
"""
import re
import sys

BS = chr(92)
PAT = re.compile(r"^(?P<path>[A-Za-z]:\x5c[^()]+?\.cs)\((?P<line>\d+),(?P<col>\d+)\): warning (?P<id>CS86\d\d): (?P<msg>.*)$")
METHOD = re.compile(r"'[^']*? (?:[\w.]+\.)?(?P<name>\w+)\((?P<args>[^']*)\)'")
NAME_IN_MSG = re.compile(r"(?:フィールド|プロパティ|イベント|field|property|event) '(?P<name>[\w.]+)'")
TYPE = r"[A-Za-z_][\w.]*(?:<[^=;()]*?>)?(?:\[[,]*\])*"
VALUE_TYPES = {"void", "bool", "int", "uint", "long", "ulong", "short", "ushort", "byte", "sbyte", "char", "float", "double", "decimal",
               "IntPtr", "UIntPtr", "var", "dynamic", "Guid", "DateTime", "TimeSpan"}
MODS = r"(?:(?:public|private|internal|protected|static|readonly|volatile|virtual|override|sealed|new|abstract|unsafe|extern|async|partial|const)\s+)*"


def read(path):
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    return (raw[3:] if bom else raw).decode("utf-8"), bom


def write(path, text, bom):
    out = text.encode("utf-8")
    open(path, "wb").write((b"\xef\xbb\xbf" + out) if bom else out)


def addq(t):
    if t.endswith("?") or t in VALUE_TYPES or t.startswith("Task<") or t == "Task":
        return None
    return t + "?"


def fix_line_decl(line, kind):
    """宣言の型へ ? を足した行を返す（直せなければ None）。"""
    patterns = []
    if kind in ("CS8600", "CS8625"):
        # out 型 名
        m = re.search(r"\bout\s+(" + TYPE + r")(\s+\w+)", line)
        if m and kind == "CS8600":
            q = addq(m.group(1))
            if q:
                return line[:m.start(1)] + q + line[m.end(1):]
        suffix = r"\s+\w+\s*=" if kind == "CS8600" else r"\s+\w+\s*=\s*null\b"
        for m in re.finditer(r"(?:(?<=[\s(])|^)(" + TYPE + r")(" + suffix + r")", line):
            t = m.group(1)
            if t in ("return", "else", "new", "await", "throw", "yield"):
                continue
            q = addq(t)
            if q:
                return line[:m.start(1)] + q + line[m.end(1):]
        # パラメーター: 型 名 = null,
        return None
    return None


def find_decl_line(lines, name, upto=None):
    pat = re.compile(r"^\s*" + MODS + r"(" + TYPE + r")\s+" + re.escape(name) + r"\s*(;|=[^=>]|\{|=>)")
    ev = re.compile(r"^\s*" + MODS + r"event\s+(" + TYPE + r")\s+" + re.escape(name) + r"\s*[;{=]")
    for i, ln in enumerate(lines):
        m = ev.match(ln) or pat.match(ln)
        if m and m.group(1) not in ("return", "else", "var", "new"):
            return i, m
    return None


def main():
    argv = sys.argv[1:]
    only = None
    kinds = None
    if "--only" in argv:
        k = argv.index("--only")
        only = argv[k + 1].replace("/", BS)
        del argv[k:k + 2]
    if "--kinds" in argv:
        k = argv.index("--kinds")
        kinds = set(argv[k + 1].split(","))
        del argv[k:k + 2]
    log = argv[0]
    items = {}
    for ln in open(log, encoding="utf-8-sig", errors="ignore"):
        m = PAT.match(ln.strip())
        if not m:
            continue
        wid = m.group("id")
        if wid not in ("CS8622", "CS8612", "CS8618", "CS8600", "CS8625", "CS8603"):
            continue
        if kinds and wid not in kinds:
            continue
        if "_wpftmp" in m.group("path"):
            continue
        if only and (BS + "DS4Windows" + BS + only) not in m.group("path"):
            continue
        items[(m.group("path"), int(m.group("line")), wid, m.group("msg"))] = 1
    files = {}
    fixed = 0
    skipped = []
    for (path, line, wid, msg) in sorted(items):
        if path not in files:
            t, bom = read(path)
            files[path] = (t.split("\n"), bom)
        lines, bom = files[path]
        idx = line - 1
        done = False
        if wid == "CS8612":
            m = re.search(r"(\bevent\s+" + TYPE + r")(\s+\w+\s*[;{])", lines[idx])
            if m and not m.group(1).endswith("?"):
                lines[idx] = lines[idx][:m.end(1)] + "?" + lines[idx][m.end(1):]
                done = True
        elif wid == "CS8622":
            mm = METHOD.search(msg)
            if mm:
                pat = re.compile(r"(\b" + re.escape(mm.group("name")) + r"\s*\(\s*)object(\s+sender\b)")
                for i, ln in enumerate(lines):
                    if pat.search(ln):
                        lines[i] = pat.sub(r"\1object?\2", ln)
                        done = True
                        break
        elif wid == "CS8618":
            mm = NAME_IN_MSG.search(msg)
            if mm:
                name = mm.group("name").split(".")[-1]
                r = find_decl_line(lines, name)
                if r:
                    i, m = r
                    q = addq(m.group(1))
                    if q:
                        # 型の直後に ? を足す
                        s = lines[i]
                        k = s.index(m.group(1), m.start(1) if m.start(1) >= 0 else 0)
                        lines[i] = s[:k] + q + s[k + len(m.group(1)):]
                        done = True
        elif wid in ("CS8600", "CS8625"):
            new = fix_line_decl(lines[idx], wid)
            if new is not None:
                lines[idx] = new
                done = True
            elif wid == "CS8625":
                m = re.match(r"^\s*(?:this\.)?(\w+)\s*=\s*null\s*;", lines[idx])
                if m:
                    r = find_decl_line(lines, m.group(1))
                    if r:
                        i, mm = r
                        q = addq(mm.group(1))
                        if q:
                            s = lines[i]
                            k = s.index(mm.group(1))
                            lines[i] = s[:k] + q + s[k + len(mm.group(1)):]
                            done = True
                if not done:
                    # パラメーターの既定値: 型 名 = null（同じ行）
                    m2 = re.search(r"(" + TYPE + r")(\s+\w+\s*=\s*null\b)", lines[idx])
                    if m2:
                        q = addq(m2.group(1))
                        if q:
                            lines[idx] = lines[idx][:m2.start(1)] + q + lines[idx][m2.end(1):]
                            done = True
        elif wid == "CS8603":
            # 囲む宣言（メソッド・プロパティ）を上へ探す
            sig = re.compile(r"^\s*" + MODS + r"(" + TYPE + r")\s+(\w+)\s*(?:<[^>]*>)?\s*(\(|\{|=>)")
            for j in range(idx, max(-1, idx - 80), -1):
                m = sig.match(lines[j])
                if m and m.group(1) not in ("return", "else", "new", "if", "while", "foreach", "for", "switch", "using", "lock", "catch", "await", "var"):
                    q = addq(m.group(1))
                    if q and "?" not in lines[j].split(m.group(2))[0]:
                        s = lines[j]
                        k = s.index(m.group(1))
                        lines[j] = s[:k] + q + s[k + len(m.group(1)):]
                        done = True
                    break
        if done:
            fixed += 1
        else:
            skipped.append((path, line, wid))
    for path, (lines, bom) in files.items():
        write(path, "\n".join(lines), bom)
    print("fixed", fixed, "skipped", len(skipped), "files", len(files))
    for p, l, w in skipped[:400]:
        print("SKIP", p[p.index(BS + "DS4Windows" + BS) + 12:].replace(BS, "/"), l, w)


if __name__ == "__main__":
    main()
