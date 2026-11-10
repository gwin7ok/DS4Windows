# Phase11 以降のロードマップ（FurtherImprovements 01〜06 と GitHub Issue の統合）

作成日: 2026-11-02（同日、Issue #2〜#4 を取り込んで Phase を組み直し）。2026-11-03: ログ整理（L1＝Phase12 の最初、L2＝Phase13b）とトリガー系 4 クラスの引数整理（Phase13）を追加
状態: **Phase の構成と順序は承認済みで運用中（Phase11・12 は完了。2026-11-04 時点）。§8 の追加案は 2026-11-04 に承認済み（8.1〜8.3 すべて採用）。実装は、各 Phase／Step の個別計画書を作り、承認を得てから行う（copilot-instructions §4）**
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
| **12**（完了 2026-11-03） | 機能追加 | **L1（ログの方針の策定）** → **R-4（#3）プログラムのフォーカス** → **R-3（#2）マクロのマウスポインタ移動** | #3、#2、Phase11 W6 の所見 | S＋M＋L |
| **13** | `ControlService` の整理 | コンストラクタ引数の集約、`Func<T>` の削減、`ServiceProviderHolder` の撤廃。F-5c・F-1c・F-2c を含む。**トリガー系 4 クラスの未使用コンストラクター引数の整理**を Step に含む | FI の Phase C、Phase11 の Debouncing 削除 | L |
| **13b** | ログの一括整理（L2） | 方針（Phase12 の L1）に沿って、ログのレベルの振り分けと文言を統一。ログを検査するテスト・黄金ファイルの更新 | Phase12 の L1 | L（実測 666 か所・101 ファイル。当初の見積りは約 624 か所・96 ファイル） |
| **14** | 名前空間・フォルダの再編（旧 Phase11） | 名前空間をフォルダと一致、`DI/` を Composition Root 専用に | 規約 §3.3 原則 3・§3.4 | L（機械的） |
| **14b**（2026-11-05 追加・完了） | 依存の向きの是正 | リポジトリ全体レビュー（`Repository-Review-2026-11-05.md`）の R-01〜R-04 ほか。層の依存のアーキテクチャテスト（許可リストのラチェット）、共有型の `Core/` への移動、UI のファクトリーの契約の移動。Phase15 の前に名前空間を確定させる | レビュー R-01〜R-04、`Phase14b-Plan.md` | M |
| **14c**（2026-11-05 追加・完了） | 共有の値の型の確定と、UI に触れない静的な状態の撤廃 | 値の型 8 つを `Core/Model` へ、`SyntheticDispatcher`・ライトバーの静的な配列・`ActionRegistry` の整理、`ServiceRegistration` の分割、L2・L7 の件数のラチェット、運用ルール（R-15・R-16）。Phase15 の前に名前空間を確定させる | `Phase14c-Plan.md`、レビュー R-05・R-06・R-13・R-15〜R-18 | M |
| **14d**（2026-11-05 追加・完了） | テストの整理 | 不要なテストの削除・統合（ソースの文字列を読むガード 171 ファイル・1,061 件の見直し。層の依存の検査などで置き換えられるものを畳む）、遅いテスト（実時間で待つ 177 件・約 28 秒）の待ち時間の短縮、テストの命名（R-16）、必要なら並列実行。Phase15 で UI のファイルを動かす前に、パスを文字列で持つ検査を減らす | 2026-11-05 の実測（合計 58.7 秒のうち 0.3 秒以上の 53 件で 44 秒）、レビュー R-16 | M |
| **15-0**（2026-11-05 追加・2026-11-08 完了） | Phase14 系の小さな持ち越しの片付け | 利用者向けのログ文言のリソース化（Phase13b-2）、Phase 名のテストの改名（R-16）、テスト固有の読み取りの補助の整理、テストの共有の状態の整理と並列実行の再計測（KI-4） | `Phase15-0-Plan.md` | M |
| **15** | UI 共通化 | コントローラー描画の共通化、デザインシステム（数値入力の共通部品、テーマの対称化、Color／Brush の命名規則。§8.2）、F-4（`ProfileEditor.xaml.cs` の分割）、G-1（`ProfileSettingsViewModel` の分割） | FI の Phase B・06 | L |
| **16** | Layer 1 のカートリッジ化 | 第 1 層のデバイスクラスの分離（9-H）、仮想パッド出力の ViGEm 分離（§8.1）、プラグイン化。05 の整備 | FI の Phase D、9-H | L |
| 任意 | 性能 | 入力パイプラインの高速化（04） | 実測で問題が出たとき | — |

