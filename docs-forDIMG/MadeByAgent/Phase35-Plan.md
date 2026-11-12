# Phase35 計画書: 画面の MVVM 化（コマンド・結びつけ・ダイアログの窓口）— Phase26 の続き

作成日: 2026-11-12
状態: **未着手。承認待ち**
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 2 と「MVVM パターンの形骸化」）、`Structure-Review-2026-11.md` の B、`Phase26-Plan.md`（一部完了。残りを本 Phase が引き継ぐ）
前提: Phase29（29-10 が `MainWindow` のタイマーに触れる）と Phase32（スティックのパネルを作り直す）のあとに行う。PhaseX（入力側の図とボタン名）より前に行う。

## 目的
画面の code-behind（`.xaml.cs`）にある、判断・検査・保存・画面の開閉の指示を、ViewModel とサービスへ移し、WPF なしでテストできるようにする。code-behind には、見た目だけに関わる処理（ホバー、図の描画、フォーカス、ドラッグ）を残す。

「code-behind を `InitializeComponent()` だけにする」ことは目標にしない。コントローラーの図のホバーやヒットテストのように、見た目そのものの処理は View に置くのが正しい。

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
3. **イベント処理を 3 つに仕分けてから移す**（35-0）: (A) 判断・検査・保存を含む → ViewModel のコマンドへ。(B) 部品の表示・有効を切り替えるだけ → ViewModel のプロパティと結びつけへ。(C) 見た目だけ（ホバー、図、フォーカス、ドラッグ、スクロール）→ code-behind に残す。
4. **画面ごと・機能ごとの小さな Step** にし、各 Step で ViewModel のテストを足す（Phase26 の進め方）。実機確認が要る項目は `Known-Issues-Ledger.md` §3 へ足す。
5. コンストラクターの引数を増やさない。窓口を足すぶん、直接受けていたサービスを ViewModel の側へ寄せて、差し引きで減らす（`MainWindow` 15、`ProfileEditor` 12 は基準の上限にある）。
6. 進み具合は、件数のラチェット（XAML のイベント属性の数と、code-behind が直接作る窓の数を、基準値と一致させる検査）で守る。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 画面の順 | (A) `ProfileEditor` → `MainWindow` → `RecordBox` → `AssignmentEditor` → `BindingWindow`（行数とイベントの多い順。`BindingWindow` は名前つきの部品が 146 あり、見た目の処理が中心なので最後）／(B) 利用者の指定の順 |
| D2 | コマンドの実装 | (A) 自前の小さな `RelayCommand`（1 つの型。依存を足さない）／(B) `CommunityToolkit.Mvvm` を導入（依存が増える。`ObservableObject` は自前のものがすでにある） |
| D3 | 到達の目安 | (A) 仕分けの (A)(B) をすべて移す（code-behind に残るのは (C) だけ）／(B) 行数の目標（例: 各 500 行以下）を置く。行数は目的にしないほうがよい |
| D4 | Phase26 の扱い | (A) Phase26 を「一部完了で終了」とし、残り（26-5 の続き）を本 Phase が引き継ぐ／(B) Phase26 を続け、本 Phase は土台（35-1）だけにする |
| D5 | `MessageBox` の文言 | (A) そのまま（英語の固定や、既存のリソース）／(B) この機会にリソース化（範囲が広がる。別の作業にするのを勧める） |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 35-0 | 棚卸し: 5 画面のイベント処理（約 107 個）と、部品への直接の代入（約 180 か所）を、(A)(B)(C) に仕分けた表を作る。画面ごとの Step を確定する | — | 未着手 |
| 35-1 | 土台: コマンド（D2）、ダイアログの窓口（確認・通知／ファイルの選択／色の選択）、`MainWindow` が直接作る 4 つの窓の生成の窓口。テスト用の偽物を用意する | 窓口の契約ごとの偽物。コマンドの実行可否の通知 | 未着手 |
| 35-2 | `ProfileEditor`: 保存・キャンセル・適用、特殊アクションの一覧（並べ替え、編集、削除。`SortSpecialActionsList`、`EditActionBtn_Click`）、プリセット、読み込み直し（`Reload` のうち、値の集め方）を ViewModel へ。図のホバーと `InputDS4`（入力による選択）は (C) として残す | 保存の検査、一覧の並べ替え、編集の開始の判断 | 未着手 |
| 35-3 | `MainWindow`: 設定タブのボタン、プロファイルの操作（新規・編集・複製・削除・書き出し・読み込み）、更新の確認、窓の生成（`ChangelogWindow` ほか）を、ViewModel と窓口へ。電源・デバイスの通知（`PowerEventArrive`、`OnSourceInitialized`）は、OS のメッセージの受け口なので View に残し、判断だけを部品へ | プロファイルの操作の判断、更新の確認、電源の復帰の判断 | 未着手 |
| 35-4 | `RecordBox`（記録の画面）: 記録の開始・停止、保存・読み込み、手順の編集を `RecordBoxViewModel` のコマンドへ。キーとマウスの生の入力の受け口は View に残す | 記録の状態の移り変わり、保存の検査 | 未着手 |
| 35-5 | `AssignmentEditor`（割り当て編集画面）: 種別の切り替え、保存の検査を ViewModel へ | 種別ごとの保存の検査 | 未着手 |
| 35-6 | `BindingWindow`（出力ピッカー）: 選択の確定、Extras の値の集め方を ViewModel へ。図の上のボタン（名前つきの部品 146）の見た目は残す | 選択の確定の判断 | 未着手 |
| 35-7 | 検査: XAML のイベント属性の数と、code-behind が直接作る窓・ダイアログの数を、画面ごとの基準値と一致させるラチェットを足す（減らしたら、同じコミットで基準も減らす） | 新規 | 未着手 |
| 35-8 | 文書: `01-Structure.md`（UI の型の表）、規約 3.1（コマンドとダイアログの窓口の使い方を 1 段落）、`Structure-Review-2026-11.md` の B の更新履歴 | `ModelDiagramGuardTests` | 未着手 |

## 確認の方法
- 各 Step: 途中は `--filter "Category!=Slow&Category!=Benchmark"`、Step の終わりに Debug ALL（警告 0・エラー 0）。
- 実機: 画面ごとに、移したボタンと操作を一通り。特に、(1) プロファイルの保存・キャンセル・未保存での閉じ方、(2) 特殊アクションの追加・編集・削除・並べ替え、(3) マクロの記録と保存、(4) 設定タブの各項目、(5) 更新の確認、(6) スリープからの復帰とコントローラーの抜き差し。

## リスク
- 中〜高。結びつけ・コマンドの名前の誤りは、コンパイルで見つからず、画面で「押しても動かない」形で現れる。35-7 のラチェットと、画面を開くテスト（結びつけのエラーが出ないこと）で守る。
- 範囲が広い（5 画面、約 6,100 行）。画面ごとに独立して進められるので、途中で止めても害はない。
- `MainWindow` は、起動・終了・電源・デバイスの通知が集まる。Phase33（終了の手順）と同じファイルに触れるため、同時に進めない。

## 履歴
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。
