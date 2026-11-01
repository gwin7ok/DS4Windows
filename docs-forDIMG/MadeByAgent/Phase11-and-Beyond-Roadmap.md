# Phase11 以降のロードマップ（FurtherImprovements 01〜06 と GitHub Issue の統合）

作成日: 2026-11-02（同日、Issue #2〜#4 を取り込んで Phase を組み直し）
状態: **案（ユーザーの承認待ち）。実装は、各 Phase／Step の個別計画書を作り、承認を得てから行う（copilot-instructions §4）**
ブランチ: `ArrangementRoadmap`
関連: `Phase9-Completion-Report.md` §7、`Phase9-Deferred-Ledger.md`、`../FurtherImprovements/01〜06`、`Release-Backlog.md`

---

## 1. 目的と、従来の Phase11 からの変更点

- 従来の「Phase11」は、名前空間・フォルダの再編の 1 件だけだった（定義は `Phase9-Responsibility-Split-Plan.md` Q4・Q10、`Phase9-Completion-Report.md` §7、規約 §3.3 原則 3・§3.4 に散在）。
- 次の 3 つの入力を統合し、Phase の番号と中身を組み直した。**名前空間・フォルダの再編は Phase14 になった。**
  1. `FurtherImprovements/01〜06`（外部 AI が Phase10 到達時点で作成。2026-10-24 追加）
  2. GitHub Issue（`gwin7ok/DS4Windows`）の未完了 3 件: #2（マクロにマウスポインタ移動。台帳の R-3）、#3（割り当てにプログラムのフォーカス。R-4）、#4（設定の保存・反映の抜け漏れ。新設の R-7）。#1（R-2）は 4.2.0 で完了済み
  3. 既存の先送り項目（`Phase9-Deferred-Ledger.md`）と K7b-1

## 2. 新しい Phase 構成

| Phase | 名称 | 内容 | 主な入力 | 作業量 |
|---|---|---|---|---|
| **11** | 安定化と不具合修正 | 先送りの実機確認の消化、K7b-1 の締め、**Issue #4（R-7）**、テスト整備（XAML のセマンティック検証）、数値の見切れの修正 | #4、FI の Phase A、台帳 §27〜§30 | S〜M |
| **12** | 機能追加 | **R-4（#3）プログラムのフォーカス** → **R-3（#2）マクロのマウスポインタ移動** | #3、#2 | M＋L |
| **13** | `ControlService` の整理 | コンストラクタ引数の集約、`Func<T>` の削減、`ServiceProviderHolder` の撤廃。F-5c・F-1c・F-2c を含む | FI の Phase C | L |
| **14** | 名前空間・フォルダの再編（旧 Phase11） | 名前空間をフォルダと一致、`DI/` を Composition Root 専用に | 規約 §3.3 原則 3・§3.4 | L（機械的） |
| **15** | UI 共通化 | コントローラー描画の共通化、デザインシステム、F-4（`ProfileEditor.xaml.cs` の分割）、G-1（`ProfileSettingsViewModel` の分割） | FI の Phase B・06 | L |
| **16** | Layer 1 のカートリッジ化 | 第 1 層のデバイスクラスの分離（9-H）、プラグイン化。05 の整備 | FI の Phase D、9-H | L |
| 任意 | 性能 | 入力パイプラインの高速化（04） | 実測で問題が出たとき | — |

各 Phase の区切りで、リリース（版数の更新、CHANGELOG）を検討できる。

## 3. 各 Phase の内容

### Phase11: 安定化と不具合修正

1. **先送りの実機確認の消化**: 台帳 §27〜§30、P9M3-3、P9F1-4、P9M2-2〜4。
2. **K7b-1 の締め**: 新しいダンプの有無をユーザーが確認 → 「解消」として閉じる（`Release-Backlog.md` §5）。
3. **Issue #4（R-7）**: 設定の「画面で変えても保存されない／反映が漏れる」4 件。着手前に **現状を再照合**する（下表）。
4. **テスト整備（FI Phase A）**: XAML のセマンティック検証ヘルパー（`XamlTestHelper`）の導入と、脆い文字列照合テストの置換（一部は `3b7ca385` で済み）。`ControllerReadingsControl` の数値の見切れ（`255` → `25`）の修正。

Issue #4 の 4 件と、2026-11-02 時点のコードとの照合:

| # | 項目 | Issue の主張 | 照合の結果 |
|---|---|---|---|
| 1 | `UseDs3PitchRollSim` | 「適用」で DS4 への補正が漏れる。直し先は `SetLateProperties`（`ProfileEditor.xaml.cs`） | **場所が古い**。`SetLateProperties` は現存せず、補正の処理は別の場所へ移っている。症状を実機で再現してから、直し先を決める |
| 2 | `Keepsize` | XAML は `Mode=TwoWay`、C# は get-only | **事実**（`ProfileEditor.xaml.cs:215`、`ProfileEditor.xaml:125`）。`set` を足すか、`Mode=TwoWay` を外す |
| 3 | `CustomSteamFolder` | 存在しないパスを入れると警告なしで破棄される | 未照合。仕様（警告を出す）の決定が要る |
| 4 | `CheckEvery` | 「更新確認」が OFF のとき、数値を変えても保存されない | 未照合。名前は現在 `CheckEveryValue`／`CheckEveryUnit`。仕様の決定が要る |

