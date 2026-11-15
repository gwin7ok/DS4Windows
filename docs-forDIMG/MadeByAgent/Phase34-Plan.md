# Phase34 計画書: 画面の MVVM 化（コマンド・結びつけ・ダイアログの窓口）— Phase26 の続き

作成日: 2026-11-12
状態: **着手済み（2026-11-15）。34-0（棚卸しの概数）と 34-1（土台）まで完了、34-2 以降は未着手**（D4 は (A) で確定。2026-11-13。2026-11-13 に、アプリ全体で MVVM が成り立つかの点検を受けて、範囲を 5 画面から全画面へ広げた。下の「MVVM の到達の定義」）
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 2 と「MVVM パターンの形骸化」）、`Structure-Review-2026-11.md` の B、`Phase26-Plan.md`（一部完了。残りを本 Phase が引き継ぐ）
前提: Phase29（29-10 が `MainWindow` のタイマーに触れる）と Phase33（スティックのパネルを作り直す）のあとに行う。PhaseX（入力側の図とボタン名）より前に行う。

## 目的
画面の code-behind（`.xaml.cs`）にある、判断・検査・保存・画面の開閉の指示を、ViewModel とサービスへ移し、WPF なしでテストできるようにする。code-behind には、見た目だけに関わる処理（ホバー、図の描画、フォーカス、ドラッグ）を残す。

「code-behind を `InitializeComponent()` だけにする」ことは目標にしない。コントローラーの図のホバーやヒットテストのように、見た目そのものの処理は View に置くのが正しい。

## MVVM の到達の定義（2026-11-13 追加）
MVVM は、アプリを 3 つの部屋に分けて、互いの知ることを決める設計である。本 Phase の終わりに、アプリ全体で次が成り立つことを目標にする。

| 部屋 | 置き場所 | 知ってよいもの | 知ってはいけないもの |
|---|---|---|---|
| Model | `Core`・`Processing`・`Output`・`Input` | 自分より下の層 | 画面（すでに `LayerDependencyRuleTests` が守る） |
| ViewModel | `UI/ViewModels` | Model の窓口（サービス・設定の領域の窓口）。画面に出す状態とコマンド | WPF の部品（`Window`・`Control`・`MessageBox`・`Application`・`Dispatcher`）。View |
| View | `UI/*.xaml(.cs)`、`UI/Controls` | ViewModel。見た目と、OS・WPF の受け口（ホバー、描画、フォーカス、ドラッグ、OS のメッセージ） | Model のサービスの直接の利用。判断・検査・保存 |

最初の版（5 画面だけ）は、View の側を軽くする計画だった。アプリ全体の点検（2026-11-13）で、次の 4 つの不足が見つかったため、範囲を広げた。

| # | 不足 | 実測 | 対処 |
|---|---|---|---|
| G1 | 範囲が 5 画面だけ | `.xaml.cs` は 37 ファイル。本計画の 5 画面（約 6,100 行）のほかに、32 ファイル（約 4,270 行）が計画の外にあった。うち 11 ファイル（`WelcomeDialog` 516 行、`SaveWhere`、`DupBox`、`About`、`PluginOutDevWindow`、`LanguageSelectDialog`、`ColorPickerWindow`、`ProfileNotificationWindow`、`LogMessageDisplay`、`RecordBoxWindow`、`ControllerReadingsControl`）は、ViewModel を 1 つも参照しない。code-behind がサービスを参照する数は、`AutoProfiles` 23、`SaveWhere` 15、`WelcomeDialog` 11、`UpdaterWindow` 8 など | 34-0 の棚卸しを全 37 ファイルへ広げ、34-6b を足す |
| G2 | ViewModel が WPF に依存している | `Visibility` の使用 33 か所、`System.Windows.Media`（ブラシ・色）18 ファイル、`System.Windows.Controls`（`MenuItem` など）11 ファイル、`Application.Current`／`Dispatcher` を直接使う 3 ファイル（`ControllerListViewModel`、`TrayIconViewModel`、`OutBinding`。`OutBinding` は `FindResource` で画面のリソースを引く）。WPF なしでテストできない部分が残る | 34-1 に UI スレッドの窓口を足し、34-6a で ViewModel を WPF から離す |
| G3 | 検査が足りない | 進み具合の検査は、XAML のイベント属性と窓の生成の数だけ。ViewModel が WPF を知らないこと、View が Model のサービスを直接使わないことを守る検査がない | 34-7 に、2 つの規則（V1・V2）を足す |
| G4 | 規約に書かれていない | `.github/copilot-instructions.md` に、3 つの部屋の規則がない | 34-8 で書く |

