# PhaseRV4 計画書: ProfileEditor・BindingWindow・RecordBox・WelcomeDialog などの MVVM 化と表駆動化

作成日: 2026-11-19
状態: **未承認（コードは未着手）**
出典: `docs-forDIMG/Code-Review-2026-11-19/00-Report.md` の RV-13〜RV-16（§3.3、§3.5、§5。RV-15 の取得・導入の部品を含む）
前提: PhaseRV3 の完了（`AsyncRelayCommand`、V3 の検査、コマンドの形）。PhaseX3 の XAML の作業の区切り。PhaseRV2 の規則。
動き: **画面の見た目と操作は変えない**。

## 目的
PhaseRV3 で確立した形（コマンド、状態のバインディング、View に残すのは見た目だけ）を、残りの画面へ広げる。あわせて、画面に散る「表」（`BindingWindow` の 149 の手結び、`ProfileEditor.HoverTables.cs` の表、`TouchpadSettingsPanel` の 32 の `Click`）を、データとして 1 か所に持ち、同じ型の部品へ畳む。`WelcomeDialog` の同型の流れ 3 本は、画面と部品を一緒に整理する。

## 背景（実測。数字は `01-Metrics.md`）
- 画面ごとの `Click`／その他のイベント属性: `TouchpadSettingsPanel.xaml` 32／0、`ProfileEditor.xaml` 19／5、`RecordBox.xaml` 12／11、`GyroSettingsPanel.xaml` 11／0、`WelcomeDialog.xaml` 6／0、`BindingWindow.xaml` 5／1、`LightbarSettingsPanel.xaml` 5／0、`WindowTargetEditor.xaml` 2／3。ほかに、4 件以下のファイルが複数。
- `UI/BindingWindow.xaml.cs`（938 行）に、`x.Click += OutputButtonBtn_Click;` が 149 件。構造の重複 69%（467／675 行）。
- `UI/ProfileEditor.xaml.cs`（1,634 行）と、`ProfileEditor.HoverTables.cs`（266 行のうち 79% が重複。`hoverIndexes[crossConBtn] = 0;` の並び）。
- `UI/RecordBox.xaml.cs`（873 行）。`RecordBoxViewModel` はある。
- `UI/WelcomeDialog.xaml.cs`（242 行のうち 58% が重複）: ViGEm・HidHide・FakerInput の「取得して、導入して、`NonFormTimer` で待つ」流れが、3 本、同じ形で並ぶ（`monitorTimer = new NonFormTimer();` と `((NonFormTimer)sender).Stop()／Start()` の組が 3 組。`async void` 3）。
- `UI/ControllerReadingsControl.xaml.cs` の構造の重複 34%。
- code-behind の部品への直接代入は計 290 件、手結び `+=` は計 227 件（うち `BindingWindow` が 149、`MainWindow` が 30、`ProfileEditor` が 25）。
- Phase34 の決定: ホバー・描画・フォーカス・ドラッグは View に置く。`ProfileEditor` の編集の本体（`ProfileEditorBackend`、`IProfileEditBackend`）は、すでに ViewModel 側にある。
- `Phase34-V2-AssignmentEditor-Plan.md`・`Phase34-V2-ProfileEditor-Plan.md` が、この画面の V2 の作業の記録。