各 Phase の区切りで、リリース（版数の更新、CHANGELOG）を検討できる。

## 3. 各 Phase の内容

### Phase11: 安定化と不具合修正

個別計画書: `Phase11-Plan.md`（承認済み、2026-11-02）。 **Phase11 は完了（2026-11-03。4.2.4。`Phase11-Completion-Report.md`）。K7b-1 は解消として閉じた。**

1. **先送りの実機確認の消化**: 台帳 §27〜§30、P9M3-3、P9F1-4、P9M2-2〜4。
2. **K7b-1 の締め**: 新しいダンプの有無をユーザーが確認 → 「解消」として閉じる（`Release-Backlog.md` §5）。
3. **Issue #4（R-7）**: 設定の「画面で変えても保存されない／反映が漏れる」4 件。着手前に **現状を再照合**する（下表）。
4. **テスト整備（FI Phase A）**: XAML のセマンティック検証ヘルパー（`XamlTestHelper`）の導入と、脆い文字列照合テストの置換（一部は `3b7ca385` で済み）。`ControllerReadingsControl` の数値の見切れ（`255` → `25`）の修正。

Issue #4 の 4 件と、2026-11-02 時点のコードとの照合:

| # | 項目 | Issue の主張 | 照合の結果 |
|---|---|---|---|
| 1 | `UseDs3PitchRollSim` | 「適用」で DS4 への補正が漏れる。直し先は `SetLateProperties` | **主張の前提が古く、実態はより重い**。`SetLateProperties` は存在せず、DS3 の擬似を ON にしたときの DS4 への補正は、保存でも適用でも行われない（2026-09-14 の保存処理の統合で消えた回帰）。詳細は `Phase11-Plan.md` §1.1 |
| 2 | `Keepsize` | XAML は `Mode=TwoWay`、C# は get-only | **事実**（`ProfileEditor.xaml.cs:215`、`ProfileEditor.xaml:125`）。`set` を足すか、`Mode=TwoWay` を外す |
| 3 | `CustomSteamFolder` | 存在しないパスを入れると警告なしで破棄される | 未照合。仕様（警告を出す）の決定が要る |
| 4 | `CheckEvery` | 「更新確認」が OFF のとき、数値を変えても保存されない | setter はそのとおりだが、OFF のときは数値入力自体が無効（`IsEnabled`）で、**仕様として閉じる見込み**（`Phase11-Plan.md` §1.1） |

- 2 は挙動を変えない整理。1・3・4 は挙動が変わるため、規約 §2.2（No Feature Drop）に照らし、仕様をユーザーが決めてから実装する。

### Phase12: 機能追加

