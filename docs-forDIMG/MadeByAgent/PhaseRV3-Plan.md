# PhaseRV3 計画書: MainWindow の MVVM 化（コマンド化と、委譲だけの窓口の解消）

作成日: 2026-11-19
状態: **未承認（コードは未着手）**。D1〜D4 は 2026-11-19 にユーザーが選択済み（「決定事項」を参照）
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-09〜RV-12（§3.3、§5）
前提: PhaseRV1 の完了。PhaseX3（日本語化）の XAML の作業の区切り（RV3 は XAML を大きく触るため、競合を避ける）。PhaseRV2 の規則（例外の扱い）を、新しく書くコードに適用する。
動き: **画面の見た目と操作は変えない**。ボタンの有効・無効、フォーカス、ホットキー、トレイの動きを、実機で確かめる。

## 目的
Phase34 は、画面の「型の規則」（V1・V2）を満たした。この Phase は、その続きとして、**操作の流れ**（`Click` のハンドラー、`Task.Run`＋`IsEnabled = false`、部品への直接代入）を ViewModel のコマンドと状態へ移す。まず基盤（非同期のコマンド）を作り、`MainWindow` とそのタブの中身（About、AutoProfiles、AssignmentEditor）で形を確立する。残りの画面は PhaseRV4。

## 背景（実測。数字は `01-Metrics.md`）
- XAML の `Command` バインディング 0、`RelayCommand` を使うファイル 0。`Click` 属性 193、その他のイベント属性 46。
- 画面ごとの `Click`／その他のイベント属性: `MainWindow.xaml` 31／8、`About.xaml` 13／0、`AutoProfiles.xaml` 11／0、`AssignmentEditor.xaml` 10／0（この 4 画面が RV3 の対象）。残りは RV4。
- `UI/MainWindow.xaml.cs` は 1,763 行。コンストラクターの引数は 12（上限ちょうど）。`+=` の手結びが 30。`StartStopBtn_Click`→`ChangeService`、`HideDS4ContCk_Click` などが、`Task.Run` で処理し、`IsEnabled = false` で多重実行を避ける同型の形を持つ。
- `UI/ViewModels/MainWindowsViewModel.cs` は 77 行と小さく、状態の多くが code-behind にある。
- `UI/Services/MainWindowServiceControl.cs`・`MainWindowSettings.cs`・`MainWindowEnvironment.cs` は、本体が相手への委譲だけの窓口（V2 の基準を満たすために挟まれた形）。
- 既存の部品: `UI/ViewModels/Util/RelayCommand.cs`（40 行。同期のみ。実行できるかの再評価は呼び出し側が `RaiseCanExecuteChanged`。`RelayCommandTests` あり）、`UI/ViewModels/ObservableObject.cs`（31 行）、`UiDispatcher`（`IUiDispatcher`）、`IDialogService`、`GuardedTimerTick`。
- 規則 V1・V2 の基準値（`ViewModelWpfReferenceBaseline`・`ViewDirectServiceReferenceBaseline`）は 0。Phase34 の決定: ホバー・描画・フォーカス・ドラッグは View の code-behind に置いてよい。