## 方針・制約
- 画面の文言は `{lex:Loc キー}`。XAML に表示文字を直接書かない（PhaseX3 の規則）。
- ホバー（見た目）は View に残す。ただし、表（どのボタンがどの番号か）は、データ 1 か所へ。
- 保存の形・プロファイルの内容は変えない（画面の中身のみ）。
- 取得・導入の部品は、画面と同じ Step で作る（画面だけ先に変えて、部品が後、とはしない）。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| RV4-0 | 調査。RV3-0 と同じ 3 分類（(a) コマンド、(b) バインディング、(c) View に残す）で、対象画面（上の表）の全ハンドラーを `PhaseRV4-Handler-Inventory.md` に表にする。`BindingWindow` の 149 のボタンは、(i) XAML に個別に置かれた固定のボタンか、(ii) 表から作れる並びかを調べ、`ItemsControl`＋`DataTemplate`＋コマンドパラメーターへ畳めるかを判断する（D1）。ユーザーの承認を得る | — | 未着手 |
| RV4-1 | `BindingWindow`: 149 の `Click +=` の手結びをなくす。表（D1 の結果）から部品を作るか、XAML の `Command`＋`CommandParameter` にする。`BindingWindowViewModel`（既存）にコマンドを置く | `BindingWindowViewModelTargetDeviceTests`、`BindingWindowWriteTests`、`BindingWindowMacroScanCodeTests`、`BindingWindowModeGuardTests`、新規: ボタンの表の網羅（全ボタンが 1 つの出力に結びつく） | 未着手 |
| RV4-2 | `TouchpadSettingsPanel`（32）、`GyroSettingsPanel`（11）、`LightbarSettingsPanel`（5）: 領域・軸ごとの選択ボタンを、表の `ItemsControl`＋コマンドへ | `TouchpadSettingsViewModel`・`GyroSettingsViewModel`・`LightbarSettingsViewModel` のテスト（既存。足す） | 未着手 |
| RV4-3 | `ProfileEditor`（19／5）と `ProfileEditor.HoverTables.cs`: ホバーは View に残し、表（`hoverIndexes[…] = n;` の並び）を、データ 1 か所（例: `HoverTargetTable`）にして、設定の並びから作る。`+=` の手結び 25 を、分類に従って減らす | `ProfileEditor` まわりの既存のテスト（`ProfileEditViewModelFactory`、`ProfileEditorBackend`）。表の網羅のテスト | 未着手 |
| RV4-4 | `WelcomeDialog`（6）: ViGEm・HidHide・FakerInput の取得・導入の流れを、部品 `ExternalComponentInstaller`（ダウンロード、導入、状態の監視。タイマーは部品の中に 1 か所）と、`WelcomeDialogViewModel`（各部品の状態: 未導入／取得中／導入中／完了、コマンド）へ。`async void` 3 は、RV2-6 で `async Task` になっている前提。部品の外への入口（ダウンロード、インストーラーの実行）だけをインターフェースにして、テストでは偽物に差し替える（規約 §3.1 の基準。部品そのものはインターフェースにしない） | 新規: `ExternalComponentInstallerTests`（偽のダウンロード・実行で、状態の遷移、失敗、キャンセル）、`WelcomeDialogViewModelTests`。ほかに `FirstLauchUtilViewModel` の既存のテスト | 未着手 |
| RV4-5 | `RecordBox`（12／11、873 行）: 記録の開始・停止、キーの記録の操作をコマンドへ。`RecordBoxViewModel` の既存の責務の整理 | `RecordBoxViewModel` のテスト（既存。足す） | 未着手 |
| RV4-6 | 残りの画面（`WindowTargetEditor`、`ControllerReadingsControl`、4 件以下の画面）の `Click`・イベント属性とタイマーを、分類（RV4-0）に従って整理。`ControllerReadingsControl` のタイマーは、`GuardedTimerTick` を使う形に揃える | 既存のテスト | 未着手 |
| RV4-7 | 検査: `ViewCommandBindingBaseline` を、全画面の目標の件数へ更新（RV4 の終了時に、コマンド率 70% 以上、`Click` 100 以下、`+=` 100 以下）。`CodeHealthRatchetTests` の `Command` バインディングの下限を更新 | 新テストの更新 | 未着手 |
| RV4-8 | 実機確認の項目を `Device-Check-Checklist.md` に足す（出力ピッカーの全ボタン、プロファイルの編集のホバー、ジャイロ・タッチパッド・ライトバーの設定、はじめの起動画面の導入、マクロの記録）。画面の名前は、利用者の呼び方に合わせる（割り当て編集画面＝`SpecialActionEditor`、出力ピッカー＝`BindingWindow`） | 実機（最後にまとめて 1 件ずつ） | 未着手 |

