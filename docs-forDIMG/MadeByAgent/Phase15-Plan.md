# Phase15 個別計画書: UI 共通化

作成日: 2026-11-08
状態: **草案（承認待ち）。決定事項 P1〜P8 の回答を得てから Step 15-1 に着手する**
ブランチ: `Phase15`（承認後に master c0c266f4 から作成）
根拠: `Phase11-and-Beyond-Roadmap.md` §3 Phase15・§8.2、`Repository-Review-2026-11-05.md`（R-09・R-10・R-12・R-14）、`FurtherImprovements/02` §2.1・`03` §3（Phase B）・`06`（デザインシステム）、`Phase9-Deferred-Ledger.md`（F-4・G-1）、規約 `.github/copilot-instructions.md`

## 1. 目的

UI（4-a）を次の 3 点で整える。

1. **依存の向き**: UI は第 2 層だけに依存する（規約 1 章）。今は具象の `ControlService` 経由で第 1 層・第 3 層へ抜けている（R-09）。
2. **共通の部品とルール**: 同じ見た目・同じ処理を画面ごとに書いている所を、部品とルールにまとめる（コントローラー描画、数値入力、テーマ、変更通知）。
3. **直しやすさ**: 肥大した `ProfileSettingsViewModel`／`IProfileSettingsService`／`ProfileEditor` を、今後の機能追加で触りやすい大きさにする（R-10、F-4、G-1）。

Phase16（第 1 層のカートリッジ化）より前に行う理由: Phase16 で第 1 層の具象（`DS4Device` など）が動くため、UI が第 1 層を直接持たない形を先に作っておくと、Phase16 の影響範囲が UI に及ばない。

## 2. 対象の実測（2026-11-08、master c0c266f4）

| # | 項目 | 実測 |
|---|---|---|
| 1 | UI の規模 | `UI/` に XAML 31・`.cs` 144。`ProfileSettingsViewModel` 3,477 行、`ProfileEditor.xaml.cs` 2,230 行、`MainWindow.xaml.cs` 2,104 行、`BindingWindow.xaml.cs` 971 行 |
| 2 | UI → 第 1 層・第 3 層（規則 L7、記録のみ） | 30 件（`LayerDependencyBaseline.RecordOnlyL7Count`）。`using DS4Windows.Input／Output` を持つ UI ファイル 20 |
| 3 | UI → 具象 `ControlService` | 24 ファイル。`DS4Controllers`／`slotManager`／`touchPad`／`HotplugController`／`OutputslotMan` の利用 23 か所 |
| 4 | `MainWindow` のコンストラクター引数 | 約 26（計画時の記述は 28。着手時に数え直す） |
| 5 | View のコードビハインドでの ViewModel の生成 | `new …ViewModel(` 28 か所（レビュー時 43 か所） |
| 6 | 変更通知の手書き | `public event EventHandler …Changed` が `ProfileSettingsViewModel` 81、`SettingsViewModel` 23、`OutBinding` 13、`SlotDeviceEntry` 10 ほか。`INotifyPropertyChanged` を使うファイルは 13 |
| 7 | `IProfileSettingsService` | 239 行（配列プロパティ約 90、スロット引数のメソッド約 42）、利用 78 ファイル |
| 8 | コントローラー描画の二重実装 | `BindingWindow.xaml` に `Canvas.Left` 39 か所＋`BindingWindow.xaml.cs` の `InitDS4Canvas()`（座標のハードコード）、`ProfileEditor.xaml` に 39 か所、`ControllerReadingsControl.xaml` に 7 か所。共通の部品はない（FI の `ControllerVisualizerControl` は未実装） |
| 9 | テーマの対称性 | `DarkTheme.xaml` のキー 103、`DefaultTheme.xaml` のキー 31。Dark だけにあるキー 72、Default だけにあるキー 0（Dark の多くはスタイル・テンプレートで、Default はそれを持たず WPF の既定に任せている可能性がある。15-2 で内訳を調べる） |
| 10 | 色・ブラシの命名 | `SolidColorBrush` なのに `…Color` で終わるキーが `DarkTheme.xaml` に 14（例: `BackgroundColor`、`BorderColor`、`ControlColor`）。FI 06 §3.1 の「Brush は `…Brush`、Color は `…Color`」に反する |
| 11 | 色の直書き | XAML の `Background="#…"`／`Foreground="White"` などの属性は `BindingWindow` 2、`ControllerReadingsControl` 9、`ProfileNotificationWindow` 3（計 14）。テーマ内・スタイルの `Value="#…"` は Dark 34・Default 15（トリガー内の直書きを含む。テーマファイル内の定義そのものは直書きに数えない） |
| 12 | 数値入力 | `SettingAxisRow`／`InputNumberBox` は未実装。Phase11 で見切れの修正（幅・余白）だけ行った |
| 13 | 検査の道具 | `XamlTestHelper`（テストにある）。XAML のリソースキー・名前空間の検査に使える |

