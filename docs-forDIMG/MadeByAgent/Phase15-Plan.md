# Phase15 個別計画書: UI 共通化

作成日: 2026-11-08
状態: **決定事項 P1〜P10 に回答済み（2026-11-08。§4.1）。Step 15-1 の細部を追記してから着手する**
ブランチ: `Phase15`（master c0c266f4 から作成）
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
| 15-2 | **テーマと命名**。Color／Brush の命名（P6）、Default が持たないキーの内訳調査と対称化（P5）、XAML の色の直書き 14 か所と、トリガー内の色の直書きのトークン化。新しい XAML の検査を `XamlTestHelper` に足す。**冒頭で、見た目が変わる項目の一覧を示して承認を得る** | FI 06 §1・§3・§5、ロードマップ §8.2 | 項目ごとに承認 | ダーク・ライトの両テーマで全タブ | ← **完了**（見た目は変えていない。①ブラシ 15 個を `…Color` → `…Brush` に改名［XAML 16 ファイルと文字列 4 か所］。②対称化: Dark だけのキー 72 個はすべて Dark 内のスタイルが使うもので参照切れはなく、色 14 個だけ Default にも足した［未使用。値は Dark と同じか、システム色］。③Dark のスタイル内の直書き 45 か所を `Template.*` のトークン［Color 型の影 1 個を含む 18 個］に、画面の外の直書き 15 か所を `Readings.*`／`Notification.*`／`KeyHighlight.*`［両テーマに同値］に置き換え。Default のドキュメント表示の直書き 15 か所も `Template.Document.*`［Dark と Default で別値］へ。④テスト `ThemeTokenRuleTests`［Color／Brush の名前、両テーマのキーの一致、直書きはテーマの定義だけ、両テーマの読み込み］を追加。Debug ALL 緑。実機の目視確認は最後の 1 回にまとめる）
| 15-3 | **数値入力の共通部品**（`SettingAxisRow`／`InputNumberBox`）。まず 1 つの画面（`ControllerReadingsControl` など、数値の見切れを修正した所）で導入し、`ProfileEditor` へ広げる。部品は `UI/Controls`（P3） | FI 02 §2.2、06 §4.1 | 原則なし（見切れの回避を維持） | 数値が 3 桁（-128〜255）で欠けない | ← **完了**（調査: 設計書の `SettingAxisRow`［スライダー＋数値の組］に合う箇所は実画面にほぼなく［ProfileEditor のスライダー 7 個は数値入力なし］、`UI/Controls` と `SettingAxisRow` は作らなかった［実在しない形の部品を作らない。ユーザー判断で、実際に繰り返している 2 つに絞った］。①読み取り値ラベルの共有スタイル `ReadingValueLabel`／`…Wide`／`…Centered`［`ControllerReadingsControl` の 18 個。幅 34／40／38 と余白は元のまま。表示は DynamicResource］。②数値入力の最小幅の共有スタイル `NumberInputSmall`／`Medium`／`Large`／`Wide`［50／60／80／100。xctk の UpDown 112 個。`BindingWindow` は暗黙のスタイルを持つため対象外］。両スタイルは Dark／Default に同じ寸法で定義。見た目は変えていない。テスト `ThemeTokenRuleTests` に寸法の一致と「UpDown の最小幅は共有スタイル」を追加、見切れ検査は共有スタイルを解決して維持。Debug ALL 緑）
| 15-4 | **コントローラー描画の共通化**（FI Phase B）。B-1: 座標のメタデータ（相対比率）、B-2: `ControllerVisualizerControl`、B-3: `BindingWindow` の `InitDS4Canvas()` の置き換え → `ProfileEditor` の置き換え。置き換えの前後で、ボタンの位置が変わらないこと（画面の重ね合わせ、ホバー・クリックの当たり判定）を確認 | FI 02 §2.1、03 §3 | 原則なし（座標を変えない）。変わる場合は承認 | 両画面でボタンのホバー・クリックがズレない（DS4・Xbox360 の表示） | ← **完了**（ユーザー判断: 設計書どおり全面共通化。`UI/Controls` を新設［規約 R1 に追加］し、`HitboxRegion`／`ControllerLayout`／`ControllerLayouts`［Xbox360・DualShock4・DualShock4Editor の配置表］／`ControllerVisualizerControl`［配置表からボタンを生成する Canvas。同じ ID のボタンは配置を切り替えてもインスタンスを保つ。`ScaleToFit` で比率の拡縮も可能、既定は表の値そのまま］を作った。座標は「基準の大きさでの画素値」で持ち、旧 BindingWindow の XAML／`InitDS4Canvas()` と旧 ProfileEditor の XAML の値を一字一句写した［旧 → 新の一致をテストで固定。ProfileEditor は git の旧版と照合済み］。`BindingWindow` の 26 個・`ProfileEditor` の 34 個のボタンのフィールド名・結び付けは元のまま。見た目と当たり判定は変えていない。Debug ALL 緑。ボタンのホバー・クリックのズレの確認は、最後の実機確認に含める）
| 15-5 | **変更通知の共通化（R-12）と ViewModel の生成（R-14）**。小さな基底を作り、`SettingsViewModel`（23）のように小さい物から移して Binding を確認し、`ProfileSettingsViewModel`（81）へ進める。View のコードビハインドの `new …ViewModel(`（28 か所）を、画面ごとのファクトリー（`IViewModelFactory` の隣）へ集める | R-12、R-14 | なし | Binding の抜け（値の表示・保存・Undo の取りこぼし） | ← **R-12 は完了**（2026-11-09。`UI/ViewModels/ObservableObject`［`SetProperty`・`OnPropertyChanged`］を新設し、`SettingsViewModel`［22 個］・`ProfileSettingsViewModel`［83 個］・`AxialStickControlViewModel` の「名前＋Changed」のイベントを `PropertyChanged` へ集めた。購読側の `MainWindow`・`ProfileEditor` は `PropertyChanged` の名前で振り分ける。テスト `ViewModelChangeNotificationTests`。**R-14 も完了**（2026-11-09。画面のコードビハインドの `new …ViewModel(` 28 か所を `IViewModelFactory` へ集めた。ダイアログ類は ViewModel を受け取る形にし、`BindingWindow`・`AssignmentEditor` は不要になった `LightbarOverrides`・`IOutputSlotService` の引数を外した。テスト `ViewCodeBehind_DoesNotConstructViewModels`））
| 15-6 | **設定サービスの分割（R-10）と、G-1／F-4（P8: 全領域）**。`ProfileSlotSettings ForSlot(int)` と領域ごとの小さな型（`Sticks`、`Triggers`、`Gyro`、`Touchpad`、`Lightbar`、`Output` ほか）を足し、領域ごとに「設定サービスの利用者の移行 → サブ ViewModel の切り出し → `ProfileEditor` の該当部分の分割」を 1 領域＝1 サブ Step で行う（最も独立したジャイロから）。領域ごとに Debug ALL とアプリの起動確認。最後に配列プロパティを消す。Issue #4 を F-4 の前に済ませる | R-10、9-G、F-4 | なし | `ProfileEditor` の全タブ（保存・読込・プリセット）。最後に 1 回（P9） | ← **15-6a（ジャイロ）・15-6b（タッチパッド）・15-6c（トリガー）・15-6d（スティック）完了**（2026-11-09。詳細は `Phase15-6-Plan.md`）
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
| 15-1-0 | 完了（2026-11-08）。`ConnectionType` を `git mv`（fe6e143e）→ 名前空間の書き換え（f195e623）。入れ子の列挙型 4 つを `Core/Model` のトップレベルへ（cb050b1a）: `DS4Device.ExclusiveStatus` → `DeviceExclusiveStatus`、`OutSlotDevice` の `AttachedStatus`／`ReserveStatus`／`InputBound` → `OutputSlotAttachedStatus`／`OutputSlotReserveStatus`／`OutputSlotInputBound`（名前が一般的すぎるため接頭辞を付けた。値は同じ）。L7 は 30 → 28（`ConnectionType` と列挙型の分、基準値を更新）。Debug ALL: 警告 0・エラー 0（基準値の更新前に L7 の一致検査だけが期待どおり失敗し、更新後に合格）。規則 L8 は `UiControlServiceReferenceRuleTests`（型参照 14 を凍結。層の規則の表ではなく型を指定する規則のため、別のテストにした） |