## 方針・制約
- 画面の文言は `{lex:Loc キー}`（PhaseX3 の規則）。XAML に表示文字を直接書かない。
- ViewModel は WPF の型を参照しない（V1）。`ICommand` は例外。`Dispatcher` は `IUiDispatcher`、確認・通知は `IDialogService`。
- 分類で「View に残す」と決めた処理（ホバー、描画、ウィンドウの位置、ドラッグ）は、無理に移さない。
- 1 画面ずつ、コマンドへの置き換えとテストを一緒に行う（一度に全画面を替えない）。
- `MainWindow` のコンストラクターの引数は、減らす方向のみ（8 以下をめざす）。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV3-0 | 調査。(1) 対象 4 画面の全ハンドラー（`Click`・その他のイベント属性・`+=`）を、**(a) 業務の呼び出し（コマンドへ）／(b) 状態の更新（バインディングへ）／(c) 見た目だけ（View に残す）** に分け、`PhaseRV3-Handler-Inventory.md` に表にする。これで §5 の④（code-behind のうち View に置いてよい割合）が出る。(2) コマンドの基盤: D1＝(B) CommunityToolkit.Mvvm に決定済み。**最初に採用の関門を通す**。パッケージ（net8 対応の安定版。版は固定）を参照に足し、(i) `[RelayCommand]` などのソース生成が `_wpftmp` の 1 回目のビルドで動き、Debug ALL が警告 0・エラー 0 で通ること、(ii) `AsyncRelayCommand` が、実行中は `CanExecute` が false になること、(iii) 例外の既定の扱い（`FlowExceptionsToTaskScheduler` を付けないと、`async void` と同じく呼び出し側の同期コンテキストへ再スローされ、アプリが落ちる、と理解している。**要確認**）、(iv) キャンセルへの対応、を確かめる。関門を通らないときは、(A) 独自の拡張へ戻す提案をする。比較の記録（規約 §4。独自の拡張、Prism、ReactiveUI との違い）も同じ文書へ。ユーザーの承認を得る | — | 未着手 |
| RV3-1 | 基盤: CommunityToolkit.Mvvm の導入（D1＝(B)）。(1) パッケージ参照を足す（版は固定）。(2) 使い方の規則を決めて文書化する: コマンドは `AsyncRelayCommand`（実行中は `CanExecute` が false。多重実行の防止を、`IsEnabled = false` の代わりに担う）。例外は、コマンドの本体または共通の包みで受けて `AppLogger`・`IDialogService` へ流し、アプリを落とさない（`async void` を作らない。RV2 の規則）。(3) 独自の `RelayCommand`（使うファイル 0）と `RelayCommandTests` は、置き換えとして削除し、`RemovedApiRuleTests` の表へ足す。(4) 独自の `ObservableObject`（12 型が継承）は、この Step では触らない（RV7 でツールキットの `ObservableObject` へ移す）。新しく作る・大きく変える ViewModel だけ、ツールキットの `ObservableObject` を使う（RV3〜RV7 の間、2 つが共存する） | 新規: コマンドの規則を確かめる単体テスト（実行中の再入の拒否、例外がアプリを落とさず記録されること、`CanExecuteChanged`）。`RelayCommandTests` の観点は新しいテストへ移す | 未着手 |
| RV3-2 | 開始・停止（`StartStopBtn_Click`、`ChangeService`）と、`Task.Run`＋`IsEnabled = false` の定型（`HideDS4ContCk_Click` ほか）を、`MainWindowsViewModel` のコマンド（非同期）と、「忙しい」状態へ。ボタンの有効・無効は、コマンドの `CanExecute` で表す | 新規: `MainWindowsViewModel` のコマンドの単体テスト（偽のサービスで、開始・停止の分岐、忙しい間の再実行の拒否） | 未着手 |
| RV3-3 | `MainWindow.xaml` の残りの `Click`（31 のうち RV3-2 の分を除く）と、その他のイベント属性（8）を、分類（RV3-0）に従ってコマンド・バインディングへ。トレイのメニューと同じ操作（開始・停止・プロファイルの切替など）は、コマンドを共有する（D4） | `TrayIconViewModel` のテスト、新規のコマンドのテスト | 未着手 |
| RV3-4 | 委譲だけの窓口の解消（D3）: `MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment` を、ViewModel へ直接注入する契約（既存の `Processing/Abstractions`・`UI/Abstractions` のインターフェース）に置き換え、窓口の型を削除する。`MainWindow` のコンストラクターの引数を減らす。`RemovedApiRuleTests` の表へ 3 型を足す | `ViewDirectServiceReferenceRatchetTests`（0 のまま）、`ConstructorArityRuleTests`、`RemovedApiRuleTests`、`LayerDependencyRuleTests` | 未着手 |
| RV3-5 | `About`（13）、`AutoProfiles`（11）、`AssignmentEditor`（10）を、同じ形でコマンドへ。`AutoProfilesViewModel` の `async void` 4 は RV2-6 で `async Task` になっている前提 | 各 ViewModel のテスト（`AutoProfilesViewModelTests` ほか） | 未着手 |
| RV3-6 | `MainWindow.xaml.cs` の部品への直接代入（`IsEnabled`・`Visibility`・`Content`・`ItemsSource`・`SelectedIndex`）を、分類（RV3-0）の (b) に従ってバインディングへ。残る手結び（`+=`）は、(c) のみにする | `MainWindowsViewModel` のテスト | 未着手 |
| RV3-7 | 検査: **V3（操作は `ICommand`）**。`ViewCommandBindingRatchetTests`（`ViewCommandBindingBaseline`）を新設し、XAML の `Click`・その他のイベント属性の件数を、画面ごとに凍結して実数と一致を検査する（RV3 の 4 画面は目標の件数。残りは現状の件数）。D2＝(B) のため、4 画面に残してよい `Click`・イベント属性は、分類 (c)（見た目だけ）の許可リスト（画面・ハンドラー名・理由）に載ったものだけとし、リスト外が 1 件でもあれば失敗する。許可リストは RV3-0 の分類表と一緒に承認を得る。規約 §3.1 に V3 の説明を足す（G1＝(A) に決定済み。文面をユーザーに見せて承認を得てから足す）。`CodeHealthRatchetTests` に `Command` バインディングの下限を足す | 新テスト自身 | 未着手 |
| RV3-8 | 実機確認の項目を `Device-Check-Checklist.md` に足す（開始・停止の連打、トレイ、ホットキー、更新の確認、最小化・復帰、自動プロファイルの一覧の操作、スペシャルアクションの編集）。モデル図 `01-Structure.md` は、型の一覧が変わる場合のみ `ModelDiagramGuardTests` を見ながら更新 | 実機（最後にまとめて 1 件ずつ） | 未着手 |
| RV3-9 | 評価の再算定（採点基準 `Code-Review-2026-11-19/03-Rubric.md`）。(1) `scripts/audit/Rubric-Inputs.json` の確認項目と手入力の値を、根拠（実行したテスト・実測）を `evidence` に書いて更新する。(2) `python -I scripts/audit/Score-CodeReview.py --json docs-forDIMG/Code-Review-2026-11-19/scorecard/RV3.json --label "RV3 完了" --diff docs-forDIMG/Code-Review-2026-11-19/scorecard/RV2.json` で、採点して保存する。(3) `02-Scorecard.md` に列と理由（等級が変わった観点、点数が動いた指標、予想とのずれ）を足し、`01-Metrics.md` §13 の「現在」を `Measure-CodeReview.py` の実測で取り直す。(4) ユーザーへ、点数・等級の変化と、次の Phase で動かす指標を報告する。机上の予想（`03-Rubric.md` §5）で動く観点: MVVM 原則遵守の割合 C+→B−。ソースは変更しない（記録のみ） | `Score-CodeReview.py`（記録のみ） | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | コマンドの基盤 | (A) 独自の `RelayCommand` を拡張し、`AsyncRelayCommand` を足す／(B) CommunityToolkit.Mvvm を採用（`AsyncRelayCommand`、`[RelayCommand]`、`ObservableObject`）／(C) Prism・ReactiveUI | (A) 利点: 追加の依存なし、既存の `RelayCommandTests`・`ObservableObject` と整合、WPF なしでテストできる。欠点: 自前の保守。(B) 利点: 標準的、定型が減り、RV7 の変更通知の統一も一度に解決。欠点: パッケージの追加、独自の `ObservableObject`（12 型）の置き換え、ソース生成と WPF の 1 回目のビルドの相性が要確認。(C) 利点: 機能が多い。欠点: 大きすぎる。**推奨は (A)**。RV3-0 の比較で (B) が明らかに有利なら再提案する |
| D2 | コマンド率の目標（報告書 §5 の②） | (A) RV3 の 4 画面で 70% 以上、全体は RV4 の終了時に 70% 以上／(B) 見た目だけを除くすべて／(C) 50% | (A) 利点: 現実的。欠点: 残りの扱いは RV4 次第。(B) 利点: 徹底。欠点: ホバーなどの View の処理を数えない定義が要る。(C) 利点: 負担が小さい。欠点: 形骸化が残る。**推奨は (A)**（RV3-0 の分類で (c) の割合が出たら、目標を調整） |
| D3 | 委譲だけの窓口 | (A) 3 つとも削除し、ViewModel へ契約を直接注入／(B) 1 つに統合して残す／(C) 残す（V2 のためのアダプター） | (A) 利点: 層が減り、`MainWindow` の引数が減る。欠点: ViewModel の引数が増える（束ねすぎない）。(B) 利点: 引数が少ない。欠点: 「何でも箱」になる（規約 §3.1 が避ける形）。(C) 利点: 変更なし。欠点: 冗長な層が残る。**推奨は (A)**（ViewModel の引数が 12 を超えるときだけ、責務で分割） |
| D4 | トレイのメニューとコマンドの共有 | (A) 共有する（同じ `ICommand` を両方からバインド）／(B) 別々に持つ | (A) 利点: 重複がない。欠点: 結びつきが 1 つ増える。(B) 利点: 独立。欠点: 同じ処理が 2 か所。**推奨は (A)** |

