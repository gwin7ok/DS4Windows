#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
コードレビューの 9 観点の評価（A〜D）を、固定の採点基準（docs-forDIMG/Code-Review-2026-11-19/03-Rubric.md）で算定する。
読むだけ。ソースは変更しない。

仕組み:
  1. Measure-CodeReview.py の各指標を取り込み、その出力から数字を拾う（指標の数え方は 1 か所に保つ）。
  2. このスクリプト自身が数える値（ラチェットの基準の件数、設定ファイル、文書の数）を足す。
  3. 手で記録する値と、はい／いいえの確認項目を scripts/audit/Rubric-Inputs.json から読む。
  4. 指標ごとに「目標値（満点）」と「下限値（0 点）」の間を直線で 0〜1 に換算し、重みで平均して観点の点数にし、
     固定の等級表で A〜D に直す。

使い方（リポジトリのルートで）:
    python -I scripts/audit/Score-CodeReview.py                     # 採点して表示
    python -I scripts/audit/Score-CodeReview.py --json OUT.json     # 結果を保存（Phase の最終 Step で使う）
    python -I scripts/audit/Score-CodeReview.py --diff PREV.json    # 前回の保存との差を表示
    python -I scripts/audit/Score-CodeReview.py --show-rubric       # 採点基準の表を Markdown で出す（03-Rubric.md の表の元）
    python -I scripts/audit/Score-CodeReview.py --calibration       # 基準の確認: 2026-11-19 の評価を再現できるか