## 10. Step 15-1 の細部（UI 向けの窓口。R-09）

作成日: 2026-11-08（S1〜S3 回答済み。§10.7）

### 10.1 目的と終了時の姿

- UI から、具象の `ControlService` と、第 1 層の `DS4Device`、第 3 層の `OutSlotDevice`／`OutputSlotManager` などの**機器の型への参照をなくす**。UI は第 2 層の契約（`Processing/Abstractions`）だけを見る。
- 終了時の数値の目標:
  - UI で `ControlService` 型を参照する型: 14 → 0（新しい禁止の規則 L8 で固定。§10.5。「24 ファイル」はコメントや未使用の記述を含む文字列検索の値で、実際の型参照は 14 型。15-1-0 で実測）
  - 規則 L7（UI → 第 1 層・第 3 層）: 30 → **6**（残りは `DS4State`／`DS4StateExposed`／`SixAxis`／`DS4SixAxis` の値の型。Phase16 の R-17 で `Core/Model` へ移す。S2）
  - `MainWindow` のコンストラクター引数: 26 → できるだけ減らす（S3。引数ごとに「除去するか残すか」をユーザーが判断する）

実測した L7 の 30 件の内訳: `DS4Device` 11 件（`BindingWindow`、`ControllerReadingsControl`、`MainWindow`、`ProfileEditor`、`CompositeDeviceModel`、`ControllerHolder`、`ControllerListViewModel`、`ControllerRegDeviceOptsViewModel`、`DeviceListItem`、`RecordBoxViewModel`、`TrayIconViewModel`）、`DualSenseDevice` 2 件、`DS4SixAxis`（`ProfileEditor`）1 件、`ConnectionType` 1 件、`NativeMethods` 1 件、出力側 8 件（`OutSlotDevice`／`OutputSlotManager`／`OutputDevice`／`Xbox360OutDevice`）＝計 24 件を 15-1 で消す。残り 6 件は `DS4State` 系（`ControllerReadingsControl` 4、`StickCalibrationWindow` 1、`RecordBoxViewModel` 1）。