画面の見た目そのもの（図の描画、ホバー、ヒットテスト）は View に残す。`ControllerReadingsControl`（653 行）は描画の部品で、View として正しい。「code-behind を空にする」ことは目標にしない。

## 現状（調査。2026-11-12 の実測）
| 画面 | `.xaml.cs` の行数 | XAML のイベント属性 | `Command="{…}"` | code-behind のメソッド（うちイベント処理らしい名前） | 部品への直接の代入 |
|---|---|---|---|---|---|
| `ProfileEditor` | 1,735 | 24 | 0 | 90（30） | 56 |
| `MainWindow` | 1,836 | 41 | 0 | 100（42） | 47 |
| `BindingWindow` | 950 | 6 | 0 | 20（10） | 23 |
| `RecordBox` | 900 | 26 | 0 | 48（25） | 32 |
| `AssignmentEditor` | 702 | 10 | 0 | 17（11） | 22 |

- **コマンドの仕組みがない**: アプリ全体で、`Command="{…}"` はテーマのスクロールバーの 8 か所だけ。`ICommand` の実装は 1 つもない。
- **ダイアログの窓口がない**: `MessageBox.Show`（`ProfileEditor` に 5、`AssignmentEditor` に 2、`RecordBox` に 1）、`OpenFileDialog`／`SaveFileDialog`、`ColorPickerWindow` を、code-behind が直接作る。
- **画面を直接作る箇所**: `MainWindow` が `ChangelogWindow`・`ControllerRegisterOptionsWindow`・`RenameProfileWindow`・`UpdaterWindow` を直接作る。編集の子画面（出力ピッカー、割り当て編集画面、記録の窓、プロファイル編集画面）は、工場（`IEditorWindowFactory`、`AssignmentWindowFactory` など）を通る。
- **すでに済んでいること**: プロファイル編集画面の 8 つの領域は、パネル（UserControl）とサブ ViewModel に分かれている（Phase15）。判断の部品 5 つ（`TouchpadSwipeProfileRule`、`RumbleTestController`、`ReadingsDeadZoneSnapshot`、`UpdateCheckPolicy`、`ActionRumbleTestController`）がある（Phase26）。`MainWindow` のトレイとプロファイルの選択は、`TrayAppearanceController`・`ProfileSelectionCoordinator` にある（Phase20）。
- **長いメソッド**: `ProfileEditor` は `SortSpecialActionsList` 105 行、`Reload` 96 行、`EditActionBtn_Click` 91 行、`InputDS4` 73 行。`MainWindow` は `SetupEvents`・`PowerEventArrive` 各 61 行、`LateChecks` 60 行、`OnSourceInitialized` 49 行、`ShowProfileEditor` 45 行。