## 決定事項（2026-11-19、ユーザーが選択）
| # | 決定 | 計画への反映 |
|---|---|---|
| D1 | (B) CommunityToolkit.Mvvm を採用 | パッケージを追加する。RV3-0 の最初に採用の関門（ソース生成と `_wpftmp` の 1 回目のビルド、`AsyncRelayCommand` の例外の既定の扱い）を通し、通らなければ (A) へ戻す提案をする。独自の `RelayCommand` は削除。独自の `ObservableObject` の置き換えは RV7（RV7 の D1 は (C) を前提に再確認）。**例外の既定の扱いが `async void` 並みなら、共通の包みを RV3-1 で決める** |
| D2 | (B) 見た目だけを除くすべて | 目標は、分類 (a)（業務の呼び出し）・(b)（状態の更新）の 100%。残る `Click`・イベント属性は、(c)（見た目だけ）の許可リスト（理由つき）に載ったものだけ。(c) の定義が甘いと抜け道になるため、RV3-0 の分類表で 1 件ずつ承認を得る。RV4 の D3（目標の件数）は、同じ考え方で確認する |
| D3 | (A) 3 つとも削除し、ViewModel へ契約を直接注入 | `MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment`（計 203 行）を削除。`MainWindow` の引数は 12 → 9 前後。`MainWindowsViewModel` の依存が増えるので、12 を超える前、または責務が混ざる前に、責務（サービス制御・設定・環境）で ViewModel を分ける |
| D4 | (A) 共有する | トレイのメニューと `MainWindow` で、同じ `ICommand` をバインドする |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 画面の起動は、ビルドしたアプリを実際に起動して確かめる（XAML の参照・バインディングの誤りは、実行時にしか出ない）。バインディングの誤りは、出力のウィンドウの `System.Windows.Data Error` を見る。
- 実機確認は RV3-8（最後にまとめて）。