### 10.2 UI の `ControlService` 利用の実測（2026-11-08）

| 使うもの | 回数 | 使い道（主な場所） | 置き換え先 |
|---|---|---|---|
| `DS4Controllers[slot]`（`DS4Device`） | 17 | 電池の表示（`MainWindow`）、接続中か（`MainWindow.IsSlotConnected`）、振動テスト（`BindingWindow`、`ProfileEditor`）、ジャイロ較正のリセット（`ProfileEditor`）、読み取り表示（`ControllerReadingsControl`）、`RecordBoxViewModel`、`ControllerRegDeviceOptsViewModel` | `IControllerSlotQuery`／`IControllerSlotCommands`／`IControllerReadingsSource` |
| `slotManager`（`ControllerColl`、`CollectionLocker`、`ReverseControllerDict`） | 5 | コントローラー一覧（`ControllerListViewModel`）、トレイ（`TrayIconViewModel`） | `IControllerSlotQuery`（ロックの内側で作った一覧を返す） |
| `ServiceStarted`／`PreServiceStop`／`RunningChanged`／`Start`／`Stop`／`HotPlug` | 6／6／1／4／3／1 | 開始・停止とイベント | **既存の `IServiceLifecycle` にすでにある**。差し替えるだけ |
| `HotplugController`（`DS4Device` を渡すイベント） | 4 | 一覧の更新、トレイ、自動プロファイルの再確認 | `IControllerSlotQuery.SlotChanged`（`IControllerSlotView` を渡す） |
| `getDS4State`／`getDS4StateTemp` | 3 | 読み取り表示、スティックの較正 | `IControllerReadingsSource` |
| `MappingService`（`GetBoolMappingExternal`、`SetCurveAndDeadzone`） | 3 | マクロ記録の入力判定、読み取り表示 | 既存の `IMapping`／`IControlActivityReader` を注入 |
| `recordingMacro` | 2 | マクロ記録中の印 | `IMacroRecordingState`（新） |
| `IsMeasuringProcessingDelay`／`GetProcessingDelay` | 2／1 | 処理遅延の表示 | `IControllerReadingsSource` |
| `touchPad[0]` | 1 | `RecordBoxViewModel` のタッチパッドの状態 | `IControllerReadingsSource`（`GetBoolMappingExternal` の引数に使うため一緒に） |
| `OutputslotMan` | 1 | `MainWindow` が出力スロットの画面へ渡す | `IOutputSlotService`（既存。15-1-4） |
| `DeviceOptions` | 1 | 機種ごとの設定画面 | 設定は `Core` の型。直接注入 |
| `ProfileSettingsService`、`LogDebug` | 1／1 | 読み取り表示 | 既存の `IProfileSettingsService`、`AppLogger` |