## 方針（決定事項の案）
1. **動作・見た目・文言は変えない**。移すだけにする。利用者に見える文言（`MessageBox` の本文など）は、そのまま窓口へ渡す。
2. **土台を先に作る**: コマンド（`ICommand` の小さな実装）と、ダイアログの窓口（確認・通知、ファイルの選択、色の選択）。窓口の契約は `UI/Abstractions`、実装は `UI/Services` に置く。ViewModel は窓口だけを受ける（Pure DI。規約 3.1）。
3. **イベント処理を 3 つに仕分けてから移す**（34-0）: (A) 判断・検査・保存を含む → ViewModel のコマンドへ。(B) 部品の表示・有効を切り替えるだけ → ViewModel のプロパティと結びつけへ。(C) 見た目だけ（ホバー、図、フォーカス、ドラッグ、スクロール）→ code-behind に残す。
4. **画面ごと・機能ごとの小さな Step** にし、各 Step で ViewModel のテストを足す（Phase26 の進め方）。実機確認が要る項目は `Known-Issues-Ledger.md` §3 へ足す。
5. コンストラクターの引数を増やさない。窓口を足すぶん、直接受けていたサービスを ViewModel の側へ寄せて、差し引きで減らす（`MainWindow` 15、`ProfileEditor` 12 は基準の上限にある）。
6. 進み具合は、件数のラチェット（XAML のイベント属性の数と、code-behind が直接作る窓の数を、基準値と一致させる検査）で守る。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 画面の順 | (A) `ProfileEditor` → `MainWindow` → `RecordBox` → `AssignmentEditor` → `BindingWindow`（行数とイベントの多い順。`BindingWindow` は名前つきの部品が 146 あり、見た目の処理が中心なので最後）／(B) 利用者の指定の順 |
| D2 | コマンドの実装 | (A) 自前の小さな `RelayCommand`（1 つの型。依存を足さない）／(B) `CommunityToolkit.Mvvm` を導入（依存が増える。`ObservableObject` は自前のものがすでにある） |
| D3 | 到達の目安 | (A) 仕分けの (A)(B) をすべて移す（code-behind に残るのは (C) だけ）／(B) 行数の目標（例: 各 500 行以下）を置く。行数は目的にしないほうがよい |
| D4 | Phase26 の扱い（**2026-11-13 にユーザーが (A) で確定**） | (A) Phase26 を「一部完了で終了」とし、残り（26-5 の続き）を本 Phase が引き継ぐ／(B) Phase26 を続け、本 Phase は土台（34-1）だけにする |
| D5 | `MessageBox` の文言 | (A) そのまま（英語の固定や、既存のリソース）／(B) この機会にリソース化（範囲が広がる。別の作業にするのを勧める） |
| D6 | 範囲（**2026-11-13 にユーザーが (A) で確定。分割するかどうかは、Phase34 の承認のときに決める**） | (A) 全画面と ViewModel の WPF 非依存化まで（34-6a・34-6b を含む）。本 Phase が大きくなるため、承認のときに、34-A（土台と 5 画面）・34-B（ViewModel の WPF 非依存化）・34-C（残りの画面と検査）の 3 つの Phase へ分けてもよい／(B) 5 画面だけ（最初の版。アプリ全体の MVVM にはならない） |
| D7 | ViewModel が知ってよい WPF の型の線引き | (A) `Window`・`Control`・`MessageBox`・`Application`・`Dispatcher`・`Visibility` を禁止。色・ブラシ（`System.Windows.Media`）は、値の型（`Color`）だけ許す。基準値を凍結し、減らす／(B) `System.Windows` 全体を禁止（色は文字列か独自の型。変更が広い）／(C) `Window`・`Control`・`MessageBox`・`Application`・`Dispatcher` だけ禁止（`Visibility` と `Media` は許す） |
| D8 | 残りの画面のうち、見た目の部品の扱い | (A) 34-0 の仕分けで (C)（見た目だけ）と判定したものは View のまま残し、棚卸しの表に理由を書く／(B) すべてに ViewModel を作る（効果が薄い） |