## リスク
- 中〜高。画面の動き（ボタンの有効・無効、連打、フォーカス）が変わり得る。`IsEnabled = false` の置き換え（`CanExecute`）は、UI スレッドと別スレッドの間で状態がずれると、ボタンが固まる。
- 開始・停止は、ドライバー・仮想パッドの寿命に関わる（Phase5 の 6 大ガードレール、特に「スレッド直列化」「ドライバ破棄順序」）。コマンドの中で、既存の手順（`ChangeService` の順序）を変えない。
- CommunityToolkit.Mvvm の追加（D1）: ソース生成と WPF の 1 回目のビルドの相性、コマンドの例外の既定の扱い（落ちるおそれ）、独自の `ObservableObject` との共存（RV7 まで）。RV3-0 の関門で確かめる。
- 窓口の削除は、`MainWindow` を作る側（`ShellViewModelFactory`・`ServiceRegistration.Ui`）の登録を変える。

## 完了の定義
- 対象 4 画面で、分類 (a)(b) の操作がすべてコマンド・バインディングへ移り、残る `Click`・イベント属性が (c) の許可リスト（理由つき）だけ（`ViewCommandBindingBaseline`。D2）。
- CommunityToolkit.Mvvm が導入され、独自の `RelayCommand` が削除されている（`RemovedApiRuleTests`）。
- `MainWindowServiceControl`・`MainWindowSettings`・`MainWindowEnvironment` が削除されている（`RemovedApiRuleTests`）。
- `MainWindow` のコンストラクターの引数が減っている（目標 8 以下）。
- `ViewModelWpfReferenceBaseline`・`ViewDirectServiceReferenceBaseline` が 0 のまま。
- V3 の検査と、規約への追記（承認後）がある。
- Debug ALL が警告 0・エラー 0。実機確認の項目が台帳・チェックリストに足されている。
- 評価の再算定（最終 Step）が済み、`02-Scorecard.md` に記録され、ユーザーへ報告されている（予想とのずれの理由つき）。

## 関連計画
- `PhaseRV1-Plan.md`（検査の枠）、`PhaseRV2-Plan.md`（`async void` の整理）、`PhaseRV4-Plan.md`（残りの画面）、`PhaseRV7-Plan.md`（変更通知の統一。本 Phase の D1＝(B) により、RV7 の D1 は、ツールキットの `ObservableObject` への移行（(C)）を前提に再確認する）。RV4 の D3（目標の件数）は、本 Phase の D2＝(B) と同じ考え方に決定済み（2026-11-19）。
- 過去: `Phase34-Plan.md`・`Phase34-V2-MainWindow-Plan.md`（型の規則）、`Phase35-Plan.md`、`Phase29-Plan.md`（`GuardedTimerTick`）。`PhaseX3-Localization-Plan.md`（文言の規則）。
- 規約: §3.1（画面の MVVM の規則 V1・V2）、§4（マイクロステップ）。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
- 2026-11-19: D1＝(B) CommunityToolkit.Mvvm、D2＝(B) 見た目だけを除くすべて、D3＝(A) 3 つとも削除、D4＝(A) 共有する、をユーザーが選択。D3 は、全体の構造（画面→窓口→契約→第 2 層 と 画面→ViewModel→契約→第 2 層）との関係を説明して選択された。
- 2026-11-19: 最終 Step「評価の再算定」を追加（採点基準 `Code-Review-2026-11-19/03-Rubric.md`、`scripts/audit/Score-CodeReview.py`）。基準は承認待ち。
