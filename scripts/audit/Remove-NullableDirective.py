"""
ファイルごとの `#nullable enable` を消す（PhaseRV9-13。プロジェクト全体の <Nullable>enable</Nullable> へ切り替えたあと）。
BOM・改行を保つ。何度流しても同じ。`--verify` は、ステージ済みの差分が「指令の行（と、それが作った空行）の削除だけ」かを検査する。

使い方（リポジトリのルートで）:
  python -I scripts/audit/Remove-NullableDirective.py DS4Windows
  python -I scripts/audit/Remove-NullableDirective.py --verify
"""
import os
import subprocess
import sys

DIRECTIVE = "#nullable enable"
SKIP_DIRS = {"obj", "bin"}


def iter_files(paths):
    for p in paths:
        for d, dirs, fs in os.walk(p):
            dirs[:] = [x for x in dirs if x not in SKIP_DIRS]
            for f in fs:
                if f.endswith(".cs"):
                    yield os.path.join(d, f)


def remove(path):
    raw = open(path, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = (raw[3:] if bom else raw).decode("utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(nl)
    idx = [i for i, l in enumerate(lines[:40]) if l.strip() == DIRECTIVE]
    if not idx:
        return False
    i = idx[0]
    del lines[i]
    # 指令を足したときに入れた空行（直前が空行か、ファイルの先頭のとき）を、あわせて消す
    if i < len(lines) and lines[i].strip() == "" and (i == 0 or lines[i - 1].strip() == ""):
        del lines[i]
    out = nl.join(lines).encode("utf-8")
    if bom:
        out = b"\xef\xbb\xbf" + out
    open(path, "wb").write(out)
    return True


def verify():
    sys.stdout.reconfigure(encoding="utf-8")
    diff = subprocess.run(["git", "diff", "--cached", "-U0", "--no-color", "--", "*.cs"], capture_output=True, text=True, encoding="utf-8").stdout
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
        for m in minus:
            if m in plus:
                plus.remove(m)
            elif m.strip() not in ("", DIRECTIVE):
                bad.append((f, "-" + m))
        for p in plus:
            bad.append((f, "+" + p))
    if bad:
        for f, l in bad[:20]:
            print("NG", f, l)
        print("指令の削除以外の差分:", len(bad))
        return 2
    print("ok: 差分は指令（と空行）の削除だけ")
    return 0


def main():
    if "--verify" in sys.argv:
        sys.exit(verify())
    n = sum(1 for f in iter_files(sys.argv[1:]) if remove(f))
    print("removed", n)


if __name__ == "__main__":
    main()