## 決定の記録（2026-11-13。ユーザーの回答）
D1＝A（ProfileEditor → MainWindow → RecordBox → AssignmentEditor → BindingWindow）、D2＝A（自前の `RelayCommand`）、D3＝A（判断・ボタン操作はすべて移し、見た目だけ残す）、D4＝A（Phase26 の残りを統合）、D5＝**B（`MessageBox` の文言を、この機会にリソース化）**、D6＝A（全画面。分割は承認時）、D7＝**B（`System.Windows` 全体を禁止）**、D8＝A（見た目だけの部品は View に残す）。
- D5＝B により、各画面の Step（34-2〜34-6b）に、`MessageBox`・ダイアログの文言を `Strings.resx`／`Strings.ja.resx` のキーへ移す作業を含める（表示言語で出し分ける。規約 2.3 の利用者向け文言の書き方に合わせる）。範囲が広がるため、文言の一覧（画面・件数）を 34-0 の棚卸しで先に作り、翻訳の確認を求める。既存の文言（英語固定のもの）の日本語訳は、ユーザーの確認を得る。
- D7＝B の注意: 規則 V1 は `System.Windows` 配下の型の参照を禁止するが、`System.Windows.Input.ICommand`（コマンドの契約。WPF の部品ではなく、`System.ObjectModel` にある）だけは許す。色は WPF の `Color`／`Brush` ではなく、`Core/Model` の色の型（`DS4Color` など）か文字列で持ち、View のコンバーターが WPF の型へ変える。`Visibility` は `bool` にする。34-6a の範囲が、最初の版より広がる。
- D6＝A の分割（34-A 土台と 5 画面／34-B ViewModel の WPF 非依存化と文言のリソース化／34-C 残りの画面と検査）は、**2026-11-15 に決定: 分割せず、1 つの Phase34 で、Step ごとのコミットで順に進める**。
- D5＝B の英語固定の文言の日本語訳（2026-11-15）: 私（エージェント）が訳を付け、途中では止まらずに、訳の一覧（画面・英文・日本語訳）を記録する。最後の実機確認のときに、まとめて確認する。
- 進め方（2026-11-15）: 承認済み。コミットは自動で行い、確認事項が生じたときだけ止める。実機確認は、Phase34・40・35・37 を終えたあとに、最後にまとめて行う。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 34-0 | 棚卸し: **全 37 の `.xaml.cs`**（2026-11-13 に拡張）のイベント処理と、部品への直接の代入を、(A)(B)(C) に仕分けた表を作る。あわせて、(1) ViewModel の WPF 依存（`Visibility`、`Media`、`Controls`、`Application`／`Dispatcher`）を、ファイルごと・理由ごとに一覧にする、(2) View のサービス参照を一覧にする。画面ごとの Step と、D6 の分け方を確定する | — | 未着手 |
| 34-1 | （完了 2026-11-15）土台: コマンド（D2）、ダイアログの窓口（確認・通知／ファイルの選択／色の選択）、`MainWindow` が直接作る 4 つの窓の生成の窓口、**UI スレッドの窓口**（`IUiDispatcher`。`UI/Abstractions`。ViewModel が `Application.Current.Dispatcher` を直接使わないため。偽物は、その場で実行する形）。テスト用の偽物を用意する | 窓口の契約ごとの偽物。コマンドの実行可否の通知。UI スレッドの窓口の偽物 | 未着手 |
| 34-2 | `ProfileEditor`: 保存・キャンセル・適用、特殊アクションの一覧（並べ替え、編集、削除。`SortSpecialActionsList`、`EditActionBtn_Click`）、プリセット、読み込み直し（`Reload` のうち、値の集め方）を ViewModel へ。図のホバーと `InputDS4`（入力による選択）は (C) として残す | 保存の検査、一覧の並べ替え、編集の開始の判断 | 未着手 |
| 34-3 | `MainWindow`: 設定タブのボタン、プロファイルの操作（新規・編集・複製・削除・書き出し・読み込み）、更新の確認、窓の生成（`ChangelogWindow` ほか）を、ViewModel と窓口へ。電源・デバイスの通知（`PowerEventArrive`、`OnSourceInitialized`）は、OS のメッセージの受け口なので View に残し、判断だけを部品へ | プロファイルの操作の判断、更新の確認、電源の復帰の判断 | 未着手 |
| 34-4 | `RecordBox`（記録の画面）: 記録の開始・停止、保存・読み込み、手順の編集を `RecordBoxViewModel` のコマンドへ。キーとマウスの生の入力の受け口は View に残す | 記録の状態の移り変わり、保存の検査 | 未着手 |
| 34-5 | `AssignmentEditor`（割り当て編集画面）: 種別の切り替え、保存の検査を ViewModel へ | 種別ごとの保存の検査 | 未着手 |
| 34-6 | `BindingWindow`（出力ピッカー）: 選択の確定、Extras の値の集め方を ViewModel へ。図の上のボタン（名前つきの部品 146）の見た目は残す | 選択の確定の判断 | 未着手 |
| 34-6a | （2026-11-13 追加。G2）ViewModel を WPF から離す。(1) `Application.Current`／`Dispatcher` を `IUiDispatcher` へ（`ControllerListViewModel`、`TrayIconViewModel`）。(2) `OutBinding` の `FindResource`（テーマのブラシの名前から色を引く）を、ViewModel は「リソースのキー名」を返し、View のコンバーターが引く形へ。(3) `Visibility` を `bool`（と、View 側の標準のコンバーター）へ。(4) `MenuItem` など `Controls` の部品を作っている箇所（`PresetMenuHelper`、`TrayIconViewModel`、`GyroSettingsViewModel`、`TouchpadSettingsViewModel` など）を、項目のデータ（名前・コマンド・入れ子）を返す形へ。メニューの組み立ては View 側。D7 の線引きに従う。保存する値と表示の文言は変えない | ViewModel のテストが、WPF の STA スレッドなしで動くこと（新規。`System.Windows.Controls` を使わずに作れること）。既存の ViewModel のテストが不変 | 未着手 |
| 34-6b | （2026-11-13 追加。G1）残りの画面（32 ファイル、約 4,270 行）。34-0 の仕分けに従い、3 つの群で進める。**群 1（ViewModel がなく、サービスを直接使う画面）**: `WelcomeDialog`（516 行）、`SaveWhere`、`DupBox`、`PluginOutDevWindow`、`LanguageSelectDialog`、`About`。**群 2（画面の判断が多い）**: `AutoProfiles`（425 行。ViewModel はあるが、サービスの参照が 23）、`UpdaterWindow`（364 行）、`StickCalibrationWindow`、`OutputSlotManagerControl`、`LanguagePackControl`、`WindowTargetEditor`、`PresetOptionWindow`、`RenameProfileWindow`、`ControllerRegisterOptionsWindow`、`FirstLaunchUtilWindow`、`ChangelogWindow`。**群 3（設定のパネル 8 つ。XAML のイベントが多い）**: `TouchpadSettingsPanel`（イベント 47）、`GyroSettingsPanel`（26）、`StickSettingsPanel`（14）ほか。イベントを結びつけ（Binding）へ。**対象外（View に残す。D8 の (A)）**: `ControllerReadingsControl`、`TouchButtonUserControl`、`AxialStickUserControl`、`ColorPickerWindow`、`LogMessageDisplay`、`ProfileNotificationWindow`、`RecordBoxWindow`。画面ごとに、判断を ViewModel へ、窓の生成・ダイアログを窓口へ | 画面ごとに、ViewModel のテストと、画面を開くテスト（結びつけのエラーがないこと） | 未着手 |
| 34-7 | 検査（2026-11-13 に拡張）: (1) XAML のイベント属性の数と、code-behind が直接作る窓・ダイアログの数を、画面ごとの基準値と一致させるラチェット。(2) **規則 V1**: `UI/ViewModels` の型は、`Window`・`Control`・`MessageBox`・`Application`・`Dispatcher`（D7 の線引き）を参照しない。今ある違反を基準値に凍結し、実数との一致を検査する（`LayerDependencyRuleTests` と同じ形。ArchUnitNET）。(3) **規則 V2**: View（`UI` の直下と `UI/Controls` の、`Window`・`UserControl` を継承する型）は、`Core`・`Processing`・`Output`・`Input` のサービスの型を直接参照しない（ViewModel・`UI/Abstractions` の窓口・値の型は可）。今ある違反を基準値に凍結する。減らしたら、同じコミットで基準も減らす。34-6a・34-6b の完了で、V1 の基準値は 0 をめざす | 新規 | 未着手 |
| 34-8 | 文書: `01-Structure.md`（UI の型の表）、規約 3.1（コマンドとダイアログの窓口の使い方を 1 段落。2026-11-13 追加: 上の「MVVM の到達の定義」の 3 つの部屋の規則 V1・V2）、`Structure-Review-2026-11.md` の B の更新履歴 | `ModelDiagramGuardTests` | 未着手 |