- **順序は R-4 → R-3**（`Release-Backlog.md` §3 の案を踏襲）。
- **R-4（#3）**: 新しい `ActionTypeId`、編集画面（`LaunchProgram` に近い）、保存、実行。前面化（`SetForegroundWindow`）には Windows の制限があり、実機での確認が多い。Phase10 で割り当て編集画面が統合されたため、種別の追加は 1 か所で済む。
- **R-3（#2）**: マクロの値の体系の拡張（座標を持つ値は、現行の整数 1 個に収まらない）、記録画面（`RecordBox`）、保存形式。着手前に **仕様の決定**が要る（絶対／相対、複数モニター、DPI）。保存形式を変えるため、古い版との互換を検討する（R-2 の `MacroExecuteMode` が前例）。
- 置く位置の理由: 新機能は利用者に見える価値があり、Phase13（内部構造の整理）より前に届けられる。新しいファイルは Phase14 の一括移動に含めればよい。
- **L1: ログの方針の策定（2026-11-03 追記。Phase12 の最初の Step）**:
  - 内容: ログのレベルの基準（Info／Debug／Trace、GUI に出すもの、警告とエラーの区別）、文言の書式、`[DI]` などの接頭辞のルールを 1 枚の文書にする。**コードは変えない**。
  - 規約 §2.3（ログの文言・レベルの完全な維持）の改定が前提。案: 「ログ整理の Phase（13b）では方針に沿った変更を許可する。それ以外の Phase では従来どおり維持する」。**ユーザーの承認を得てから改定する**。
  - 置く理由: Phase12 は新しいコードを書く Phase のため、方針を先に決めれば、新しいコードは最初から方針に沿い、13b で直す量が増えない。

### Phase13: `ControlService` の整理

- 範囲: 引数の集約、`Func<T>` 14 件の削減、`ServiceProviderHolder`（6 ファイル・13 件）の撤廃、F-5c（`running`／`suspending`）、F-1c、F-2c。
- 実測: 文書の「34 引数」は、手元の数え方で 30 台前半。**着手前に現状と再照合**する（§5）。
- **トリガー系 4 クラスの未使用コンストラクター引数（2026-11-03 追記。Phase11 の Debouncing 削除で生じた）**: `ControlButtonInputAction`／`ComboTriggerInputAction`／`MultiTapInputAction`／`TwoStageTriggerProcessor` は、`DebouncingMs` を使わなくなったが、呼び出し側とテストの変更を避けるため、コンストラクター引数を残した（各クラスに理由のコメントあり）。この Phase で Step にして整理する（呼び出し側とテストを同時に更新）。
- Phase14 の前に行う理由: ここで新設・移動する型の名前空間を、2 回直さないため。

### Phase13b: ログの一括整理（L2）

- 2026-11-03 追加。**Phase13 の後、Phase14 の前**。
- 範囲: アプリ全体のログ出力（実測: 666 か所・101 ファイル［当初の見積りは約 624 か所・96 ファイル］。`LogToGui`／`LogTrace`／`LogDebug` ほか）のレベルの振り分けと文言の統一。Phase12 の L1 で決めた方針に従う。
- ログの内容を検査するテストと黄金ファイルを更新する。規約 §2.3 の改定（L1）が前提。
- **L1 の結果（2026-11-03 追記）**: 方針は `Log-Policy.md`、現状の実測は `Phase12-Step1-Log-Inventory.md`、規約 §2.3 は改定済み。**実測は 666 か所・101 ファイル**（NLog 直接、`Console`／`Debug.WriteLine`、`LogToTray` を含む。`AppLogger` の 6 メソッドだけでは 546 か所）。API を汎用 `Log(level, message, options)` に集約するため、変更量は当初の見積り（M〜L）より大きい（**L**）。旧 API は薄いシムで残す期間を設け、段階的に移行する。旧 `LogToGui(warning, temporary)` と新 API の対応表、GUI・トレイに見えるログの変更一覧（承認用）、ログを検査する 14 テストファイルの書き換え（レベルと主要語で検査）を、13b の個別計画書で扱う。
- Phase13 の後にする理由: Phase13 は `ControlService` から引数やサービスを整理し、ログ文も一緒に動く。先に文言を揃えても、移動で無駄になる。
- Phase14 の前にする理由: Phase14 は `git mv` と名前空間の書き換えだけの機械的な変更にしたく、ログの内容変更を混ぜないほうが、差分の検証が簡単。
- 番号を「13b」とした理由: Phase14 以降の番号を繰り下げると、既存の文書の参照を直す範囲が広がるため。

### Phase14: 名前空間・フォルダの再編（旧 Phase11）

- **完了（2026-11-04。実機確認済み）**: 層別フォルダへ再編し、名前空間を `DS4Windows.<フォルダ>` に統一。詳細は `Phase14-Completion-Report.md`。

