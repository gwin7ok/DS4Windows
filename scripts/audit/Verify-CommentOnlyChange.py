#!/usr/bin/env python3
"""コメントと空白だけの変更かを検査する（PhaseRV8）。

使い方:
  python -I scripts/audit/Verify-CommentOnlyChange.py [--base REV] [--self-test] [パス...]
  既定: --base HEAD と作業ツリー（ステージ済みを含む）を比べる。--base HEAD~1 で直前のコミットとの差も検査できる
  （--head REV を付けると、作業ツリーの代わりにそのコミットと比べる）。
対象: .cs（// と /* */ を除く）、.xaml（<!-- --> を除く）。空白はすべて除いて比べる。
文字列・文字リテラル・逐語的文字列・生文字列・補間文字列の中の // は、コメントとして扱わない。
終了コード: 0 = 同じ、1 = 構文が違う、2 = 使い方の誤り
"""
import argparse, re, subprocess, sys


def strip_cs(src: str) -> str:
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        two = src[i:i + 2]
        if two == "//":
            j = src.find("\n", i)
            i = n if j < 0 else j
            continue
        if two == "/*":
            j = src.find("*/", i + 2)
            i = n if j < 0 else j + 2
            out.append(" ")
            continue
        # 生文字列 """..."""
        m = re.match(r'\$*"{3,}', src[i:])
        if m:
            q = m.group(0).lstrip("$")
            j = src.find(q, i + len(m.group(0)))
            j = n if j < 0 else j + len(q)
            out.append(src[i:j]); i = j
            continue
        # 逐語的・補間文字列の接頭辞
        m = re.match(r'(\$@|@\$|@|\$)"', src[i:])
        if m:
            verbatim = "@" in m.group(1)
            j = i + len(m.group(0))
            while j < n:
                if verbatim and src[j] == '"':
                    if src[j + 1:j + 2] == '"':
                        j += 2; continue
                    j += 1; break
                if not verbatim and src[j] == "\\":
                    j += 2; continue
                if not verbatim and src[j] == '"':
                    j += 1; break
                j += 1
            out.append(src[i:j]); i = j
            continue
        if c == '"':
            j = i + 1
            while j < n and src[j] != '"':
                j += 2 if src[j] == "\\" else 1
            out.append(src[i:j + 1]); i = j + 1
            continue
        if c == "'":
            j = i + 1
            while j < n and src[j] != "'":
                j += 2 if src[j] == "\\" else 1
            out.append(src[i:j + 1]); i = j + 1
            continue
        out.append(c); i += 1
    return re.sub(r"\s+", "", "".join(out))


def strip_xaml(src: str) -> str:
    return re.sub(r"\s+", "", re.sub(r"<!--.*?-->", "", src, flags=re.S))


def normalize(path: str, text: str) -> str:
    if path.endswith(".cs"):
        return strip_cs(text.lstrip("﻿"))
    if path.endswith(".xaml"):
        return strip_xaml(text.lstrip("﻿"))
    return re.sub(r"\s+", "", text)


def git(*args: str) -> bytes:
    return subprocess.run(["git", *args], check=True, capture_output=True).stdout


def self_test() -> int:
    a = 'int x = 1; // 経緯\nstring s = "a // b";\n/* c */ var y = @"p""//q";'
    b = 'int x = 1;\nstring s = "a // b";\nvar y = @"p""//q";'
    assert strip_cs(a) == strip_cs(b), "コメントだけの差を、同じと判定できない"
    c = a.replace("int x = 1", "int x = 2")
    assert strip_cs(a) != strip_cs(c), "コードの変更を検出できない"
    d = a.replace('"a // b"', '"a // c"')
    assert strip_cs(a) != strip_cs(d), "文字列の変更を検出できない"
    assert strip_xaml("<A><!-- x --></A>") == strip_xaml("<A/>".replace("/>", "></A>")) , "XAML のコメント除去"
    print("self-test OK")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="HEAD")
    ap.add_argument("--head", default=None)
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("paths", nargs="*")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    diff_args = ["diff", "--name-only", a.base] + ([a.head] if a.head else []) + (["--", *a.paths] if a.paths else [])
    files = [f for f in git(*diff_args).decode("utf-8").splitlines() if f.endswith((".cs", ".xaml"))]
    bad = 0
    for f in files:
        try:
            old = git("show", f"{a.base}:{f}").decode("utf-8")
        except subprocess.CalledProcessError:
            old = ""
        try:
            new = git("show", f"{a.head}:{f}").decode("utf-8") if a.head else open(f, encoding="utf-8-sig").read()
        except (subprocess.CalledProcessError, FileNotFoundError):
            new = ""
        if normalize(f, old) != normalize(f, new):
            print("構文が違う:", f); bad += 1
    print(f"検査 {len(files)} ファイル、違い {bad}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