## 確認の方法
- 各 Step: 途中は `--filter "Category!=Slow&Category!=Benchmark"`、Step の終わりに Debug ALL（警告 0・エラー 0）。
- 実機: 画面ごとに、移したボタンと操作を一通り。特に、(1) プロファイルの保存・キャンセル・未保存での閉じ方、(2) 特殊アクションの追加・編集・削除・並べ替え、(3) マクロの記録と保存、(4) 設定タブの各項目、(5) 更新の確認、(6) スリープからの復帰とコントローラーの抜き差し。

## リスク
- 範囲が全画面になったため、本 Phase は大きい（約 10,400 行の code-behind。ViewModel の WPF 非依存化を含む）。D6 の (A) で、承認のときに、34-A（土台と 5 画面）・34-B（ViewModel の WPF 非依存化）・34-C（残りの画面と検査）へ分ける案を勧める。34-6a は、ViewModel の見た目の値（色、表示の有無）に触れるため、実機で画面を一通り見る（テーマの切り替えを含む）。
- 中〜高。結びつけ・コマンドの名前の誤りは、コンパイルで見つからず、画面で「押しても動かない」形で現れる。34-7 のラチェットと、画面を開くテスト（結びつけのエラーが出ないこと）で守る。
- 範囲が広い（5 画面、約 6,100 行）。画面ごとに独立して進められるので、途中で止めても害はない。
- `MainWindow` は、起動・終了・電源・デバイスの通知が集まる。Phase35（終了の手順）と同じファイルに触れるため、同時に進めない。