- 名前空間をフォルダパスと一致させる。`DI/` を Composition Root 専用にする（インターフェースは層別のフォルダへ）。
- 移動は `git mv` のみで、同じコミットで内容を変えない（R7）。名前空間の書き換えは、別のコミットにする。
- 現状（実測）: `DS4Windows` 195 ファイル、`DS4Windows.Actions` 108、`DS4Windows.Services` 103、`DS4Windows.DI` 72、`DS4WinWPF.*` 約 200。`DI/` に 81 ファイル。
- 構造変更（Phase12・13）の後に、機械的な一括変更を 1 回で行う。

### Phase15: UI 共通化

- コントローラー描画の共通化（FI の Phase B）、デザインシステム（06）、F-4・G-1 の判断。
- **追加（2026-11-04 承認。§8.2）**: 数値入力の共通部品（`SettingAxisRow`／`InputNumberBox`）、テーマの対称化とインラインカラーの除去、Color／Brush のリソース命名規則（検査を `XamlTestHelper` に加える）。見た目が変わる場合は項目ごとに承認を得る。
- Phase14 の後に行う理由: 新しい部品と XAML を、最終の名前空間で最初から作れる。XAML の名前空間（`xmlns:local`）の一括変更は、UI を触り終えてからのほうが安全。
- Phase11 の Issue #4 は `ProfileEditor` を触るため、F-4（分割）の前に済ませる。
- **追加（2026-11-05。`Phase14c-Plan.md` §7）**: 最初の Step で、R-09（UI 向けの窓口。`ControlService` の `DS4Controllers`／`slotManager`／`touchPad` などの置き換え）と、`MainWindow` のコンストラクターの引数（28）の整理を行う。層の依存の記録のみの規則 L7（UI → 第1層・第3層。Phase14c の後 30 件）の残りをここで減らす。続けて R-10（`IProfileSettingsService` の分割）・R-12（変更通知の基底）・R-14（ViewModel の生成）を、G-1・F-4 と同時に行う。

### Phase16: Layer 1 のカートリッジ化

- 9-H（第 1 層のデバイスクラスの分離）と、FI の Phase D（`IHidTransport` と `IInputReportParser` の分離）を統合する。K7b-1 の締め（Phase11）が前提（2026-11-05 にユーザーが新しいダンプなしを確認し、解消。`K7b1-Investigation.md` §8）。
- **追加（2026-11-04 承認。§8.1）**: 仮想パッド出力の ViGEm 分離。`VirtualPadService` を、ViGEm を包むポート（例: `IVirtualPadBackend`）の背後へ移し、偽の実装でドライバなしにテストできるようにする。利用者に見える変更はない。
- 05（プラグイン開発ガイド）は、この Phase の完了後に実装に合わせて整備する。
- **追加（2026-11-05。`Phase14d-Plan.md` §10）**: マクロの再生・繰り返し・タップ間隔の時間の扱いを `TimeProvider` 経由にし、テストが実時間で待たずに済むようにする（Debug ALL のテストの時間の残りの大半。本体の変更のため実機確認が要る）。
- **追加（2026-11-05。`Phase14c-Plan.md` §7）**: R-05 の後半（ライトバーの色の計算を第1層の `DS4LightBar.updateLightBar` から第2層の `ILightbarService` へ。強制表示の状態は Phase14c で `LightbarOverrides` にした）、`DS4Devices` のインスタンス化、R-17（`DS4State` など層間の状態の型）、記録のみの規則 L2（Core → 第1層・第3層。Phase14c の後 29 件）の残り、R-07・R-08・R-11（入力・出力のカートリッジ化と機種の設定の共通化。Phase14c で層ごとに分けた `ServiceRegistration` の partial ファイルを、カートリッジの登録の受け皿にする）。

### Phase17: Phase16 で見送った項目の実装（2026-11-10 追加。計画書は Phase16 の完了後に作成する）

Phase16 の追補（16-12・16-13）で見送った次の項目を、Phase17 で計画を立てて実装する。根拠と詳細は `Phase16-Plan.md` §12「16-12a」「16-13a」「16-13b-2／13c」。