- 2 は挙動を変えない整理。1・3・4 は挙動が変わるため、規約 §2.2（No Feature Drop）に照らし、仕様をユーザーが決めてから実装する。

### Phase12: 機能追加

- **順序は R-4 → R-3**（`Release-Backlog.md` §3 の案を踏襲）。
- **R-4（#3）**: 新しい `ActionTypeId`、編集画面（`LaunchProgram` に近い）、保存、実行。前面化（`SetForegroundWindow`）には Windows の制限があり、実機での確認が多い。Phase10 で割り当て編集画面が統合されたため、種別の追加は 1 か所で済む。
- **R-3（#2）**: マクロの値の体系の拡張（座標を持つ値は、現行の整数 1 個に収まらない）、記録画面（`RecordBox`）、保存形式。着手前に **仕様の決定**が要る（絶対／相対、複数モニター、DPI）。保存形式を変えるため、古い版との互換を検討する（R-2 の `MacroExecuteMode` が前例）。
- 置く位置の理由: 新機能は利用者に見える価値があり、Phase13（内部構造の整理）より前に届けられる。新しいファイルは Phase14 の一括移動に含めればよい。

### Phase13: `ControlService` の整理

- 範囲: 引数の集約、`Func<T>` 14 件の削減、`ServiceProviderHolder`（6 ファイル・13 件）の撤廃、F-5c（`running`／`suspending`）、F-1c、F-2c。
- 実測: 文書の「34 引数」は、手元の数え方で 30 台前半。**着手前に現状と再照合**する（§5）。
- Phase14 の前に行う理由: ここで新設・移動する型の名前空間を、2 回直さないため。

### Phase14: 名前空間・フォルダの再編（旧 Phase11）

- 名前空間をフォルダパスと一致させる。`DI/` を Composition Root 専用にする（インターフェースは層別のフォルダへ）。
- 移動は `git mv` のみで、同じコミットで内容を変えない（R7）。名前空間の書き換えは、別のコミットにする。
- 現状（実測）: `DS4Windows` 195 ファイル、`DS4Windows.Actions` 108、`DS4Windows.Services` 103、`DS4Windows.DI` 72、`DS4WinWPF.*` 約 200。`DI/` に 81 ファイル。
- 構造変更（Phase12・13）の後に、機械的な一括変更を 1 回で行う。

### Phase15: UI 共通化

- コントローラー描画の共通化（FI の Phase B）、デザインシステム（06）、F-4・G-1 の判断。
- Phase14 の後に行う理由: 新しい部品と XAML を、最終の名前空間で最初から作れる。XAML の名前空間（`xmlns:local`）の一括変更は、UI を触り終えてからのほうが安全。
- Phase11 の Issue #4 は `ProfileEditor` を触るため、F-4（分割）の前に済ませる。

### Phase16: Layer 1 のカートリッジ化

- 9-H（第 1 層のデバイスクラスの分離）と、FI の Phase D（`IHidTransport` と `IInputReportParser` の分離）を統合する。K7b-1 の締め（Phase11）が前提。
- 05（プラグイン開発ガイド）は、この Phase の完了後に実装に合わせて整備する。

## 4. FurtherImprovements 01〜06 の扱い

| 文書 | 扱い |
|---|---|
| 01 | 診断。Phase13・15・16 の入力。数値は採用前に再照合（§5） |
| 02・03 | 設計と手順。A→Phase11、B→Phase15、C→Phase13、D→Phase16 に再配置 |
| 04 | 性能。実測で問題が出たときだけ |
| 05 | Phase16 の後 |
| 06 | Phase15 と一体 |

## 5. 採用前の再照合と条件

2026-11-02 の実測（FI 文書との差）:
- `ControlService` のコンストラクタ引数: 文書は 34 個。実測は 30 台前半（数え方の確認が要る）。
- `Func<T>` の登録: `ServiceRegistration.cs` に 14 件。
- `ServiceProviderHolder` の使用: 6 ファイル・13 件（台帳 §17.3 の 17 件から減少）。
- FI 文書の対象ブランチは `DarkThemeArrange`、テスト 2,728 件など、作成時点の値。

採用の条件:
- 提案が `copilot-instructions.md` の規約（§2 品質・安全基準、§3.3 1 ファイル 1 型、§3.4 配置ルール R1〜R7）に反しないこと。
- 機能の削減をしないこと（§2.2）。ログの文言・レベルを維持すること（§2.3）。
- 個別計画書を作り、承認を得てから実装すること（§4）。

## 6. 順序についての判断（代替案）

- 推奨は Phase11 → 12 → 13 → 14 → 15 → 16。リリースの価値（不具合修正、新機能）を先に届け、内部構造の変更は後ろにまとめる。
- 代替案 A: 機能追加（Phase12）を Phase14 の後ろへ回す。新しいファイルを最終の名前空間で作れるが、利用者への機能の提供が遅れる。
- 代替案 B: Phase13 と Phase14 を入れ替える。名前空間の再編が先になるが、Phase13 で型を新設・移動するたびに名前空間を揃える手間が増える。

## 7. 次の作業

1. 本書の承認（Phase の構成と順序、Issue #4 の各項目の仕様）。
2. Phase11 の個別計画書の作成（実機確認の消化の計画、Issue #4 の再照合、テスト整備）。