## 履歴
- 2026-11-15: 34-2・34-4・34-5（一部）を実施し、実施の記録へ追記した。
- 2026-11-13: 決めること（D 項目）のユーザーの回答を「決定の記録」に記録した。
- 2026-11-13: D6 は (A)（全画面と ViewModel の WPF 非依存化まで）で確定。34-A／34-B／34-C への分割は、承認のときに決める。D7・D8 は未回答。
- 2026-11-13: D4 は (A) で確定。アプリ全体の MVVM の点検を受けて、「MVVM の到達の定義」、決めること D6〜D8、Step 34-6a（ViewModel の WPF 非依存化）・34-6b（残りの画面）、34-0・34-1・34-7・34-8 の拡張を足した。
- 2026-11-13: Phase 番号の付け替え（旧 Phase35 → 34）。Phase26 の残りを統合した（D4 の (A)）。
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。

## 実施の記録
### 2026-11-15
- 分割（D6）: ユーザーの「次へ進んで」を承認とみなし、34-A（土台と 5 画面）・34-B（ViewModel の WPF 非依存化と文言のリソース化）・34-C（残りの画面と検査）の順に進める。本ブランチ `Phase34` は 34-A の土台まで。
- 34-0（棚卸しの概数。`.xaml.cs` の行数／XAML のイベント属性／`MessageBox`・ファイル選択の直接の呼び出し／サービスらしい参照の概数）: `MainWindow` 1,849／37／13／124、`ProfileEditor` 1,736／21／8／44、`BindingWindow` 942／6／0／8、`RecordBox` 900／22／3／6、`AssignmentEditor` 702／10／3／16、`WelcomeDialog` 516／6／0／13、`AutoProfiles` 425／11／1／23、`UpdaterWindow` 364／4／4／8。ViewModel のうち、`Application.Current`／`Dispatcher` を使うのは `ControllerListViewModel`（`ContextIdle` の優先度つきの呼び出しがあるため、`IUiDispatcher` に優先度の指定が要る）、`OutBinding`（`FindResource`）、`TrayIconViewModel`（`Dispatcher` を引数で受ける。`IUiDispatcher` へ置き換える）。`System.Windows` を参照する ViewModel は 35 ファイル。`MessageBox`・ファイル選択を直接作る箇所は、`UI` の View に約 47 か所（最多は `MainWindow` の 13、`ProfileEditor` の 8）。
- 34-1（完了）: `RelayCommand`（`UI/ViewModels/Util`。`CommandManager` に依存せず、`RaiseCanExecuteChanged` で通知）、`IUiDispatcher`＋`WpfUiDispatcher`、`IDialogService`（`ShowMessage`／`Confirm`／`PickOpenFile`／`PickSaveFile`）＋`WpfDialogService`＋`DialogIcon`＋`FileDialogOptions` を新設し、DI に登録した（`ServiceRegistration.Ui`）。テストの偽物は `FakeDialogService`・`ImmediateUiDispatcher`。色の選択の窓口は、使う側（34-6b の `ColorPickerWindow` は View に残す方針）が決まるまで作らない。
- 次: 34-2 以降（画面ごとに、判断を ViewModel のコマンドへ移し、`IDialogService`・`IUiDispatcher` を使う）。`MainWindow` が最大で、Phase35（終了の手順）と同じファイルに触れるため、先に Phase35 を済ませてから 34-3 に入るほうがよい。