| ID | 項目 | 内容 | 出典 |
|---|---|---|---|
| P17-1 | `MainWindow` の責務分割 | IPC の受け付けと結果の書き込み、テーマ・言語、更新の確認、プロファイルの適用などを ViewModel／サービスへ移し、`MainWindow`（21 引数）・`MainWindowFactory`（20）を 15 以下にする | 16-13b-2／13c |
| P17-2 | `EditorWindowFactory` の整理 | 編集画面群（18 引数）を 15 以下にする。P17-1 と同じ方針 | 16-13b-2 |
| P17-3 | `Mapping` の残りの分割 | 段 2（`ProcessStick`／`ProcessControlSettingAction`／`MapCustomAction`）を `MappingFrame`（共有の `_sampleState`／`_sampleFields` の入れ物）で切り出し、17 引数を 15 以下にする。毎フレームの経路のため、割り当て 0 のテストと A/B 計測を伴う | 16-13a（D-13-1 の本来の案） |
| P17-4 | 解析器の選択のカートリッジ化 | DS4 以外のデバイスが自分で持つ解析器の選択を、カートリッジから渡す形にする。デバイスのコンストラクター・初期化の順序の変更を伴う | 16-12a（K-e） |
| P17-5 | 設定画面の機種ごとの統合 | `*OptionsWrapper` と `ControllerRegDeviceOptsViewModel` の種類ごとの `switch` を、カートリッジの表から作る形にする。`EnableHomeLED`（SwitchPro・JoyCon の同一の 2 行）の共通化を含む。見た目は変えない | 16-12c／12d |
| P17-6 | DS4 以外の機種の実機確認 | DualSense・Switch Pro・Joy-Con・DS3 は実機がなく、差分テストだけで守っている。実機が得られたときに確認する（`Known-Issues-Ledger.md` §3） | Phase16 全体 |
| P17-7 | 入力機種の登録のカートリッジ化の完全な形 | 機種の追加が「デバイス＋カートリッジ＋登録 1 行」で済む形に近づける（モジュールの登録、設定の入れ物の登録を含む） | R-07 |
| P17-8 | ライトバーの設定画面の改良 | 2026-11-10 の実機確認で、動作は問題ないが、設定の画面（単色・バッテリー連動・虹色などの選び方）が分かりにくいとの指摘。どこが分かりにくいかの洗い出し（項目の並び、名前、ヘルプ文）から始め、`ILightbarService` の設定とそろえた分かりやすい画面にする。Phase15 のデザインシステムと数値入力の共有部品を使う | Phase16 実機確認（確認2） |

- 進め方: Phase16 の master への取り込みと実機確認の後に、`Phase17-Plan.md` を作成し、承認を得てから実装する（規約 4 章）。順序の案は P17-5 → P17-4 → P17-3 → P17-1・P17-2（機種に近い順に。画面の分割は最後）。P17-6 は実機の入手を条件とする。

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