## 決めること
| # | 決めること | 選択肢（先頭が推奨） | 利点／欠点 |
|---|---|---|---|
| D1 | `BindingWindow` の 149 のボタン | (A) 表（出力の一覧）から `ItemsControl`＋`DataTemplate`＋`CommandParameter` で作る／(B) XAML の固定のボタンに `Command`＋`CommandParameter` を書く（手結びだけ消す）／(C) 添付ビヘイビアで、名前から自動結びつけ | (A) 利点: 149 の要素が表 1 つになり、追加が容易（機能追加の容易化）。欠点: 見た目の再現（配置・グループ）に手間。(B) 利点: 見た目が変わらない。欠点: XAML が長いまま。(C) 利点: 変更が小さい。欠点: 暗黙の結びつき。**推奨は (A)**（配置が表から作れないと分かれば (B)） |
| D2 | `WelcomeDialog` の部品化の範囲 | (A) 3 本を 1 つの `ExternalComponentInstaller` に畳み、画面は ViewModel の状態を表示／(B) 部品だけ作り、画面は code-behind のまま／(C) 触らない | (A) 利点: 重複が消え、テストできる。欠点: 3 つの部品の違い（URL・導入の引数・確認の条件）を表で持つ必要。(B) 利点: 画面の変更が小さい。欠点: タイマー・状態が code-behind に残る。(C) 利点: 変更なし。欠点: 重複・`async void` が残る。**推奨は (A)** |
| D3 | 目標の件数（報告書 §5） | (A) コマンド率 70% 以上、`Click` 100 以下、`+=` 100 以下、直接代入 150 以下（残りは見た目だけ）／(B) 分類 (a)(b) をすべて 0 にする／(C) 数字の目標を置かない | (A) 利点: 現実的で検査できる。欠点: 数字が目的化するおそれ。(B) 利点: 徹底。欠点: ホバー等の扱いの定義が要る。(C) 利点: 柔軟。欠点: 形骸化。**推奨は (A)**（RV4-0 の分類の結果で調整） |
| D4 | `ProfileEditor` の分割（1,634 行） | (A) ハンドラーの移動で自然に減る分だけ。ファイルの分割はしない／(B) 画面ごとの部分（`partial`）へ分割／(C) UserControl へ分割 | (A) 利点: 変更が小さい。欠点: 行数は大きいまま。(B) 利点: 読みやすい。欠点: 責務は分かれない（`HoverTables` が前例）。(C) 利点: 責務が分かれる。欠点: 大きな変更。**推奨は (A)** |

## 確認の方法
- 各 Step のあとに、ビルドと `--filter "Category!=Slow&Category!=Benchmark"`。Step の終わりに Debug ALL（全件。警告 0・エラー 0）。
- 画面ごとに、ビルドしたアプリを起動して確かめる。バインディングの誤りは、`System.Windows.Data Error` を見る。
- 実機確認は RV4-8（最後にまとめて）。

## リスク
- 中〜高。`BindingWindow` の 149 のボタンの結びつけは、1 つ違っても別の出力が割り当たる。表の網羅のテスト（全ボタンが 1 つの出力に結びつく。重複なし）で守る。
- `WelcomeDialog` の導入は、外部のインストーラーを実行する（ViGEm・HidHide・FakerInput）。**実機でも、テストでも、実際の導入を行わない**（規約 §4「OS の状態に触れない」。偽物を渡す）。部品の実物の確認は、手順を決めたうえで利用者が行う。
- ホバーの表をデータにすると、図（コントローラーの画像）の座標との対応がずれると、見た目が壊れる。

## 完了の定義
- コマンド率 70% 以上、`Click` 100 以下、`+=` 100 以下、直接代入 150 以下（`ViewCommandBindingBaseline`、`CodeHealthRatchetTests`。D3 で調整した値）。
- `BindingWindow` の手結び（149）が 0。`ProfileEditor.HoverTables.cs` の構造の重複が大きく減っている（`Measure-CodeReview.py --only clone`）。
- `WelcomeDialog` の取得・導入の流れが 1 つの部品に畳まれ、`async void` が 0。
- `ViewModelWpfReferenceBaseline`・`ViewDirectServiceReferenceBaseline` が 0 のまま。
- Debug ALL が警告 0・エラー 0。実機確認の項目が台帳・チェックリストに足されている。

## 関連計画
- `PhaseRV3-Plan.md`（基盤）、`PhaseRV2-Plan.md`（`async void`）、`PhaseRV6-Plan.md`（共有部品化の残り）。
- 過去: `Phase34-Plan.md`、`Phase34-V2-AssignmentEditor-Plan.md`、`Phase34-V2-ProfileEditor-Plan.md`、`Phase10-Assignment-UI-Unification-Plan.md`、`PhaseX3-Localization-Plan.md`。
- 規約: §3.1（画面の MVVM の規則）、§4（OS の状態に触れない）。

## 履歴
- 2026-11-19: 計画書を作成。コードは未着手。