## 3. 守るもの

- 現在の機能を落とさない（規約 2.2）。ログの文言・レベルを変えない（2.3。新しい文言は `LogUser`＋リソース）。
- **見た目が変わる変更は、項目ごとに承認を得る**（ロードマップ §8.2）。承認が要るものは、スクリーンショットか説明つきの一覧で示す（Step 15-2・15-4 の冒頭）。
- XAML の Binding の誤りはビルドで検出できない。UI の Step ごとに、**実機（または手元のアプリ起動）で画面を確認**する。確認の項目は各 Step に書く。
- ファイルの移動は `git mv` のみのコミット、名前空間の書き換えは別のコミット（R7）。1 ファイル 1 型。
- 新しい静的な窓口・Service Locator を作らない（2.1、3.1 の Pure DI）。ViewModel・サービスはコンストラクターで依存を受ける。
- 層の依存の検査（`LayerDependencyRuleTests`）の基準値は、減らしたら同じコミットで更新する（一致検査）。
- Issue #4（Phase11 の持ち越し）は `ProfileEditor` を触るため、F-4 の前に済ませる（ロードマップ）。内容は着手時に台帳から確認する（P7）。

## 4. 決定事項（承認待ち。推奨を太字）

| # | 項目 | 選択肢 |
|---|---|---|
| P1 | Phase15 の範囲 | **A**: §5 の Step 15-1〜15-7 すべて（G-1 は「最も独立した領域を 1 つ切り出して確認してから続けるか決める」の形）／ B: R-09・コントローラー描画・数値入力・テーマ・命名規則まで（15-1〜15-4）。R-10・R-12・R-14・F-4・G-1 は Phase15b として別計画／ C: 15-1〜15-2 だけ（依存の向きと命名・テーマ。見た目が変わらない部分）で止める |
| P2 | UI 向けの窓口の形（R-09） | **A**: 用途別の小さな契約を第 2 層に置く（スロットの読み取り `IControllerSlotQuery`、接続・切断などの操作、HID 周りは必要なものだけ）。`ControlService` はインターフェースの背後へ／ B: `IControlService` を 1 つ作って UI に渡す（作業は少ないが、UI が全機能に依存する形が残る） |
| P3 | `UI/Controls/` フォルダーの新設（共通部品の置き場所） | **A**: 規約 §3.4 R1 に `UI/Controls`（再利用する UserControl）を追加し、名前空間 `DS4Windows.UI.Controls` に `ControllerVisualizerControl`／`SettingAxisRow` を置く／ B: `UI/` 直下に置く（フォルダー規約は変えない。UI 直下のファイルが増える） |
| P4 | コントローラー描画の共通化の進め方（FI Phase B） | **A**: 座標のメタデータ（相対比率）＋単一コントロール。`BindingWindow` で先に導入し、座標の見た目を変えないこと（画像の重ね合わせ比較）を確認してから `ProfileEditor` へ／ B: `BindingWindow` だけ導入し、`ProfileEditor` は据え置き／ C: 見送る（座標のハードコードを残す） |
| P5 | テーマの対称化（FI 06 §1・§5） | **A**: Default が持たないキーの内訳（スタイル・テンプレートか、色か）を調べ、色・ブラシのキーは 1 対 1 にし、スタイル・テンプレートは共通ファイルへ寄せるか Dark 専用のままにするかを個別に決める（見た目が変わる場合は承認）／ B: キー名の 1 対 1 だけ（色とブラシに限る）／ C: 見送る |
| P6 | Color／Brush の命名規則の適用（FI 06 §3.1） | **A**: `…Color` で終わるブラシ 14 個を `…Brush` へ改名（XAML の参照を一括置換）し、`XamlTestHelper` を使うテストで「Brush は `Brush`、Color は `Color` で終わる」を検査／ B: 検査だけ追加し、既存の 14 個は許可リストで凍結（ラチェット）／ C: 規則を書くだけ |
| P7 | 変更通知の共通化（R-12）の方式 | **A**: 自前の小さな基底（`SetProperty` ヘルパー）を `UI/ViewModels` に置く。依存を増やさない／ B: CommunityToolkit.Mvvm の `[ObservableProperty]`（ソース生成。外部パッケージが 1 つ増える）／ C: 見送る。`XxxChanged` イベントの購読（コードビハインド）を書き換えるのは 15-5 で、1 つの ViewModel から始めて確認してから広げる |
| P8 | F-4（`ProfileEditor` の分割）と G-1（`ProfileSettingsViewModel` のサブ VM 化） | **A**: 15-6 で、ジャイロの領域 1 つだけを切り出して画面全体を確認し、続けるかをそのとき決める（`Phase9-Deferred-Ledger.md` の条件どおり）／ B: Phase15 では行わず、R-10（設定サービスの分割）まで／ C: 領域を全部切り出す |
| P9 | 実機確認 | **A**: 見た目・操作が変わる Step（15-2・15-3・15-4・15-5・15-6）の終わりに各 1 回、Release ビルド 2 か所で確認項目を確認／ B: Phase15 の最後に 1 回だけ（Step ごとの戻しが難しくなる） |
| P10 | 取り込み | **A**: Phase14c〜15-0 と同じ。Debug ALL の後、実機確認を済ませたら master へ fast-forward で取り込み push／ B: 取り込みの前に確認する |