1. 済み: 本書の承認、Phase11（4.2.4）、Phase12（4.3.0。`Phase12-Completion-Report.md`）。
2. 済み（2026-11-04）: モデル図の現状合わせ（§8.3。`Model-Diagram/01〜04`。Phase8 の部品を実線化、Phase11・12 を反映、行数を更新）。
3. 済み（2026-11-04）: Phase13（`ControlService` の整理。34 → 17 引数、`Func<T>` 2 件のみ、`ServiceProviderHolder` 撤廃、F-5c／F-2c 完了、F-1c 見送り）。`Phase13-Completion-Report.md`。実機確認（1 回）は Release ビルドの後に実施。
4. 済み（2026-11-04。実機確認済み・`Phase13b-Completion-Report.md`。CLI の件は未解決で台帳に記録）: Phase13b（ログの一括整理 L2。`Phase13b-Plan.md`、`Phase13b-Status.md`、`Phase13b-Log-Change-List.md`）。API を `AppLogger.Log` に集約し、`LogToGui`・サービスの `DebugMessage`・`ServiceDebugRelay` を廃止した。次: Phase14（名前空間・フォルダの再編）。
5. 済み（2026-11-05。実機確認済み・`Phase14b-Completion-Report.md`）: Phase14b（依存の向きの是正）。層の依存の違反は 108 → 18 件。続く Phase14b-2（残りの片付け。計画書 §10）で 0 件。次: Phase15（UI 共通化）。
6. 済み（2026-11-05。実機確認済み）: Phase14c（`Phase14c-Plan.md`）。値の型 8 つを `Core/Model` へ、`SyntheticDispatcher`・ライトバーの静的な配列・`ActionRegistry` を撤廃、`ServiceRegistration` の分割、記録のみの規則 L2・L7 は 155 → 59 件（件数のラチェット）、運用ルール（規約 §3.3 原則5、§4 のテストの命名）。次: Phase14d（テストの整理）→ Phase15。
7. 済み（2026-11-05。本体の変更なしのため実機確認なし）: Phase14d（テストの整理。`Phase14d-Plan.md` §10）。ソースを文字列で読むテストメソッド 391 → 275、撤去済み API の検査を `RemovedApiRuleTests` 1 か所へ、空振り 0（見つからないファイルで失敗）、Debug ALL のテスト 65.6 → 約 50 秒。本体の変更なし。次: Phase15。
8. 済み（2026-11-08）: Phase15-0（`Phase15-0-Plan.md`）。利用者向けのログ文言 85 件をリソースへ（画面は表示言語、ファイルは英語。日本語の訳つき）、Phase 名のテスト 303 件を機能名へ改名（対応表あり）、テストの読み取りの補助 17 個を `SourceText` へ、KI-4 の対策。並列実行は条件未達で直列のまま。次: Phase15（UI 共通化）。
8. 済み（2026-11-09。実機確認済み・master へ取り込み・push 済み）: Phase15（UI 共通化。`Phase15-Plan.md`、`Phase15-Completion-Report.md`）。UI 向けの窓口（R-09。L7 80→5）、テーマの命名と対称化、数値入力の共有スタイル、`ControllerVisualizerControl`、変更通知の共通化（R-12）、ViewModel の生成の集約（R-14）、設定サービスの領域の窓口と `ProfileEditor` の 8 領域への分割（R-10・G-1・F-4）。持ち越し: 特殊アクション一覧の見出し・並べ替えの整理、編集画面の外のスロット配列の直接参照 5 か所。実機確認で 4 件を修正（編集画面を開く時の InvalidCastException、出力ピッカーのホバー名の前後関係と点滅、言語切り替え時の開始・停止ボタン）。持ち越し 2 件は Phase15-8 で片付けた（`Phase15-8-Plan.md`。実機確認待ち）。次: Phase16。
9. 済み（2026-11-10。追補を含め DS4 で実機確認済み。master への取り込みは未実施）: Phase16（第 1 層・第 3 層のカートリッジ化。`Phase16-Plan.md`、`Phase16-Completion-Report.md`）。`IHidTransport`／`IInputReportParser`（Joy-Con・DS3 を含む）、ViGEm の分離、`TimeProvider`、`ILightbarService`、`IOutputDeviceCartridge`／`OutContTypeInfo`、引数の整理（`ActionBindingBuilder` 16→5、`Mapping` 25→20）、層の違反 L2・L7 を 0 件に。実機確認は DS4 のみ 1 回。
8. 小作業の持ち越し: テストの `[Collection("ServiceProviderHolder")]` の改名（**Phase14d-Step5 で実施済み**）。`ControlService` のスロット状態の転送（約 50 利用者）の整理は、Phase15 の最初の Step（R-09）で行う。
9. （済み）Phase15-0（Phase14 系の小さな持ち越しの片付け。`Phase15-0-Plan.md`。2026-11-05 承認、決定 H1〜H6 はすべて推奨案）: 利用者向けのログ文言のリソース化（Phase13b-2）、Phase 名のテストの改名、テスト固有の読み取りの補助の整理、テストの共有の状態の整理と並列実行の再計測。続けて Phase15 の本体。
10. 次（2026-11-10 追加）: Phase16 の追補分の実機確認 → master への取り込み → Phase17（Phase16 で見送った項目。§3 Phase17。計画書は未作成）。
11. 未解決の不具合は `Known-Issues-Ledger.md` にまとめた（2026-11-05。KI-1〜KI-5）。