加えて、`UI/Services` の 3 つのファクトリー（`ViewModelFactory`、`EditorWindowFactory`、`MainWindowFactory`）が `ControlService` を受けて中継している。

### 10.3 サブ Step

各サブ Step の終わりに Debug ALL（途中は `Category!=Slow&Category!=Benchmark`）。1 サブ Step は 1 コミット以上。

| サブ Step | 内容 |
|---|---|
| 15-1-0 | **準備**。(a) 値の型を `Core/Model` へ: `ConnectionType`（`TrayIconViewModel` が使う）、`DS4Device.ExclusiveStatus`、`OutSlotDevice` の `ReserveStatus`／`AttachedStatus`／`InputBound`（入れ子の列挙型はトップレベルに出して移す。規約 R2）。移動は `git mv` のみのコミット → 名前空間の書き換えは別コミット（R7）。(b) 新しい禁止の規則 L8（UI → `DS4Windows.Processing.ControlService` の型）を `LayerDependencyRuleTests` に足し、今の 24 ファイルを許可リストに凍結 |
| 15-1-1 | **機器の読み取りの窓口**: `IControllerSlotView`（1 台の表示用の情報と通知）と `IControllerSlotQuery`（一覧・スロット指定の取得・追加／取り外しの通知）を第 2 層に作る（§10.4）。`ControllerListViewModel`、`TrayIconViewModel`、`CompositeDeviceModel`、`ControllerHolder`、`DeviceListItem`、`ControllerRegDeviceOptsViewModel`、`MainWindow`（電池・接続中か）を移す | ← **完了**（実装: `IControllerSlotView`／`IControllerSlotQuery`［`SlotCount`、`Snapshot`、`SlotChanged`］、`ControllerSlotView`［機器ごとに 1 つ。イベントは面を送り主にして転送］、`ControllerSlotQuery`。L7 の基準値 28 → 21、L8 の許可リスト 14 → 10 型、MainWindow の引数は 29。テスト `ControllerSlotQueryTests` を追加。Debug ALL 緑）
| 15-1-2 | **機器への操作の窓口**: `IControllerSlotCommands`（振動テスト、ジャイロ較正のリセット）。`BindingWindow`、`ProfileEditor`（`DualSenseDevice` の振動方式の分岐を含む）を移す。既存の `IDeviceRumbleService`（`DS4Device` を受ける）は、中身で使う | ← **完了**（実装: `IControllerSlotCommands`［`IsConnected`、`SetRumble`、`SetHeavyRumble`／`SetLightRumble`、`ApplyDualSenseRumbleOptions`、`ResetGyroCalibration`］と `ControllerSlotCommands`。`DualSenseDevice` の振動方式の分岐は実装側へ移した。L7 の基準値 21 → 17、L8 の許可リスト 10 → 9 型［`ProfileEditor` は `ControllerReadingsControl` の初期化で 15-1-3 まで残る］。`EditorWindowFactory` と `ProfileEditor` の引数が 1 つ増えた。Debug ALL 緑）
| 15-1-3 | **読み取り表示・マクロ記録の窓口**: `IControllerReadingsSource`（状態のコピー、処理遅延）と `IMacroRecordingState`。`ControllerReadingsControl`、`StickCalibrationWindow`、`RecordBox`／`RecordBoxViewModel` を移す。`DS4State` 系の型の参照は残す（S2）。**毎フレーム近くの経路なので、割り当てを増やさない**（コピー先は呼び出し側の持つ配列） | ← **完了**（実装: `IControllerReadingsSource`［`TryRead`＝待機を取って呼び出し側の配列へコピー、`GetCurrentState`、`IsMeasuringProcessingDelay`］、`ControllerReadingInfo`［構造体。較正カウント・入力遅延・警告値・処理遅延］、`IMacroRecordingState`［`IsRecording`、`CanReadInput`、`IsPressed`］と実装 `ControllerReadingsSource`／`MacroRecordingState`。`ControllerReadingsControl`・`StickCalibrationWindow`・`RecordBox`・`RecordBoxViewModel`・`ProfileEditor`・`EditorWindowFactory`・`ViewModelFactory` から `ControlService` を外した。L7 の基準値 17 → 13、L8 の許可リスト 9 → 2 型［`MainWindow`、`MainWindowFactory`］。Debug ALL 緑）
| 15-1-4 | **出力スロットの窓口**: `IOutputSlotView`（種類・予約状態・接続状態・機能の有無・通知）を作り、`IOutputSlotService`（既存）から返す。`SlotDeviceEntry`、`CurrentOutDeviceViewModel`、`OutputSlotManagerControl`、`PluginOutDevWindow`、`MainWindow` の `OutputslotMan` の受け渡しを移す | ← **完了**（実装: `IOutputSlotView`［種別名・種別・接続状態・常設設定・入力の割り当て・XInput 番号・2 つの通知］と実装 `OutputSlotView`［`Output/Services`。スロットごとに 1 つ］、`IOutputSlotService` に `SlotViews`／`SlotAssigned`／`SlotUnassigned` を追加［スロット管理の通知を、最初の購読か参照の時点で一度だけ購読して転送］。`SlotDeviceEntry`・`CurrentOutDeviceViewModel`・`OutputSlotManagerControl` から出力層の具象型を外し、`MainWindow` は `OutputslotMan` を渡さなくなった。L7 の基準値 13 → 6［15-1-7 の目標値に到達］。Debug ALL 緑）
| 15-1-5 | **残りの利用の置き換え**: `MainWindow` などの `Start`／`Stop`／`HotPlug`／イベントを `IServiceLifecycle` へ。`HookWindowMessages` の `NativeMethods.HidD_GetHidGuid`（HID の通知の登録）は、窓のメッセージの登録を既存の `IWindowSystem` か専用の窓口へ寄せるかを、この Step の冒頭で調べて決める（L7 の 1 件） | ← **完了**（`MainWindow` の Start／Stop／HotPlug は `IServiceLifecycle` へ、`DeviceOptions` は `IAppSettingsService` へ。`HookWindowMessages` の `NativeMethods.HidD_GetHidGuid` は、同じ辺を既に持つ `EnvironmentService` に `IEnvironmentService.HidClassGuid` として置いた［新しい層またぎの辺を増やさない］。L7 の基準値 6 → 5。`MainWindow` の `controlService` フィールドは未使用になり、15-1-6 で引数ごと外す。Debug ALL 緑）
| 15-1-6 | **`ControlService` の公開メンバーの整理と、UI のファクトリーの引数から `ControlService` を外す**。利用者が 0 になったメンバー（`DS4Controllers`／`touchPad`／`slotManager` の転送ほか。約 50 利用者の残りを数える）を、規約 2.1 の過渡期の原則に従い、**利用者が本当に 0 になったものだけ**削除。L8 の許可リストを 0 にする | ← **完了**（`MainWindow`・`MainWindowFactory` の `ControlService` 引数を外し［`MainWindow` の引数は 28 個。S3 の 1 つずつの判断の前に、未使用になった 1 つが減った］、L8 の許可リストを廃止して「参照 0 件」の検査にした。`ControlService` の利用者が 0 件になった転送を削除: `recordingMacro`／`IsMeasuringProcessingDelay`／`ProcessingDelayMs`／`GetProcessingDelay`／`OutputslotMan`／`MappingService`／`getDS4State`／`getDS4StateTemp`／`DeviceOptions`／`outputDevices`／`GetController`／`VirtualPadService`／`Start`／`Stop`／`HotPlug`／`PrepareAbort`／5 つのイベント、公開の `DS4Controllers`／`touchPad`／`slotManager`［`App` の `Stop`／`PrepareAbort` は `IServiceLifecycle` へ。テストの利用者は `IDeviceSlotState`／`IInputReportProcessor`／`IVirtualPadService` へ移した］。Debug ALL 緑）
| 15-1-7 | **`MainWindow` のコンストラクター引数の整理**（S3）。26 個を 1 つずつ調べ、**引数ごとに「除去する案」か「残す案」かを、理由・代替案・影響の大きさ（変更するファイル数）とともに一覧で示し、ユーザーが 1 つずつ判断する**（判断前に除去しない）。除去が決まったものを実施する。L7 の基準値を 6 へ更新。モデル図（01〜04）の更新。完了の記録 | ← **完了**（引数は 28 → 21。判断の結果: 未使用の `IProfileXmlStore`／`IProfileLoadTarget` を除去、`LightbarOverrides` は `ServiceLifecycle.Suspending` の setter が `shuttingdown` も切り替える形にして除去、`SettingsViewModel`／`ControllerListViewModel`／`CurrentOutDeviceViewModel` の生成を `IViewModelFactory` へ移して `ILogSettingsApplier`／`IStartupEntryService`／`IProfileSettingsService`／`IOutputSlotService` を除去。`OutputSlotManagerControl.SetupDataContext` は ViewModel を受ける。残す 21 個は画面が直接使うもの。L7 の基準値は 15-1-5 で到達済みの 5 のまま。テスト `Suspending_SwitchesTheLightbarShuttingDownFlagTogether` を追加。モデル図 01〜04 を更新。Debug ALL 緑）

