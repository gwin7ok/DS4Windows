#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
コードレビューの定量指標を、ソースから機械的に数える（読むだけ。ファイルは変更しない）。

目的: docs-forDIMG/Code-Review-2026-11-19/ の報告書の数値を、あとから同じ手順で取り直せるようにする。
      PhaseRV1〜RV8 の完了のたびに実行し、報告書 §3 の「指標の一覧」と比べる。
使い方（リポジトリのルートで）:
    python -I scripts/audit/Measure-CodeReview.py            # 全指標
    python -I scripts/audit/Measure-CodeReview.py --only mvvm,catch
指標の名前: size, mvvm, catch, comments, iface, unref, clone, cloneexact, fields, naming, tests, di
注意: 正規表現とトークンの数え上げによる近似（構文解析ではない）。件数は ±数件の誤差がありうる。
      比べるときは、同じスクリプトの前後の値を使う。
"""
import argparse
import hashlib
import os
import re
import sys
from collections import Counter, defaultdict

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "DS4Windows")
TESTS = os.path.join(ROOT, "DS4WindowsTests")
SKIP_DIRS = {"obj", "bin", "libs"}


def walk(base, exts):
    for dp, dn, fn in os.walk(base):
        dn[:] = [d for d in dn if d not in SKIP_DIRS]
        for f in fn:
            if any(f.endswith(e) for e in exts):
                yield os.path.join(dp, f)


def read(p):
    return open(p, encoding="utf-8-sig", errors="ignore").read()


def strip_comments(t):
    t = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), t, flags=re.S)
    return re.sub(r"//[^\n]*", "", t)


def src_files():
    for p in walk(SRC, [".cs"]):
        if not p.endswith(".Designer.cs"):
            yield p


def rel(p):
    return os.path.relpath(p, ROOT).replace("\\", "/")


def layer(p):
    parts = rel(p).split("/")
    if len(parts) < 3:
        return "(root)"
    return parts[1] if parts[1] in ("Input", "Processing", "Output", "UI", "Composition", "Core") else "(root)"


# ---------------------------------------------------------------------------
def m_size():
    print("## size（層ごとの規模）")
    lines, files = Counter(), Counter()
    big = []
    for p in src_files():
        n = read(p).count("\n") + 1
        lines[layer(p)] += n
        files[layer(p)] += 1
        big.append((n, rel(p)))
    for k in ("Input", "Processing", "Output", "UI", "Composition", "Core", "(root)"):
        print(f"- {k}: {files[k]} ファイル / {lines[k]} 行")
    print(f"- 合計: {sum(files.values())} ファイル / {sum(lines.values())} 行")
    big.sort(reverse=True)
    print("- 行数の上位 12:")
    for n, p in big[:12]:
        print(f"    {n:5d}  {p}")
    over500 = sum(1 for n, _ in big if n >= 500)
    print(f"- 500 行以上のファイル: {over500} 件")


def m_mvvm():
    print("## mvvm（画面の結びつけ）")
    xaml = list(walk(SRC, [".xaml"]))
    txt = {p: read(p) for p in xaml}
    cmd = sum(len(re.findall(r'\bCommand="\{Binding', t)) for t in txt.values())
    click = sum(len(re.findall(r'\sClick="', t)) for t in txt.values())
    other = sum(len(re.findall(
        r'\s(SelectionChanged|MouseDown|MouseUp|MouseLeftButtonDown|MouseLeftButtonUp|MouseRightButtonUp|MouseMove|MouseEnter|MouseLeave|'
        r'Checked|Unchecked|TextChanged|ValueChanged|PreviewMouse\w+|PreviewKey\w+|KeyDown|KeyUp|LostFocus|GotFocus|Loaded|Unloaded|Drop|DragOver|'
        r'MouseDoubleClick|SizeChanged|Closed|Closing|ContextMenuOpening|ToolTipOpening)="', t)) for t in txt.values())
    binding = sum(len(re.findall(r"\{Binding", t)) for t in txt.values())
    xname = sum(len(re.findall(r'x:Name="', t)) for t in txt.values())
    print(f"- XAML: Command バインディング {cmd} / Click 属性 {click} / その他のイベント属性 {other} / {{Binding}} {binding} / x:Name {xname}")
    cb = [p for p in src_files() if p.endswith(".xaml.cs")]
    cb_lines = sum(read(p).count("\n") + 1 for p in cb)
    vm_lines = sum(read(p).count("\n") + 1 for p in src_files() if "/UI/ViewModels/" in rel(p))
    handlers = 0
    for p in cb:
        handlers += len(re.findall(r"void\s+\w+\(object\s+\w+,\s*\w+\s+\w+\)", read(p)))
    print(f"- code-behind(.xaml.cs): {len(cb)} ファイル / {cb_lines} 行 / イベントハンドラー型のメソッド {handlers} 件。ViewModels: {vm_lines} 行")
    rc = 0
    for p in src_files():
        if "/UI/ViewModels/Util/RelayCommand.cs" in rel(p):
            continue
        if re.search(r"\bRelayCommand\b", read(p)):
            rc += 1
    print(f"- RelayCommand を使うファイル（定義を除く）: {rc}")
    sets = Counter()
    for p in cb:
        t = strip_comments(read(p))
        for name, pat in (("IsEnabled", r"\.IsEnabled\s*=[^=]"), ("Visibility", r"\.Visibility\s*=[^=]"), ("Content", r"\.Content\s*=[^=]"),
                          ("ItemsSource", r"\.ItemsSource\s*=[^=]"), ("SelectedIndex", r"\.SelectedIndex\s*=[^=]")):
            sets[name] += len(re.findall(pat, t))
    print("- code-behind が部品へ直接代入: " + " / ".join(f"{k} {v}" for k, v in sets.items()))
    plus = 0
    for p in cb:
        plus += len(re.findall(r"\.\w+\s*\+=\s*\w+\s*;", strip_comments(read(p))))
    print(f"- code-behind の `+= メソッド名;`（イベントの手結び）: {plus}")
    inpc = Counter()
    for p in src_files():
        t = strip_comments(read(p))
        if re.search(r"class\s+\w+\s*:\s*[^{]*\bObservableObject\b", t):
            inpc["ObservableObject 継承"] += 1
        if re.search(r"class\s+\w+\s*:\s*[^{]*\bINotifyPropertyChanged\b", t):
            inpc["INotifyPropertyChanged 直接実装"] += 1
    print("- 変更通知: " + " / ".join(f"{k} {v}" for k, v in inpc.items()))


def m_catch():
    print("## catch（握りつぶしと防御コード）")
    tot = empty = 0
    byl = Counter()
    toplist = Counter()
    ml = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}")
    for p in src_files():
        t = strip_comments(read(p))
        tot += len(re.findall(r"\bcatch\b", t))
        n = len(ml.findall(t))
        empty += n
        byl[layer(p)] += n
        if n:
            toplist[rel(p)] = n
    print(f"- catch 全体 {tot} 件のうち、本体が空（コメントのみを含む）: {empty} 件（{100.0 * empty / max(tot, 1):.0f}%）")
    print("- 空の catch の層別: " + " / ".join(f"{k} {v}" for k, v in byl.most_common()))
    print("- 上位ファイル: " + ", ".join(f"{k.split('/')[-1]}={v}" for k, v in toplist.most_common(8)))
    wrap = 0
    for p in src_files():
        for line in read(p).split("\n"):
            if "AppLogger" in line and re.search(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}", line):
                wrap += 1
    print(f"- ログ呼び出しを try/catch{{}} で包む行: {wrap}")
    av = su = th = 0
    for p in src_files():
        t = strip_comments(read(p))
        av += len(re.findall(r"\basync\s+void\b", t))
        su += len(re.findall(r"\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)", t))
        th += len(re.findall(r"Thread\.Sleep", t))
    print(f"- async void {av} / .Result・.Wait() {su} / Thread.Sleep {th}")
    nn = sum(len(re.findall(r"\?\?\s*throw\s+new\s+ArgumentNullException", read(p))) for p in src_files())
    print(f"- `?? throw new ArgumentNullException` {nn} 件")


def m_comments():
    print("## comments（コメントに残る経緯）")
    pf = pl = old = tot_files = 0
    for p in src_files():
        t = read(p)
        tot_files += 1
        k = len(re.findall(r"Phase\d", t))
        if k:
            pf += 1
            pl += sum(1 for ln in t.split("\n") if re.search(r"Phase\d", ln))
        old += sum(1 for ln in t.split("\n") if re.search(r"//.*旧", ln))
    print(f"- 'Phase<数字>' を含む行 {pl} 件 / 含むファイル {pf} / 全 {tot_files}（{100.0 * pf / tot_files:.0f}%）")
    print(f"- `//` コメントに「旧」を含む行 {old}")
    todo = sum(len(re.findall(r"TODO|FIXME|HACK", read(p))) for p in src_files())
    print(f"- TODO/FIXME/HACK {todo}")


def collect_ifaces():
    src = {p: strip_comments(read(p)) for p in src_files()}
    tst = {p: strip_comments(read(p)) for p in walk(TESTS, [".cs"])}
    iface_re = re.compile(r"\binterface\s+(I[A-Z]\w*)")
    class_re = re.compile(r"\b(?:class|struct|record)\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*([^{]+?)\s*(?:where[^{]*)?\{")
    ifaces = {}
    for p, t in src.items():
        for m in iface_re.finditer(t):
            ifaces[m.group(1)] = p
    impl_src, impl_tst = defaultdict(list), defaultdict(list)
    for store, d in ((src, impl_src), (tst, impl_tst)):
        for p, t in store.items():
            for m in class_re.finditer(t):
                for b in re.split(r",(?![^<]*>)", m.group(2)):
                    bn = re.sub(r"<.*", "", b).strip()
                    if bn in ifaces:
                        d[bn].append(m.group(1))
    return ifaces, impl_src, impl_tst


def m_iface():
    print("## iface（インターフェースの実装数）")
    ifaces, impl_src, impl_tst = collect_ifaces()
    multi = sum(1 for i in ifaces if len(impl_src.get(i, [])) >= 2)
    single_fake = [i for i in ifaces if len(impl_src.get(i, [])) == 1 and impl_tst.get(i)]
    single_nofake = [i for i in ifaces if len(impl_src.get(i, [])) == 1 and not impl_tst.get(i)]
    zero = [i for i in ifaces if len(impl_src.get(i, [])) == 0]
    print(f"- 全 {len(ifaces)} 件: 実装 2 以上 {multi} / 実装 1 で偽物あり {len(single_fake)} / 実装 1 で偽物なし {len(single_nofake)} / 実装 0（継承のみ・工場の契約など）{len(zero)}")
    byf = Counter(rel(ifaces[i]).split("/")[1] + "/" + rel(ifaces[i]).split("/")[2] for i in single_nofake)
    print("- 実装 1・偽物なしの置き場所: " + " / ".join(f"{k} {v}" for k, v in byf.most_common()))


def m_unref():
    print("## unref（ほかから参照されない型）")
    decl = re.compile(r"\b(class|interface|struct|enum|record)\s+([A-Za-z_]\w*)")
    tok = re.compile(r"[A-Za-z_]\w*")
    files = {p: strip_comments(read(p)) for p in src_files()}
    xt = "\n".join(read(p) for p in walk(SRC, [".xaml"]))
    xtoks = Counter(tok.findall(xt))
    toks = {p: Counter(tok.findall(t)) for p, t in files.items()}
    ttoks = {p: Counter(tok.findall(strip_comments(read(p)))) for p in walk(TESTS, [".cs"])}
    types = {}
    where = defaultdict(set)
    for p, t in files.items():
        for m in decl.finditer(t):
            types.setdefault(m.group(2), m.group(1))
            where[m.group(2)].add(p)
    none, testonly = [], []
    for name, kind in types.items():
        refs = sum(1 for q, c in toks.items() if q not in where[name] and c.get(name, 0) > 0)
        if refs or xtoks.get(name, 0):
            continue
        t_refs = sum(1 for c in ttoks.values() if c.get(name, 0) > 0)
        (testonly if t_refs else none).append((name, kind, rel(next(iter(where[name])))))
    interop = re.compile(r"NativeMethods|Win32WindowSystem|LowLevelMouseHook|SendInputHandler|XInputNativeApi|XusbInterfaceSource|Util\.cs|WindowPlacementHelper|DS4_REPORT")
    plain = [x for x in none if not interop.search(x[2]) and x[1] in ("class", "enum", "interface")]
    print(f"- 型 {len(types)} 件のうち、自分のファイル以外から参照されない: {len(none)} 件（うち P/Invoke 構造体などを除く class/enum: {len(plain)} 件）、テストからだけ参照: {len(testonly)} 件")
    for n, k, p in sorted(plain, key=lambda x: x[2]):
        print(f"    {k:9s} {n}  ({p})")


def m_clone(exact=False):
    WIN = 10 if exact else 14
    print("## clone（同一の重複。10 行の窓、正規化なし）" if exact else "## clone（構造の重複。識別子・数値・文字列を正規化した 14 行の窓）")
    str_re = re.compile(r'"(?:[^"\\]|\\.)*"')
    num_re = re.compile(r"\b(?:0x[0-9A-Fa-f]+|\d+(?:\.\d+)?)[fFdDuUlL]*\b")
    kw = ("if|else|for|foreach|while|return|new|var|int|bool|string|double|float|byte|void|null|true|false|this|try|catch|finally|"
          "throw|switch|case|break|continue|public|private|protected|internal|static|readonly|const|class|using|lock|out|ref|in|is|as|"
          "get|set|ushort|uint|long|ulong|short|object|default|await|async|do|S|N")
    id_re = re.compile(r"\b(?!(?:" + kw + r")\b)[A-Za-z_]\w*\b")
    trivial = re.compile(r"^(\{|\}|\};?|\)|\);|\]|else|try|break;|return;|\[.*\]|using .*;|#region.*|#endregion.*|namespace .*)$")
    files, total = {}, 0
    for p in src_files():
        if p.endswith(".g.cs") or p.endswith(".g.i.cs"):
            continue
        txt = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), read(p), flags=re.S)
        ls = []
        for ln in txt.split("\n"):
            s = re.sub(r"\s+", " ", re.sub(r"//.*$", "", ln).strip())
            if not exact:
                s = id_re.sub("I", num_re.sub("N", str_re.sub("S", s)))
            if s and not trivial.match(s):
                ls.append(s)
        files[p] = ls
        total += len(ls)
    idx = defaultdict(list)
    for p, ls in files.items():
        for k in range(0, len(ls) - WIN + 1):
            idx[hashlib.md5("\n".join(ls[k:k + WIN]).encode()).hexdigest()].append((p, k))
    marks = defaultdict(set)
    for occ in idx.values():
        if len(occ) >= 2:
            for p, k in occ:
                marks[p].update(range(k, k + WIN))
    dup = sum(len(v) for v in marks.values())
    print(f"- 意味のある行 {total} 件のうち、他と同じ形の窓に入る行: {dup} 件（{100.0 * dup / total:.1f}%）。表・宣言の並び（ProfilePresets、DS4ControlNames など）を多く含む")
    rows = sorted(((len(v), len(files[p]), rel(p)) for p, v in marks.items()), reverse=True)
    print("- 重複の多いファイル（重複行/行、%）:")
    for d, n, p in rows[:14]:
        print(f"    {d:4d}/{n:4d} ({100 * d // max(n, 1):3d}%)  {p}")


def m_fields():
    print("## fields（公開の可変フィールド）")
    pub_field = re.compile(r"^\s*public\s+(?!static|const|class|interface|enum|struct|event|delegate|override|virtual|abstract|sealed|partial|readonly\s+static|new\b)"
                           r"[\w<>\[\],\.\? ]+?\s+\w+\s*(?:=[^>]|;)", re.M)
    rows, tot = [], 0
    for p in src_files():
        t = strip_comments(read(p))
        n = len(pub_field.findall(t))
        tot += n
        if n >= 15:
            rows.append((n, rel(p)))
    print(f"- public の可変フィールド 合計 {tot} 件。15 件以上のファイル:")
    for n, p in sorted(rows, reverse=True):
        print(f"    {n:4d}  {p}")


def m_naming():
    print("## naming（命名・採用手段のばらつき）")
    pat = re.compile(r"^\s*(?:public|protected|internal)\s+(?:(?:static|virtual|override|abstract|sealed|async|new|unsafe)\s+)*[A-Za-z_][\w<>\[\]\?\.,\s]*?\s+([a-z]\w*)\s*\(", re.M)
    skip = {"if", "for", "while", "switch", "lock", "using", "foreach", "catch", "return", "new", "nameof", "typeof", "get", "set"}
    tot, byf = 0, Counter()
    for p in src_files():
        n = len([x for x in pat.findall(strip_comments(read(p))) if x not in skip])
        tot += n
        if n:
            byf[rel(p).split("/")[-1]] += n
    print(f"- public/protected/internal で小文字始まりのメソッド: {tot} 件。上位: " + ", ".join(f"{k}={v}" for k, v in byf.most_common(5)))
    # NonFormTimer は System.Timers.Timer の別名（`using NonFormTimer = System.Timers.Timer;`）。別種として数えない。
    # `new Timer(` は System.Threading.Timer（System.Threading を using しているファイルのもの）として数える。
    kinds = {
        "System.Timers.Timer（別名 NonFormTimer を含む）": r"new\s+(?:NonFormTimer|System\.Timers\.Timer)\s*\(",
        "System.Threading.Timer": r"new\s+(?:System\.Threading\.)?Timer\s*\(",
        "DispatcherTimer": r"new\s+(?:System\.Windows\.Threading\.)?DispatcherTimer\b",
        "TimeProvider.CreateTimer": r"\.CreateTimer\s*\(",
    }
    c = Counter()
    alias_lines = 0
    for p in src_files():
        t = strip_comments(read(p))
        alias_lines += len(re.findall(r"using\s+NonFormTimer\s*=\s*System\.Timers\.Timer\s*;", t))
        for k, pt in kinds.items():
            c[k] += len(re.findall(pt, t))
    print("- タイマーの生成（new の数）: " + " / ".join(f"{k} {v}" for k, v in c.items())
          + f"。別名 `using NonFormTimer = …` の行 {alias_lines}")


def m_tests():
    print("## tests（テストの構成）")
    files = list(walk(TESTS, [".cs"]))
    facts = theories = 0
    srctext = guards = arch = 0
    for p in files:
        t = read(p)
        facts += len(re.findall(r"\[Fact", t))
        theories += len(re.findall(r"\[Theory", t))
        if re.search(r"SourceFileLocator|SourceText\.|ReadAllText\([^)]*\.cs", t):
            srctext += 1
        if re.search(r"Guard|Ratchet|Rule|Baseline", os.path.basename(p)):
            guards += 1
        if "ArchUnitNET" in t:
            arch += 1
    src_lines = sum(read(p).count("\n") + 1 for p in src_files())
    tst_lines = sum(read(p).count("\n") + 1 for p in files)
    print(f"- テストのファイル {len(files)} / [Fact] {facts} / [Theory] {theories} / 行数 {tst_lines}（本体 {src_lines} 行の {100.0 * tst_lines / src_lines:.0f}%）")
    print(f"- ソースの文字列を読むテスト {srctext} ファイル / 名前に Guard・Ratchet・Rule・Baseline を持つ {guards} ファイル / ArchUnitNET を使う {arch} ファイル")
    phase_named = sum(1 for p in files if re.search(r"^\s*///.*Phase\d", read(p), re.M))
    print(f"- XML コメントに Phase 名を持つテストのファイル: {phase_named}")


def m_di():
    print("## di（注入・静的な状態）")
    sl = 0
    for p in src_files():
        r = rel(p)
        if "/Composition/" in r or r.endswith("DS4Windows/App.xaml.cs"):
            continue
        sl += len(re.findall(r"GetRequiredService|GetService<|GetService\(|AppHost\.GetService", strip_comments(read(p))))
    app = strip_comments(read(os.path.join(SRC, "App.xaml.cs")))
    print(f"- Composition・App 以外での Service Locator 呼び出し: {sl}")
    lines = read(os.path.join(SRC, "App.xaml.cs")).count("\n") + 1
    rps = len(re.findall(r"ResolvePostHostService", app))
    ch = len(re.findall(r"AppHost\.CreateHost", app))
    news = len(re.findall(r"\bnew\s+[A-Z]", app))
    print(f"- App.xaml.cs: {lines} 行 / ResolvePostHostService {rps} 件 / AppHost.CreateHost {ch} 件 / `new 型名` {news} 件")
    st = re.compile(r"^\s*(?:public|internal|private|protected)?\s*static\s+(?!readonly|class|void|async|extern|partial|implicit|explicit)"
                    r"[\w<>\[\],\.\? ]+\s+\w+\s*(?:=[^>=]|;|\{\s*get)", re.M)
    n = sum(len(st.findall(strip_comments(read(p)))) for p in src_files())
    print(f"- static の可変フィールド・自動プロパティ: {n} 件（多くは表・キャッシュ。状態を持つ主なもの: AppLogger、PathService、EnvironmentService）")
    wi = Counter()
    for p in src_files():
        if layer(p) in ("Core", "Input", "Output", "Processing") and re.search(r"^using System\.Windows(\.\w+)*;", read(p), re.M):
            wi[layer(p)] += 1
    print("- UI 以外で `using System.Windows*;` を持つファイル（雛形の残りが多い）: " + " / ".join(f"{k} {v}" for k, v in wi.items()))


def m_cloneexact():
    m_clone(exact=True)


ALL = {"size": m_size, "mvvm": m_mvvm, "catch": m_catch, "comments": m_comments, "iface": m_iface, "unref": m_unref,
       "clone": m_clone, "cloneexact": m_cloneexact, "fields": m_fields, "naming": m_naming, "tests": m_tests, "di": m_di}

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="", help="カンマ区切りの指標名（省略時は全部）")
    a = ap.parse_args()
    names = [x for x in a.only.split(",") if x] or list(ALL)
    for n in names:
        ALL[n]()
        print()