## 8. 追加案（2026-11-04。FurtherImprovements との照合で見つかった抜け。**承認済み: 8.1〜8.3 すべて採用**）

`FurtherImprovements/01` の指摘のうち、Phase に明記されていないものと、モデル図の更新の扱い。Phase の順序と構成は変えない。

### 8.1 ViGEm 密結合の解消（01 の 3.3(2)）— **採用: Phase16 に追加**

- 内容: `VirtualPadService` が `Nefarius.ViGEm.Client` の具象型を直接参照している。仮想パッドの出力を、ViGEm を包むポート（例: `IVirtualPadBackend`）の背後へ移し、`VirtualPadService` は抽象にだけ依存させる。ドライバなしでテストできる偽の実装を用意する。
- Phase16 に置く理由: 第 1 層（入力）の通信／解析の分離（9-H）と対になる、第 3 層（出力）側の分離。第 3 層の依存方向の規約（第 2 層は第 3 層をポート経由で使う）にも合う。
- 規模: M。効果は、テスト容易性と将来のドライバ移行。利用者に見える変更はない。優先度は低い（任意としてもよい）。

### 8.2 Phase15 の内容の明記（01 の 3.2(2)(3)、3.4(2)）— **採用: Phase15 の範囲に追記**

- 数値入力の共通部品（`SettingAxisRow`／`InputNumberBox`。02 §2.2、06 §4.1）: Phase11 では、見切れの修正（幅・余白）だけを行った。部品の導入は未実施。
- テーマの対称化とインラインカラーの除去（06 §1・§5）: `DarkTheme.xaml` と `DefaultTheme.xaml` のキーを 1 対 1 にし、トリガー内の色の直書きをトークンにする。
- Color／Brush のリソース命名規則（06 §3.1）: `ForegroundColor` が Brush を指すような型の取り違えを防ぐ。新規の XAML の検査（`XamlTestHelper` を使う）に規則を加える。
- 既存の描画・見た目を変えない範囲で行い、利用者に見える変更が出る場合は、項目ごとに承認を得る。

### 8.3 モデル図の更新の扱い（`Model-Diagram/01〜04`）— **採用: 完了条件と、Phase13 の前の現状合わせ**

- 常時のルール（案）: 各 Phase の完了報告書の条件に「モデル図への反映」を加える（規約には「構造に関わる変更はモデル図に反映する」とあるが、Phase の完了条件には入っていない）。
- Phase13 の前に 1 回（案）: モデル図の現状合わせ。内容は、(1) Phase11・12 の反映（`IWindowFocuser`、`IWindowSystem`、`IMouseInputHook`、`WindowTargetMatcher`、`MacroPointerRecorder`、`FocusWindow`、`Debouncer` の廃止、電池の `UpdateBattery`）、(2) Phase8 の `ITriggerDispatchEngine`／`IActionRegistry`／`IActionBindingBuilder` を「未実装（点線）」から実線へ、(3) 行数の更新（`DS4Device.cs` 約 2,068、`ProfileSettingsViewModel.cs` 約 3,469 ほか）。コードの変更はなく、文書だけ。
- 各 Phase での更新: Phase13（`ControlService`、`Func<T>`、`ServiceProviderHolder`）、Phase14（名前空間・フォルダ。`DI/` の位置づけ）、Phase16（`IHidTransport` など）で、該当の図を更新する。

