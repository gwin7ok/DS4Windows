"""
ファイルの先頭へ `#nullable enable` を足す（PhaseRV9）。BOM と改行（CRLF/LF）を保つ。何度流しても同じ（すでにあれば何もしない）。

使い方（リポジトリのルートで）:
  python -I scripts/audit/Add-NullableDirective.py DS4Windows/Input DS4Windows/Output     # 足す
  python -I scripts/audit/Add-NullableDirective.py --verify                                # ステージ済みの差分が「指令の追加だけ」か検査
位置: ファイル先頭の `//` コメント・空行（ライセンスなど）の後、最初のコードの前。直後に空行を 1 つ入れる。
対象外: obj・bin・自動生成（*.g.cs、*.g.i.cs、*.designer.cs、AssemblyInfo）。
"""
import os
import subprocess
import sys

DIRECTIVE = "#nullable enable"
SKIP_DIRS = {"obj", "bin"}
SKIP_SUFFIX = (".g.cs", ".g.i.cs", ".designer.cs", ".assemblyinfo.cs")


def iter_files(paths):
    for p in paths:
        if os.path.isfile(p):
            yield p
            continue
        for d, dirs, fs in os.walk(p):
            dirs[:] = [x for x in dirs if x not in SKIP_DIRS]
            for f in fs:
                if f.endswith(".cs") and not f.lower().endswith(SKIP_SUFFIX):
                    yield os.path.join(d, f)


def add(path):
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = (raw[3:] if bom else raw).decode("utf-8")
    if DIRECTIVE in text:
        # 指令が先頭付近にあれば済み（#nullable disable などの別指令も尊重して触らない）
        return False
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(nl)
    i = 0
    in_block = False
    while i < len(lines):
        s = lines[i].strip()
        if in_block:
            if "*/" in s:
                in_block = False
            i += 1
            continue
        if s == "" or s.startswith("//"):
            i += 1
        elif s.startswith("/*"):
            in_block = "*/" not in s
            i += 1
        else:
            break
    head = lines[:i]
    rest = lines[i:]
    insert = [DIRECTIVE]
    if rest and rest[0].strip() != "":
        insert.append("")
    elif not head:
        pass
    new = head + insert + rest
    out = nl.join(new).encode("utf-8")
    if bom:
        out = b"\xef\xbb\xbf" + out
    open(path, "wb").write(out)
    return True


def verify():
    sys.stdout.reconfigure(encoding="utf-8")
    diff = subprocess.run(["git", "diff", "--cached", "-U0", "--no-color"], capture_output=True, text=True, encoding="utf-8").stdout
    bom = chr(0xFEFF)
    files = {}
    cur = None
    for ln in diff.splitlines():
        if ln.startswith("+++ "):
            cur = ln[4:]
            files.setdefault(cur, ([], []))
        elif ln.startswith(("--- ", "diff ", "index ", "@@", "new file", "deleted file", "similarity", "rename")):
            continue
        elif ln.startswith("-"):
            files[cur][0].append(ln[1:].replace(bom, ""))
        elif ln.startswith("+"):
            files[cur][1].append(ln[1:].replace(bom, ""))
    bad = []
    for f, (minus, plus) in files.items():
        # BOM が先頭行から指令の行へ移るため、先頭行の「消して同じ内容を足す」は同じ内容とみなす
        for m in minus:
            if m in plus:
                plus.remove(m)
            else:
                bad.append((f, "-" + m))
        for p in plus:
            if p.strip() not in ("", DIRECTIVE):
                bad.append((f, "+" + p))
    if bad:
        for f, l in bad[:20]:
            print("NG", f, l)
        print("指令の追加以外の差分:", len(bad))
        return 2
    print("ok: 差分は指令の追加（と空行）だけ")
    return 0


def main():
    if "--verify" in sys.argv:
        sys.exit(verify())
    n = 0
    for f in iter_files(sys.argv[1:]):
        if add(f):
            n += 1
    print("added", n)


if __name__ == "__main__":
    main()