### 10.4 契約の案（第 2 層。`Processing/Abstractions`。実装は `Processing/Services`）

```csharp
// 1 台の機器の「UI が見る面」。DS4Device を包むアダプター（DS4Device ごとに 1 つだけ作る）
public interface IControllerSlotView
{
    int SlotIndex { get; }
    string DisplayName { get; }
    string MacAddress { get; }
    ConnectionType ConnectionType { get; }       // Core/Model（15-1-0）
    int Battery { get; }
    bool Charging { get; }
    int Latency { get; }
    bool Synced { get; }
    bool IsRemoving { get; }
    ExclusiveStatus ExclusiveStatus { get; }     // Core/Model（15-1-0）
    bool IsJoined { get; }                       // JointDeviceSlotNumber の有無
    event EventHandler BatteryChanged;
    event EventHandler ChargingChanged;
    event EventHandler MacAddressChanged;
    event EventHandler Removed;
}

public interface IControllerSlotQuery
{
    int SlotCount { get; }
    IControllerSlotView GetSlot(int slot);                    // 接続なしは null
    IReadOnlyList<IControllerSlotView> Snapshot();            // slotManager のロックの内側で作る
    event EventHandler<ControllerSlotChangedEventArgs> SlotChanged; // 旧 HotplugController
}

public interface IControllerSlotCommands
{
    void SetRumble(int slot, byte heavy, byte light);         // 振動テスト（DualSense の方式の調整を含む）
    void ResetGyroCalibration(int slot);                      // 合体したデバイスも含む
}

public interface IControllerReadingsSource
{
    bool TryCopyStates(int slot, DS4State baseDst, DS4State interDst, int profileSlot, out long calibratingCount); // 旧 ReadWaitEv の取り扱いを中に隠す
    bool IsMeasuringProcessingDelay { get; set; }
    double GetProcessingDelay(int slot);
}

public interface IMacroRecordingState { bool IsRecording { get; set; } }
```