- 2026-11-10 Phase17 の見送り: 既存機種（DS4Device 系）の `ProtocolDevice` への移行は Phase18 候補（前提: 共通部品の切り出しと差分テスト）。`MainWindow` 20・`MainWindowFactory` 20・`Mapping` 17 は 15 以下にしていない（理由は `Phase17-Plan.md`）。
- 2026-11-10 Phase18 完了（ブランチ Phase17 の中）: 4 機種（DS4・DualSense・Switch Pro・`ProtocolDevice`）が `InputLoopDevice` の骨組みに載り、基底は抽象の `InputDeviceCore`（旧 `DS4Device`）になった（`Phase18-Plan.md`）。同日、到達できなかった 2 台結合の仕組み（Joint／`PrimaryDevice`）を削除した。
- 2026-11-10 Phase の並び（引数 6 以上を避ける根本対策の計画を受けて改定）: Phase19＝工場の廃止（`Phase19-Plan.md`）→ Phase20＝責務の分割（窓、続いて引数 10 以上の型。`Phase20-Plan.md`）→ Phase21＝同じ契約の依存を列で受ける（`Phase21-Plan.md`）→ **Phase22＝USB の Xbox 360 コントローラーを入力機種として足す（最後。旧 Phase19。`Phase22-Plan.md`。調査は `Xbox-Input-Research.md`）**。コードは未着手。
- 2026-11-10 構造の宿題（引数 15 以下）: `Mapping` を 17 → 15 にした（ログだけに使っていた `IManagedActionManager` と、個数の取得だけに使っていた `ISpecialActionRepository` を外した。ログの文言は `DefaultActionManager` へ移した。個数は `IProfileActionProvider.ActionDefinitionCount` から得る）。`MainWindow` と `MainWindowFactory` は 20 → 18（CLI の問い合わせの結果の書き込み `IIpcResultWriter` を `IpcCommandProcessor` が持ち、画面からのプロファイルの適用を `IProfileSlotApplier.ApplyFromUi` に集約した。`IIpcResultWriter` は `Processing/Abstractions` へ移した）。`ConstructorArityBaseline` の残りは `MainWindow` 18 と `MainWindowFactory` 18。15 以下にするには、画面の責務（トレイ・テーマ、通知、更新の確認、起動引数）を別の部品へ分ける必要があり、UI の実機確認を伴う。
- 2026-11-10 Phase19 完了（コード）: `MainWindowFactory` を `Composition` へ移し、`ActivatorUtilities` で窓をつくる形にして 18 → 1 にした。ほかの工場は基準内の多品目・実行時引数つきのため見送り（`Phase19-Plan.md` §6）。`ConstructorArityBaseline` の残りは `MainWindow` 18 のみ（Phase20）。実機の確認（起動、`-m` 起動、複製の名前入力欄）は未了。
- 2026-11-10 Phase20 の 20-0〜20-3 完了（コード）: `MainWindow` を 18 → 15 にし、`ConstructorArityBaseline` は空になった（`TrayAppearanceController`／`ProfileSelectionCoordinator`）。20-5 以降（窓以外の引数 10 以上の型）は未着手。実機確認は台帳へ登録済み。
- 2026-11-10 Phase20 完了: 窓の分割（18→15）、`InputReportProcessor` 15→13、しきい値を 12 へ。基準値の一覧に 6 型が凍結。次: Phase21（列で受ける。調査から）→ Phase22（Xbox）。
- 2026-11-10 Phase21 完了（調査のみ。列で受ける候補は 0 件、コード変更なし）。次: Phase22（Xbox 360 USB 入力）。
- 2026-11-10 構造の見直し: 引数の数の基準ではなく「単純で美しいか」で構造を評価し、変えるべき箇所 A〜G（設定のオブジェクト化、画面の ViewModel 化、`App` の分解、`Processing` 直下の整理、実行時の管理の整理、DS4 名の一般化、工場の整理）を `Structure-Review-2026-11.md` にまとめた。Xbox 対応（Phase22）は、これらの後の最後に回す。着手順の案は同文書 §3。
- 2026-11-10 Phase24（C: `App.xaml.cs` の分解）完了。詳細は Structure-Review-2026-11.md の更新履歴。次は Phase25（A: 設定のオブジェクト化）。