### 2026-11-15（続き。ブランチ `Phase34-2`）
- 34-2（ProfileEditor。完了）: 特殊アクションの「追加・編集後の一覧の更新」「削除の確認」「複製できないときの通知」「エクスポート」を `SpecialActionsListViewModel`（`AddSavedAction`／`ReplaceEditedAction`／`RemoveSelectedWithConfirm`／`DuplicateSelectedWithNotice`／`PickAndExportActions`）へ移した。確認・通知の文言は `Strings.resx`・`Strings.ja.resx` のキー（`SpecialAction_RemoveConfirm`／`SpecialAction_DuplicateNotSaved`）にした（D5＝B。日本語訳は、最後の実機確認で確認する一覧に入れる）。`IDialogService` は `ActionViewModelFactory` が渡す。テスト: `SpecialActionsListViewModelDialogTests`。残り（ProfileEditor に残る直接の呼び出し）: 名前の入力チェックの `MessageBox`、リセットの確認、スティック校正の案内、起動プログラムの選択（`GeneralSettingsViewModel` へ移す）。34-3 のあとに行う。
- 34-4（RecordBox。完了）: プリセットの読み書きのファイル選択（`PickAndLoadPreset`／`PickAndSavePreset`）と、移動の記録の上限の通知（`NotifyMoveRecordLimitReached`）を `RecordBoxViewModel` へ。`ProfileEditViewModelFactory` が `IDialogService` を渡す。テスト: `RecordBoxViewModelDialogTests`。
- 34-5（AssignmentEditor。一部完了）: 起動プログラムの選択（`LaunchProgramViewModel.PickProgram`）を移した。位置を指定して出す「同名のアクションがある」の通知は、描画上の位置の指定のため View に残す（D8＝A）。
- 34-6（BindingWindow）: 直接の `MessageBox`・ファイル選択が 0 件のため、このブランチでは変更なし。34-6a／34-6b（ViewModel の WPF 非依存化・残りの画面）は、`MainWindow`・`App` に触れる Phase35 のあとで、34-3・34-7・34-8 と一緒に行う（計画の変更。実機確認のあとの最終まとめで記録する）。