### 4.1 決定結果（2026-11-08）

| # | 決定 | 推奨との差 |
|---|---|---|
| P1 | A: 15-1〜15-7 すべて | 推奨どおり |
| P2 | A: 用途別の小さな契約 | 推奨どおり |
| P3 | A: `UI/Controls` を新設（規約 R1 に追加） | 推奨どおり |
| P4 | A: `BindingWindow` 先行 → `ProfileEditor` | 推奨どおり |
| P5 | A: 内訳を調べて個別に決める | 推奨どおり |
| P6 | A: 14 個を改名＋検査を追加 | 推奨どおり |
| P7 | A: 自前の小さな基底 | 推奨どおり |
| P8 | **C: 全領域を切り出す（G-1 を最後まで行う）** | 推奨（A: ジャイロ 1 領域で判断）と異なる。15-6 を、領域ごとに分けて進める形に改める（§5 の 15-6）。Binding 約 336 か所・コードビハインドの参照約 151 か所の書き換えを含む |
| P9 | **B: 最後に 1 回だけ** | 推奨（A: Step ごと）と異なる。P8 と組み合わせると不具合の原因の Step を特定しにくいため、§6 の対策を追加（Binding の自動検査を先に作る、領域ごとに起動して自分で確認する） |
| P10 | A: 実機確認後に ff で取り込み push | 推奨どおり |

## 5. Step 構成（案）

各 Step の終わりに Debug ALL（Benchmark を除く全件）。途中は `Category!=Slow&Category!=Benchmark`。各 Step の前に、その Step の細部（対象ファイルの一覧）をこの計画書へ追記する。

| Step | 内容 | 出典 | 見た目の変更 | 実機確認 |
|---|---|---|---|---|
| 15-1 | **UI 向けの窓口（R-09）**。`ControlService` の `DS4Controllers`／`slotManager`／`touchPad`／`HotplugController`／`OutputslotMan` の UI 側の利用 23 か所を、第 2 層の読み取り用・操作用の契約へ置き換える。`ControlService` のスロット状態の転送（約 50 利用者）を整理する。`MainWindow` のコンストラクター引数（約 26）を、画面ごとのまとまりへ整理する。L7 の 30 件を減らし、基準値を更新する | R-09、F-5c の残り、ロードマップ | なし | 起動・接続・切断・スロット表示 |
| 15-2 | **テーマと命名**。Color／Brush の命名（P6）、Default が持たないキーの内訳調査と対称化（P5）、XAML の色の直書き 14 か所と、トリガー内の色の直書きのトークン化。新しい XAML の検査を `XamlTestHelper` に足す。**冒頭で、見た目が変わる項目の一覧を示して承認を得る** | FI 06 §1・§3・§5、ロードマップ §8.2 | 項目ごとに承認 | ダーク・ライトの両テーマで全タブ |
| 15-3 | **数値入力の共通部品**（`SettingAxisRow`／`InputNumberBox`）。まず 1 つの画面（`ControllerReadingsControl` など、数値の見切れを修正した所）で導入し、`ProfileEditor` へ広げる。部品は `UI/Controls`（P3） | FI 02 §2.2、06 §4.1 | 原則なし（見切れの回避を維持） | 数値が 3 桁（-128〜255）で欠けない |
| 15-4 | **コントローラー描画の共通化**（FI Phase B）。B-1: 座標のメタデータ（相対比率）、B-2: `ControllerVisualizerControl`、B-3: `BindingWindow` の `InitDS4Canvas()` の置き換え → `ProfileEditor` の置き換え。置き換えの前後で、ボタンの位置が変わらないこと（画面の重ね合わせ、ホバー・クリックの当たり判定）を確認 | FI 02 §2.1、03 §3 | 原則なし（座標を変えない）。変わる場合は承認 | 両画面でボタンのホバー・クリックがズレない（DS4・Xbox360 の表示） |
| 15-5 | **変更通知の共通化（R-12）と ViewModel の生成（R-14）**。小さな基底を作り、`SettingsViewModel`（23）のように小さい物から移して Binding を確認し、`ProfileSettingsViewModel`（81）へ進める。View のコードビハインドの `new …ViewModel(`（28 か所）を、画面ごとのファクトリー（`IViewModelFactory` の隣）へ集める | R-12、R-14 | なし | Binding の抜け（値の表示・保存・Undo の取りこぼし） |
| 15-6 | **設定サービスの分割（R-10）と、G-1／F-4（P8: 全領域）**。`ProfileSlotSettings ForSlot(int)` と領域ごとの小さな型（`Sticks`、`Triggers`、`Gyro`、`Touchpad`、`Lightbar`、`Output` ほか）を足し、領域ごとに「設定サービスの利用者の移行 → サブ ViewModel の切り出し → `ProfileEditor` の該当部分の分割」を 1 領域＝1 サブ Step で行う（最も独立したジャイロから）。領域ごとに Debug ALL とアプリの起動確認。最後に配列プロパティを消す。Issue #4 を F-4 の前に済ませる | R-10、9-G、F-4 | なし | `ProfileEditor` の全タブ（保存・読込・プリセット）。最後に 1 回（P9） |
| 15-7 | **仕上げ**。規約・モデル図（01〜04）・ロードマップ・台帳・`CHANGELOG.md` を更新、Debug ALL、Release ビルド 2 か所、実機確認（P9）、取り込み（P10）、`Phase15-Completion-Report.md` | 規約、ロードマップ §8.3 | — | 最終 1 回 |