採点基準（RUBRIC_VERSION）の目標値・下限値・重み・等級表は、承認のあとは動かさない。直すときは版を上げ、
過去の保存をすべて新しい版で採点し直して比べられるようにする（03-Rubric.md §6）。
"""
import argparse
import contextlib
import datetime
import importlib.util
import io
import json
import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(ROOT, "DS4Windows")
TESTS = os.path.join(ROOT, "DS4WindowsTests")

RUBRIC_VERSION = "v1"

# 等級表（点数 0〜1 → 等級）。上から順に最初に満たしたもの。どれにも当たらなければ D
BANDS = [(0.90, "A"), (0.83, "A−"), (0.76, "B+"), (0.69, "B"), (0.62, "B−"), (0.55, "C+"), (0.48, "C"), (0.41, "C−")]
GRADE_ORDER = ["D", "C−", "C", "C+", "B−", "B", "B+", "A−", "A"]

# 2026-11-19 の報告書 §1.2 の評価（--calibration の照合先）
REPORT_GRADES = {
    "Pure DI 化": "A−", "各層の役割分担": "B+", "MVVM 原則遵守の割合": "C+", "オブジェクト指向": "B−", "共有部品化": "B",
    "冗長処理": "C", "採用手段の統一": "C+", "メンテナンス性": "B−", "機能追加の容易化": "B",
}


def band(score):
    for lo, g in BANDS:
        if score >= lo:
            return g
    return "D"


# ---------------------------------------------------------------------------
# 1. 計測値を集める
# ---------------------------------------------------------------------------
def load_measure():
    # 一覧から個別のファイルの値を拾うため、一覧を最長にして取り込む
    os.environ["MEASURE_CLONE_TOP"] = "100000"
    os.environ["MEASURE_FIELDS_MIN"] = "1"
    path = os.path.join(HERE, "Measure-CodeReview.py")
    spec = importlib.util.spec_from_file_location("measure_code_review", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def capture(fn, *a):
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        fn(*a)
    return buf.getvalue()


def grab(text, pat, name, cast=int, default=None):
    m = re.search(pat, text)
    if not m:
        if default is not None:
            return default
        raise SystemExit(f"[Score] 計測の出力から '{name}' を読めない（Measure-CodeReview.py の出力の書式が変わった？）: {pat}")
    return cast(m.group(1))


def count_entries(path):
    """ラチェットの基準ファイルの Entries の件数（"..." だけの行）。"""
    t = open(path, encoding="utf-8-sig", errors="ignore").read()
    t = re.sub(r"/\*.*?\*/", "", t, flags=re.S)
    t = re.sub(r"//[^\n]*", "", t)
    return len(re.findall(r'^\s*"[^"\n]*"\s*,?\s*$', t, flags=re.M))


def read_text(path):
    try:
        return open(path, encoding="utf-8-sig", errors="ignore").read()
    except OSError:
        return ""


def collect_metrics(m):
    out = {}
    size = capture(m.m_size)
    out["files_total"] = grab(size, r"合計: (\d+) ファイル", "files_total")
    out["lines_total"] = grab(size, r"合計: \d+ ファイル / (\d+) 行", "lines_total")
    out["big_files"] = grab(size, r"500 行以上のファイル: (\d+) 件", "big_files")

    mv = capture(m.m_mvvm)
    pat = r"Command バインディング (\d+) / Click 属性 (\d+) / その他のイベント属性 (\d+) / \{Binding\} (\d+) / x:Name (\d+)"
    for k, v in zip(("xaml_command", "xaml_click", "xaml_other_events", "xaml_binding", "xaml_xname"),
                    re.search(pat, mv).groups() if re.search(pat, mv) else (None,) * 5):
        if v is None:
            raise SystemExit("[Score] mvvm の XAML の行を読めない")
        out[k] = int(v)
    out["codebehind_lines"] = grab(mv, r"code-behind\(\.xaml\.cs\): \d+ ファイル / (\d+) 行", "codebehind_lines")
    out["viewmodel_lines"] = grab(mv, r"ViewModels: (\d+) 行", "viewmodel_lines")
    out["direct_assign"] = sum(int(x) for x in re.findall(
        r"(?:IsEnabled|Visibility|Content|ItemsSource|SelectedIndex) (\d+)", grab(mv, r"(code-behind が部品へ直接代入: .*)", "direct_assign", str)))
    out["handwire"] = grab(mv, r"手結び）: (\d+)", "handwire")
    out["inpc_direct"] = grab(mv, r"INotifyPropertyChanged 直接実装 (\d+)", "inpc_direct", default=0)
    out["observable_object"] = grab(mv, r"ObservableObject 継承 (\d+)", "observable_object", default=0)

    ct = capture(m.m_catch)
    out["catch_total"] = grab(ct, r"catch 全体 (\d+) 件", "catch_total")
    out["catch_empty"] = grab(ct, r"本体が空（コメントのみを含む）: (\d+) 件", "catch_empty")
    out["catch_bare"] = grab(ct, r"理由のコメントがない空の catch[^:]*: (\d+) 件", "catch_bare")
    out["log_wrap"] = grab(ct, r"包む行: (\d+)", "log_wrap")
    out["async_void"] = grab(ct, r"async void (\d+)", "async_void")
    out["async_void_nonhandler"] = grab(ct, r"async void \d+（うち[^）]*?以外 (\d+)）", "async_void_nonhandler")
    out["result_wait"] = grab(ct, r"\.Result・\.Wait\(\) (\d+)", "result_wait")
    out["thread_sleep"] = grab(ct, r"Thread\.Sleep (\d+)", "thread_sleep")
    out["arg_null_throw"] = grab(ct, r"ArgumentNullException` (\d+) 件", "arg_null_throw")

    cm = capture(m.m_comments)
    out["phase_lines"] = grab(cm, r"含む行 (\d+) 件", "phase_lines")
    out["phase_files"] = grab(cm, r"含むファイル (\d+)", "phase_files")
    out["todo"] = grab(cm, r"TODO/FIXME/HACK (\d+)", "todo")

    ifc = capture(m.m_iface)
    out["iface_total"] = grab(ifc, r"全 (\d+) 件", "iface_total")
    out["iface_lonely"] = grab(ifc, r"実装 1 で偽物なし (\d+)", "iface_lonely")

    un = capture(m.m_unref)
    out["unref_plain"] = grab(un, r"class/enum: (\d+) 件", "unref_plain")

    cl = capture(m.m_clone)
    out["clone_struct_pct"] = grab(cl, r"他と同じ形の窓に入る行: \d+ 件（([\d.]+)%）", "clone_struct_pct", float)
    out["_clone_struct_files"] = {p: int(pc) for pc, p in re.findall(r"^\s+\d+/\s*\d+ \(\s*(\d+)%\)\s+(\S+)\s*$", cl, flags=re.M)}
    ce = capture(m.m_cloneexact)
    out["clone_exact_pct"] = grab(ce, r"他と同じ形の窓に入る行: \d+ 件（([\d.]+)%）", "clone_exact_pct", float)

    fl = capture(m.m_fields)
    out["pub_fields_total"] = grab(fl, r"合計 (\d+) 件", "pub_fields_total")
    pf = {p: int(n) for n, p in re.findall(r"^\s+(\d+)\s+(DS4Windows/\S+)\s*$", fl, flags=re.M)}
    out["backingstore_fields"] = pf.get("DS4Windows/Core/Settings/BackingStore.cs", 0)

    nm = capture(m.m_naming)
    out["lower_camel"] = grab(nm, r"小文字始まりのメソッド: (\d+) 件", "lower_camel")
    kinds = {}
    for k in ("System.Timers.Timer", "System.Threading.Timer", "DispatcherTimer", "TimeProvider.CreateTimer"):
        kinds[k] = grab(nm, re.escape(k) + r"[^/]*? (\d+)(?: /|。)", "timer " + k)
    out["_timer_kinds"] = kinds
    out["timer_kind_count"] = sum(1 for v in kinds.values() if v > 0)
    out["timer_alias_lines"] = grab(nm, r"別名 `using NonFormTimer = …` の行 (\d+)", "timer_alias_lines")

    ts = capture(m.m_tests)
    out["test_files"] = grab(ts, r"テストのファイル (\d+)", "test_files")
    out["test_facts"] = grab(ts, r"\[Fact\] (\d+)", "test_facts")
    out["test_theories"] = grab(ts, r"\[Theory\] (\d+)", "test_theories")
    out["test_to_src_pct"] = grab(ts, r"本体 \d+ 行の (\d+)%", "test_to_src_pct")
    out["srctext_tests"] = grab(ts, r"ソースの文字列を読むテスト (\d+) ファイル", "srctext_tests")
    out["guard_named_tests"] = grab(ts, r"を持つ (\d+) ファイル / ArchUnitNET", "guard_named_tests")
    out["arch_tests"] = grab(ts, r"ArchUnitNET を使う (\d+) ファイル", "arch_tests")

    di = capture(m.m_di)
    out["service_locator"] = grab(di, r"Service Locator 呼び出し: (\d+)", "service_locator")
    out["app_lines"] = grab(di, r"App\.xaml\.cs: (\d+) 行", "app_lines")
    out["app_resolve"] = grab(di, r"ResolvePostHostService (\d+) 件", "app_resolve")
    out["static_mutable"] = grab(di, r"static の可変フィールド・自動プロパティ: (\d+) 件", "static_mutable")
    win = grab(di, r"持つファイル[^:]*: (.*)", "win_using", str, default="")
    out["nonui_windows_using"] = sum(int(x) for x in re.findall(r"\w+ (\d+)", win))

    # --- このスクリプト自身が数える値 ---
    out["baseline_layer"] = count_entries(os.path.join(TESTS, "LayerDependencyBaseline.cs"))
    out["baseline_arity"] = count_entries(os.path.join(TESTS, "ConstructorArityBaseline.cs"))
    out["baseline_vm_wpf"] = count_entries(os.path.join(TESTS, "ViewModelWpfReferenceBaseline.cs"))
    out["baseline_view_service"] = count_entries(os.path.join(TESTS, "ViewDirectServiceReferenceBaseline.cs"))
    out["baseline_type_rules"] = out["baseline_vm_wpf"] + out["baseline_view_service"]

    upper = re.compile(r"^using DS4Windows\.(Processing|UI|Output|Composition|Input)(\.\w+)*;", re.M)
    core_upper = 0
    for p in m.src_files():
        if m.layer(p) == "Core" and upper.search(m.read(p)):
            core_upper += 1
    out["core_upper_using"] = core_upper

    docs_root = os.path.join(ROOT, "docs-forDIMG")
    docs_all = docs_active = 0
    for p in m.walk(docs_root, [".md"]):
        docs_all += 1
        if "/Archive/" not in p.replace("\\", "/"):
            docs_active += 1
    out["docs_all"], out["docs_active"] = docs_all, docs_active

    cfg = read_text(os.path.join(ROOT, "Directory.Build.props")) + read_text(os.path.join(SRC, "DS4WinWPF.csproj"))
    editorconfig = read_text(os.path.join(ROOT, ".editorconfig"))
    out["cfg_nullable_enabled"] = bool(re.search(r"<Nullable>\s*enable\s*</Nullable>", cfg, re.I))
    gates = 0
    gates += 1 if re.search(r"<TreatWarningsAsErrors>\s*true\s*</TreatWarningsAsErrors>", cfg, re.I) else 0
    gates += 0 if re.search(r"<EnableNETAnalyzers>\s*false\s*</EnableNETAnalyzers>", cfg, re.I) else 1
    gates += 0 if re.search(r"^\s*dotnet_analyzer_diagnostic\.category-Style\.severity\s*=\s*none", editorconfig, re.M) else 1
    out["cfg_gates_met"] = gates

    standalone = 0
    sdir = os.path.join(ROOT, "StandaloneTests")
    if os.path.isdir(sdir):
        for p in m.walk(sdir, [".cs"]):
            if "[TestMethod" in m.read(p):
                standalone += 1
    out["test_framework_kinds"] = 1 + (1 if standalone else 0)

    cf = out["_clone_struct_files"]
    out["bindingwindow_clone_pct"] = cf.get("DS4Windows/UI/BindingWindow.xaml.cs", 0)
    out["appsettings_clone_pct"] = cf.get("DS4Windows/Core/Settings/AppSettingsService.cs", 0)
    derive(out)
    return out


def derive(out):
    """ほかの値から決まる値。--forecast で元の値を差し替えたあとにも、もう一度呼ぶ。"""
    out["commands_rate"] = out["xaml_command"] / max(1, out["xaml_command"] + out["xaml_click"] + out["xaml_other_events"])
    out["binding_rate"] = out["xaml_binding"] / max(1, out["xaml_binding"] + out["xaml_xname"])
    out["viewmodel_share"] = out["viewmodel_lines"] / max(1, out["viewmodel_lines"] + out["codebehind_lines"])
    out["blocking_calls"] = out["result_wait"] + out["thread_sleep"]
    out["baseline_type_rules"] = out["baseline_vm_wpf"] + out["baseline_view_service"]


# ---------------------------------------------------------------------------
# 2. 採点基準（目標値＝満点、下限値＝0 点。間は直線）
# ---------------------------------------------------------------------------
def ind(id, label, key, goal, floor, weight, unit=""):
    """数値の指標。goal が floor より小さければ「小さいほど良い」、大きければ「大きいほど良い」。"""
    return {"id": id, "label": label, "kind": "metric", "key": key, "goal": goal, "floor": floor, "weight": weight, "unit": unit}


def chk(id, label, key, weight):
    """はい／いいえの確認項目（Rubric-Inputs.json の checklist[key].answer。はい＝満点、いいえ＝0）。"""
    return {"id": id, "label": label, "kind": "check", "key": key, "weight": weight}


def man(id, label, key, goal, floor, weight, unit=""):
    """手で記録する数値（Rubric-Inputs.json の manual[key].value）。"""
    return {"id": id, "label": label, "kind": "manual", "key": key, "goal": goal, "floor": floor, "weight": weight, "unit": unit}


VIEWPOINTS = [
    ("Pure DI 化", [
        ind("di_sl", "Composition・App 以外の Service Locator 呼び出し", "service_locator", 0, 10, 4, "件"),
        ind("di_static", "static の可変フィールド・自動プロパティ", "static_mutable", 50, 200, 1, "件"),
        ind("di_app_resolve", "App.xaml.cs の ResolvePostHostService", "app_resolve", 10, 60, 1, "件"),
        ind("di_iface_lonely", "実装 1・偽物なしのインターフェース", "iface_lonely", 40, 120, 1, "件"),
        ind("di_arity", "コンストラクターの引数が 12 を超える型", "baseline_arity", 0, 10, 1, "件"),
    ]),
    ("各層の役割分担", [
        ind("ly_arch", "層の依存の違反（LayerDependencyBaseline）", "baseline_layer", 0, 20, 3, "件"),
        ind("ly_type_rules", "V1・V2 の違反（2 つの基準ファイルの合計）", "baseline_type_rules", 0, 20, 2, "件"),
        ind("ly_core_upper_using", "Core にある上位層の using のファイル", "core_upper_using", 0, 40, 1, "ファイル"),
        ind("ly_nonui_windows_using", "UI 以外の System.Windows* の using のファイル", "nonui_windows_using", 0, 80, 1, "ファイル"),
    ]),
    ("MVVM 原則遵守の割合", [
        ind("mv_type_rules", "型の規則 V1・V2 の違反", "baseline_type_rules", 0, 20, 4, "件"),
        ind("mv_command_rate", "コマンド率（Command ÷ Command＋Click＋その他のイベント）", "commands_rate", 0.70, 0.0, 2),
        ind("mv_binding_rate", "結びつけ率（{Binding} ÷ {Binding}＋x:Name）", "binding_rate", 0.80, 0.30, 1),
        ind("mv_viewmodel_share", "ViewModel の行の割合", "viewmodel_share", 0.75, 0.40, 1),
        ind("mv_handwire", "イベントの手結び（+= メソッド名;）", "handwire", 50, 300, 1, "件"),
        ind("mv_direct_assign", "code-behind の部品への直接代入", "direct_assign", 100, 400, 1, "件"),
    ]),
    ("オブジェクト指向", [
        chk("oo_skeleton", "入力機種が共通の骨組み（InputLoopDevice）の差し込み口だけで書かれている", "oo_skeleton", 3),
        ind("oo_backingstore", "BackingStore の公開フィールド", "backingstore_fields", 20, 150, 2, "件"),
        ind("oo_pub_fields", "公開の可変フィールドの合計", "pub_fields_total", 300, 1000, 1, "件"),
        ind("oo_big_files", "500 行以上のファイル", "big_files", 10, 80, 1, "件"),
        ind("oo_lower_camel", "小文字始まりの公開メソッド", "lower_camel", 0, 200, 1, "件"),
    ]),
    ("共有部品化", [
        chk("sh_input_path", "入力機種を 1 行（AddInputDevice）で足せる", "input_device_path", 2),
        chk("sh_output_path", "出力の種類をカートリッジで足せる", "output_cartridge_path", 2),
        chk("sh_settings_ui", "機種ごとの設定画面が ControllerSetting の定義から作られる", "settings_ui_from_definition", 1),
        chk("sh_install_flow", "外部コンポーネントの取得・導入の流れが 1 つの部品に集約されている", "install_flow_unified", 1),
        ind("sh_clone_struct", "構造の重複（正規化した 14 行の窓）", "clone_struct_pct", 5, 30, 1, "%"),
        ind("sh_clone_exact", "同一の重複（10 行の窓）", "clone_exact_pct", 1, 5, 1, "%"),
        ind("sh_bindingwindow_clone", "BindingWindow.xaml.cs の構造の重複", "bindingwindow_clone_pct", 20, 80, 1, "%"),
    ]),
    ("冗長処理", [
        ind("rd_bare_catch", "理由のコメントがない空の catch", "catch_bare", 0, 500, 2, "件"),
        ind("rd_log_wrap", "ログ呼び出しを包む try/catch", "log_wrap", 0, 60, 1, "件"),
        ind("rd_arg_null", "?? throw new ArgumentNullException", "arg_null_throw", 0, 1000, 1, "件"),
        ind("rd_phase_lines", "Phase<数字> を含む行（経緯のコメント）", "phase_lines", 100, 3000, 1, "行"),
        ind("rd_srctext_tests", "ソースの文字列を読むテスト", "srctext_tests", 30, 200, 1, "ファイル"),
        ind("rd_clone_exact", "同一の重複（10 行の窓）", "clone_exact_pct", 1, 5, 3, "%"),
        ind("rd_clone_struct", "構造の重複（正規化した 14 行の窓）", "clone_struct_pct", 5, 30, 1, "%"),
    ]),
    ("採用手段の統一", [
        ind("un_inpc", "INotifyPropertyChanged を直接実装する型", "inpc_direct", 0, 25, 1, "件"),
        ind("un_timer_kinds", "タイマーの生成の種類", "timer_kind_count", 1, 6, 1, "種類"),
        ind("un_timer_alias", "別名 using NonFormTimer の行", "timer_alias_lines", 0, 20, 1, "行"),
        ind("un_lower_camel", "小文字始まりの公開メソッド", "lower_camel", 0, 200, 1, "件"),
        ind("un_async_void", "async void（イベントハンドラーの形以外）", "async_void_nonhandler", 0, 20, 1, "件"),
        ind("un_blocking", ".Result・.Wait()・Thread.Sleep", "blocking_calls", 0, 80, 1, "件"),
        ind("un_test_frameworks", "テストの枠の数", "test_framework_kinds", 1, 3, 1, "種類"),
        chk("un_policy", "採用手段の方針が文書にある（規約・Log-Policy）", "policy_documented", 1),
    ]),
    ("メンテナンス性", [
        ind("mt_test_ratio", "テストの行数 ÷ 本体の行数", "test_to_src_pct", 50, 10, 3, "%"),
        chk("mt_safety_net", "ArchUnitNET・黄金テスト・ラチェットが Debug ALL で通る", "safety_net_green", 2),
        ind("mt_big_files", "500 行以上のファイル", "big_files", 10, 80, 1, "件"),
        ind("mt_docs", "Archive の外の文書（docs-forDIMG の Markdown）", "docs_active", 100, 600, 1, "ファイル"),
        ind("mt_phase_lines", "Phase<数字> を含む行（経緯のコメント）", "phase_lines", 100, 3000, 1, "行"),
        man("mt_nullable", "Nullable を有効にした層の数（全 6 層。プロジェクト全体で有効なら 6）", "nullable_layers_enabled", 6, 0, 1, "層"),
        ind("mt_gates", "品質の門（警告をエラー化・アナライザー有効・スタイル診断を無効化していない）の達成数", "cfg_gates_met", 3, 0, 1, "/3"),
    ]),
    ("機能追加の容易化", [
        chk("ad_input_path", "入力機種を 1 行（AddInputDevice）で足せる", "input_device_path", 2),
        chk("ad_output_path", "出力の種類をカートリッジで足せる", "output_cartridge_path", 2),
        chk("ad_settings_ui", "機種ごとの設定画面が ControllerSetting の定義から作られる", "settings_ui_from_definition", 1),
        man("ad_profile_files", "プロファイルの設定の項目を 1 つ足すときに触るファイル数", "profile_setting_files", 2, 8, 3, "ファイル"),
        ind("ad_appsettings_dup", "AppSettingsService.cs の構造の重複", "appsettings_clone_pct", 20, 90, 1, "%"),
        ind("ad_backingstore", "BackingStore の公開フィールド", "backingstore_fields", 20, 150, 1, "件"),
    ]),
]


# ---------------------------------------------------------------------------
# 机上の予想（--forecast）。各 PhaseRV の計画書の完了の定義どおりに進んだとして、元の値を差し替えて採点する。
# 値は計画書の目標値、目標がないものは下の「仮定」。実際の完了時は、仮定ではなく実測で採点し直す（Phase の最終 Step）。
# キーは Measure の値の名前。"manual." で始まるものは Rubric-Inputs.json の manual、"check." で始まるものは checklist の答え。
# 値は「その Phase の完了時にそうなる」値で、前の Phase の差し替えの上に重ねる（累積）。
# ---------------------------------------------------------------------------
FORECAST = [
    ("RV1", {  # 未使用の using の整理、別名の削除、アナライザー。Nullable は試験のみ
        "core_upper_using": 0, "nonui_windows_using": 8, "timer_alias_lines": 0,
        "cfg_gates_met": 2,  # 仮定: アナライザー有効＋スタイル診断の無効化を外す（警告のエラー化は別）
    }),
    ("RV2", {  # 空の catch、ログを包む try、async void
        "catch_bare": 0, "log_wrap": 0, "async_void_nonhandler": 0,
    }),
    ("RV3", {  # MainWindow と 3 画面。仮定: RV3＋RV4 の最終の値（下）へ 35% 近づく（Click の 193 のうち約 65 が対象）
        "xaml_command": 60, "xaml_click": 141, "xaml_other_events": 40, "handwire": 183, "direct_assign": 241,
        "xaml_binding": 850, "xaml_xname": 650, "viewmodel_lines": 15900, "codebehind_lines": 9400,
    }),
    ("RV4", {  # 全画面。コマンド率 70% 以上、手結び 100 以下、直接代入 150 以下（報告書 §5 の目標）。仮定: 結びつけ率・ViewModel の割合
        "xaml_command": 170, "xaml_click": 45, "xaml_other_events": 28, "handwire": 100, "direct_assign": 150,
        "xaml_binding": 1000, "xaml_xname": 600, "viewmodel_lines": 17500, "codebehind_lines": 7500,
        "bindingwindow_clone_pct": 25, "check.install_flow_unified": True,
    }),
    ("RV5", {  # 設定の宣言化。AppSettingsService の重複 20% 以下。BackingStore の公開フィールドは調査のみ（D3＝A）で変わらない
        "appsettings_clone_pct": 20, "manual.profile_setting_files": 2,
        "clone_struct_pct": 11.0,  # 仮定: AppSettingsService・DTO・節の重複が減る分
    }),
    ("RV6", {  # 重複の整理（同一・構造）。未参照の型の削除は指標に入れていない
        "clone_exact_pct": 1.4, "clone_struct_pct": 10.4,  # 仮定
    }),
    ("RV7", {  # 変更通知・命名・タイマー・null 検査・テストの枠
        "inpc_direct": 1, "lower_camel": 0, "timer_kind_count": 1, "timer_alias_lines": 0, "arg_null_throw": 0,
        "test_framework_kinds": 1,
    }),
    ("RV8", {  # 経緯のコメント・文書・ガードテスト。仮定: 値は計画書に目標がなく、分類のあとに決まる
        "phase_lines": 200, "srctext_tests": 60, "docs_active": 100,
    }),
    ("RV9（予定）", {  # Nullable の全体有効化。計画書はまだない
        "manual.nullable_layers_enabled": 6, "cfg_gates_met": 3,
    }),
]


def apply_overrides(metrics, inputs, ov):
    import copy
    m, i = dict(metrics), copy.deepcopy(inputs)
    for k, v in ov.items():
        if k.startswith("manual."):
            i["manual"][k[7:]]["value"] = v
        elif k.startswith("check."):
            i["checklist"][k[6:]]["answer"] = v
        else:
            m[k] = v
    derive(m)
    return m, i


def print_forecast(metrics, inputs):
    names = [n for n, _ in FORECAST]
    cur_m, cur_i = metrics, inputs
    table = [("現在", score_all(cur_m, cur_i))]
    for n, ov in FORECAST:
        cur_m, cur_i = apply_overrides(cur_m, cur_i, ov)
        table.append((n, score_all(cur_m, cur_i)))
    print("# 机上の予想（各 Phase が計画書の完了の定義どおりに終わった場合。累積）\n")
    print("| 観点 | " + " | ".join(n for n, _ in table) + " |")
    print("|---|" + "---|" * len(table))
    for idx, (vp, _) in enumerate(VIEWPOINTS):
        cells = []
        prev = None
        for n, rows in table:
            g = rows[idx]["grade"]
            cells.append(f"**{g}**" if prev is not None and g != prev else g)
            prev = g
        print(f"| {vp} | " + " | ".join(cells) + " |")
    print("\n点数（%）:\n")
    print("| 観点 | " + " | ".join(n for n, _ in table) + " |")
    print("|---|" + "---|" * len(table))
    for idx, (vp, _) in enumerate(VIEWPOINTS):
        print(f"| {vp} | " + " | ".join(f"{rows[idx]['score'] * 100:.0f}" for _, rows in table) + " |")


def score_indicator(spec, metrics, inputs):
    """(value, score0to1)。値が取れないとき value は None、score は 0。"""
    k = spec["key"]
    if spec["kind"] == "check":
        item = inputs.get("checklist", {}).get(k)
        if item is None:
            raise SystemExit(f"[Score] Rubric-Inputs.json に checklist.{k} がない")
        return bool(item["answer"]), 1.0 if item["answer"] else 0.0
    if spec["kind"] == "manual":
        item = inputs.get("manual", {}).get(k)
        if item is None:
            raise SystemExit(f"[Score] Rubric-Inputs.json に manual.{k} がない")
        v = item["value"]
        if k == "nullable_layers_enabled" and metrics.get("cfg_nullable_enabled"):
            v = max(v, 6)  # プロジェクト全体で有効なら全層
    else:
        v = metrics[k]
    goal, floor = spec["goal"], spec["floor"]
    s = (v - floor) / (goal - floor)
    return v, max(0.0, min(1.0, s))


def score_all(metrics, inputs):
    rows = []
    for name, specs in VIEWPOINTS:
        tw = sum(s["weight"] for s in specs)
        items, acc = [], 0.0
        for s in specs:
            v, sc = score_indicator(s, metrics, inputs)
            acc += s["weight"] * sc
            items.append({"id": s["id"], "label": s["label"], "value": v, "score": round(sc, 4), "weight": s["weight"],
                          "unit": s.get("unit", ""), "kind": s["kind"]})
        total = acc / tw
        rows.append({"name": name, "score": round(total, 4), "grade": band(total), "indicators": items})
    return rows


# ---------------------------------------------------------------------------
# 3. 出力
# ---------------------------------------------------------------------------
def fmt_value(it):
    v = it["value"]
    if isinstance(v, bool):
        return "はい" if v else "いいえ"
    suffix = it["unit"] if it["unit"] in ("%", "/3") else ""
    if isinstance(v, float):
        return (f"{v:.3f}" if abs(v) <= 1.0 else f"{v:.1f}") + suffix
    return f"{v}{suffix}"


def print_scores(rows, label):
    print(f"# 採点結果（採点基準 {RUBRIC_VERSION}。{label}）\n")
    print("| 観点 | 点数 | 等級 |")
    print("|---|---|---|")
    for r in rows:
        print(f"| {r['name']} | {r['score'] * 100:.1f}% | **{r['grade']}** |")
    for r in rows:
        print(f"\n## {r['name']}（{r['score'] * 100:.1f}% → {r['grade']}）\n")
        print("| 指標 | 値 | 点（0〜1） | 重み |")
        print("|---|---|---|---|")
        for it in r["indicators"]:
            print(f"| {it['label']} | {fmt_value(it)} | {it['score']:.2f} | {it['weight']} |")


def print_rubric():
    print(f"## 採点基準 {RUBRIC_VERSION}\n")
    print("等級表（点数 → 等級）: " + " / ".join(f"{g} ≥ {lo * 100:.0f}%" for lo, g in BANDS) + " / それ未満は D\n")
    for name, specs in VIEWPOINTS:
        tw = sum(s["weight"] for s in specs)
        print(f"### {name}（重みの合計 {tw}）\n")
        print("| ID | 指標 | 種類 | 目標値（満点） | 下限値（0 点） | 重み |")
        print("|---|---|---|---|---|---|")
        for s in specs:
            if s["kind"] == "check":
                print(f"| `{s['id']}` | {s['label']} | はい／いいえ | はい | いいえ | {s['weight']} |")
            else:
                u = s.get("unit", "")
                kind = "手で記録" if s["kind"] == "manual" else "計測"
                print(f"| `{s['id']}` | {s['label']} | {kind} | {s['goal']}{u} | {s['floor']}{u} | {s['weight']} |")
        print()


def print_diff(prev, rows):
    print(f"\n## 前回との差（前回: {prev.get('label', '?')} / {prev.get('date', '?')}、採点基準 {prev.get('rubric', '?')}）\n")
    if prev.get("rubric") != RUBRIC_VERSION:
        print("**注意**: 採点基準の版が違う。比べる前に、前回の保存を同じ版で採点し直すこと（03-Rubric.md §6）。\n")
    pv = {v["name"]: v for v in prev["viewpoints"]}
    print("| 観点 | 前回 | 今回 | 差 |")
    print("|---|---|---|---|")
    for r in rows:
        p = pv.get(r["name"])
        if not p:
            continue
        d = (r["score"] - p["score"]) * 100
        print(f"| {r['name']} | {p['grade']}（{p['score'] * 100:.1f}%） | {r['grade']}（{r['score'] * 100:.1f}%） | {d:+.1f} 点 |")
    print("\n指標の変化（点が動いたもの。影響は 重み × 点の差 ÷ 観点の重みの合計）:\n")
    lines = []
    for r in rows:
        p = pv.get(r["name"])
        if not p:
            continue
        pi = {i["id"]: i for i in p["indicators"]}
        tw = sum(i["weight"] for i in r["indicators"])
        for it in r["indicators"]:
            q = pi.get(it["id"])
            if q is None or abs(it["score"] - q["score"]) < 1e-9:
                continue
            eff = it["weight"] * (it["score"] - q["score"]) / tw * 100
            lines.append(f"| {r['name']} | {it['label']} | {fmt_value(q)} | {fmt_value(it)} | {eff:+.1f} 点 |")
    if not lines:
        print("（点が動いた指標はない）")
        return
    print("| 観点 | 指標 | 前回の値 | 今回の値 | 影響（観点の点数への寄与） |")
    print("|---|---|---|---|---|")
    print("\n".join(lines))


def main():
    ap = argparse.ArgumentParser(description="コードレビューの 9 観点を固定の採点基準で算定する")
    ap.add_argument("--inputs", default=os.path.join(HERE, "Rubric-Inputs.json"), help="手で記録する値と確認項目（JSON）")
    ap.add_argument("--json", default="", help="結果の保存先")
    ap.add_argument("--label", default="", help="保存に付ける名前（例: RV1 完了）")
    ap.add_argument("--diff", default="", help="前回の保存（--json の出力）との差を表示する")
    ap.add_argument("--show-rubric", action="store_true", help="採点基準の表を出して終わる")
    ap.add_argument("--calibration", action="store_true", help="2026-11-19 の評価（報告書 §1.2）を再現できるかを確かめる")
    ap.add_argument("--forecast", action="store_true", help="各 Phase が完了の定義どおりに終わった場合の机上の予想を出して終わる")
    a = ap.parse_args()

    if a.show_rubric:
        print_rubric()
        return 0

    inputs = json.load(open(a.inputs, encoding="utf-8"))
    m = load_measure()
    metrics = collect_metrics(m)
    if a.forecast:
        print_forecast(metrics, inputs)
        return 0
    rows = score_all(metrics, inputs)
    label = a.label or inputs.get("snapshot_label", "")
    print_scores(rows, label or "現在")

    if a.diff:
        print_diff(json.load(open(a.diff, encoding="utf-8")), rows)

    if a.json:
        snap = {"rubric": RUBRIC_VERSION, "label": label, "date": datetime.date.today().isoformat(),
                "metrics": {k: v for k, v in metrics.items() if not k.startswith("_")},
                "viewpoints": rows}
        os.makedirs(os.path.dirname(os.path.abspath(a.json)), exist_ok=True)
        with open(a.json, "w", encoding="utf-8", newline="\n") as f:
            json.dump(snap, f, ensure_ascii=False, indent=1)
            f.write("\n")
        print(f"\n保存: {a.json}")

    if a.calibration:
        print("\n## 基準の確認（報告書 §1.2 の評価との照合）\n")
        print("| 観点 | 報告書 | 採点 | 一致 |")
        print("|---|---|---|---|")
        bad = 0
        for r in rows:
            exp = REPORT_GRADES.get(r["name"])
            ok = exp == r["grade"]
            bad += 0 if ok else 1
            print(f"| {r['name']} | {exp} | {r['grade']}（{r['score'] * 100:.1f}%） | {'○' if ok else '×'} |")
        return 1 if bad else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