- 名前と細部は 15-1-1〜3 の着手時に、実際の利用箇所に合わせて詰める（上は案）。
- **アダプターは `DS4Device` ごとに 1 つだけ**（`ConditionalWeakTable`）。`CompositeDeviceModel` などが「同じ機器か」を参照の同一性で判定しているため、呼ぶたびに新しい物を返さない。
- 一覧は、従来の `slotManager.CollectionLocker`（`ReadLocker`）の**内側で作った複製**を返す。UI が機器の集合のロックを直接持たない。
- `IServiceLifecycle` の `HotplugController`（`DS4Device` を渡す）は第 2 層の内部のイベントとして残し、UI はもう購読しない。

### 10.5 検査

- 規則 L8（UI → `ControlService`）: 禁止の規則として追加。15-1-0 で 24 ファイルを許可リストに凍結し、15-1-6 で 0 にする（`LayerDependencyBaseline` の一致検査）。
- L7 の基準値: 30 → 6（15-1-7）。途中のサブ Step ごとに実数へ更新（減ったら更新する運用）。
- 新しい契約には単体テストを書く。アダプターの同一性（同じ機器は同じ物）、`SlotChanged` の通知、ロック下の複製、`Removed` の転送と購読の解除を確かめる。

### 10.6 リスク

| リスク | 対策 |
|---|---|
| 機器の一覧と通知の競合（接続・切断が UI の更新と重なる） | 従来のロックの取り方を変えない。複製は同じロックの内側で作る。`Removal` の購読は、アダプターの `Removed` に 1 対 1 で移し、購読の重複・解除漏れを増やさない |
| 読み取り表示の経路（約 60 回／秒）で割り当てが増える | `TryCopyStates` は呼び出し側の配列へコピーするだけ。アダプターは作り置き。15-1-3 で、割り当てを増やさないことをテストで確認 |
| `DS4Device` のイベントの購読元が変わる（電池・MAC・取り外し） | アダプターが元のイベントを購読して転送する。アダプターの寿命を機器と同じにする（機器が取り外されたら購読を解く） |
| 値の型の移動（`ConnectionType` など）で、第 1 層・第 3 層の多数のファイルの `using` が変わる | 移動と名前空間の書き換えは別コミット（R7）。機械的な変更として扱い、差分は `git diff -M` と型の検査で確認 |
| 実機でしか見えない不具合（電池の表示、一覧の追加・削除、振動テスト） | P9 の対策（サブ Step ごとにアプリを起動して確認）。最後の実機確認の項目に、一覧・電池・トレイ・振動・較正・出力スロットを入れる |

### 10.7 決定結果（2026-11-08）

| # | 決定 | 推奨との差 |
|---|---|---|
| S1 | A: インターフェース `IControllerSlotView` で包む（アダプターは機器ごとに 1 つ） | 推奨どおり |
| S2 | A: `DS4State` 系は 15-1 では残す（L7 の残り 6 件。Phase16 の R-17 で解消） | 推奨どおり |
| S3 | **引数を 1 つずつ判断する**。なるべく減らす。除去が難しく残す場合は、残す理由と除去のための代替案を示し、ユーザーが除去か残すかを 1 つずつ決める | 推奨（15 以下の数値目標）と異なる。数値の目標は置かない |