順序の理由:
- 15-1 を最初に置くのは、以後の Step で触る ViewModel・画面が「第 2 層の窓口」だけを受ける形になるため（ロードマップの追記どおり）。
- 15-2 の後に 15-3・15-4: 新しい部品を、整ったテーマのトークンで最初から書ける。
- 15-5・15-6 は同じ画面（`ProfileEditor`）を触る。Binding の誤りはビルドで見つからないため、領域を 1 つずつ確認しながら進める。
- 15-4 と 15-6 はどちらも `ProfileEditor` を触るので、同時に進めない。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| Binding の誤りがビルドで見つからない（P8＝全領域、P9＝実機確認は最後に 1 回、の組み合わせで特に大きい） | **15-5 の前に、Binding のパスを XAML から集め、ViewModel の公開プロパティと照合するテストを `XamlTestHelper` で作る**（Binding の誤りを自動で検出する）。小さい ViewModel・領域から移す。各サブ Step の終わりに、エージェントがアプリを起動して該当の画面を開き、例外が出ないこと（出力ウィンドウの Binding エラー）を確認する。実機確認は最後の 1 回だが、そこで見つかった不具合は Step の境界のコミットで切り分けられるよう、サブ Step ごとにコミットする |
| テーマの対称化で、ライト／ダークの見た目が意図せず変わる | 15-2 の冒頭に変更点の一覧（キー、旧 → 新の色）を示して承認を得る。承認していない色は変えない |
| コントローラー描画の置き換えで、ボタンの位置がずれる | 旧座標（ハードコード）を新メタデータの初期値へ機械的に換算し、換算の結果を旧座標と比較するテストを書く。画面の重ね合わせで確認 |
| `ControlService` の窓口化で、スロット状態の取りこぼしが出る | 利用者を 1 か所ずつ移し、移すたびに Debug ALL。`ControlService` の公開メンバーは、利用者が 0 になってから消す（規約 2.1 の過渡期の原則） |
| 改名・移動で、パスを文字列で持つテストが落ちる | Phase14d の整理で減らしてある。残りは `SourceFileLocator` の厳格な検索で、見つからなければ落ちる（空振りしない） |
| テストの実行順が状態に依存する | Phase15-0 の方針どおり、テストは自分で状態を初期化し、終わりに戻す |
| 範囲が大きく、1 回のレビューで終わらない | P1 の B／C で分割できる。各 Step の終わりで取り込み可能な状態を保つ |

## 7. 完了条件

- Debug ALL が警告 0・エラー 0・全件合格。
- 層の依存の検査: 禁止の規則は 0 件のまま。記録のみの規則 L7 は 30 件から減っている（目標値は 15-1 の細部で決める）。
- モデル図 01〜04 に、新しい窓口・部品・規則を反映済み。
- 実機確認の項目（各 Step）が「問題なし」。
- `Phase15-Completion-Report.md`、`CHANGELOG.md`、ロードマップ・台帳を更新。

## 8. 次の手順

1. このドラフトに対して、P1〜P10 を回答する。
2. 承認後、`Phase15` ブランチを作り、Step 15-1 の細部（対象ファイル・契約の案）を追記して着手する。

## 9. 進捗

| Step | 状態 |
|---|---|
| 計画書 | 草案作成・P1〜P10 回答済み（2026-11-08。P8＝全領域、P9＝最後に 1 回） |
