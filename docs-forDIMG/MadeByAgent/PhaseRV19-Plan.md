# PhaseRV19 計画書: 画面の「見せる・隠す・使える」の判断を ViewModel へ

作成日: 2026-11-21
状態: **計画。決定事項 D1〜D4 の選択待ち（コードは未着手）**
出典: `Measure-CodeReview.py --only mvvm`（RV18 完了時点）、`BindingWindow.xaml.cs` ほかの読み取り。
動き: **挙動・ログ・保存の形は変えない**（見た目も変えない）。

## 目的
MVVM の指標のうち、**判断が画面（code-behind）にあるもの**を、ViewModel の状態へ移す。規約 3.1 の画面の MVVM の規則: 「判断・状態は ViewModel、見た目だけの処理（ホバー・描画・フォーカス・ドラッグ）は View の code-behind」。

| 指標（RV18 完了時点） | 点 | 内容 |
|---|---|---|
| code-behind が部品へ直接代入 237（`Content` 94・`Visibility` 75・`IsEnabled` 34・`SelectedIndex` 25・`ItemsSource` 9） | 0.54 | |
| 結びつけ率（`{Binding}` ÷ `{Binding}`＋`x:Name`）0.608 | 0.62 | |
| ViewModel の行の割合 0.596 | 0.54 | |

## 読んで分かった実態
直接代入は、**どの画面にも少しずつではなく、いくつかの画面に集中**している:
| 代入 | 場所 | 中身 |
|---|---|---|
| `Visibility` 20 | `BindingWindow.xaml.cs` | 開くときの種類（モード）で、記録ボタン・上のオプション・モードのパネル・マクロ表示を出し分ける（106〜168 行）。割り当て先の種類で、パネルを切り替える（425〜443 行） |
| `Visibility` 16 | `AssignmentEditor.xaml.cs` | 種類（Default・Unbound ほか）で、項目と欄を出し分ける |
| `IsEnabled` 11 | `RecordBox.Keyboard.cs` | 記録中・種類で、ボタンの使える／使えないを切り替える |
| `Content` 34 | `ProfileEditor.Visualizer.cs` | コントローラーの図の上の、ボタンごとの割り当て表示（ホバーと更新の見た目） |
| `Content` 21 | `ControllerReadingsControl.Readings.cs` | 入力の値の読み出し（毎フレームに近い更新） |

- 上の 3 つ（`BindingWindow`・`AssignmentEditor`・`RecordBox.Keyboard`）は、**モード・種類・記録中という状態から、見せる・隠す・使えるが決まる**。状態は ViewModel に置き、`Visibility`（`bool` ＋コンバーター）・`IsEnabled` を `{Binding}` へ結べば、**単体テストで「この状態ならこの欄が見える」を固定できる**。
- 下の 2 つ（`Visualizer`・`Readings`）は、図の描画に近く、更新の頻度が高い。バインディングへ移すと、性能と見た目のずれのリスクが大きい。**見送り**（規約の「見た目だけの処理」に近い）。

## 決定してほしい事項

### D1 対象
- **(A) モード・種類で決まる出し分けの 3 画面: `BindingWindow`・`AssignmentEditor`・`RecordBox.Keyboard`（推奨）**。直接代入の約 47 件が、状態＋バインディングになる。
- (B) (A) ＋ `ProfileEditor.Visualizer`・`ControllerReadingsControl`（描画に近く、性能のリスクがある）。
- (C) やらない。

### D2 進め方
- **(A) 1 画面ずつ、(1) 現在の出し分けの表を作る（モード × 部品 → 見える／隠れる／使える）、(2) その表を単体テストで固定してから、ViewModel の状態とバインディングへ置き換える（推奨）**。表が置き換え前後で一致することが、挙動を変えない根拠になる。
- (B) 先にバインディングへ置き換え、実機の見た目で確かめる。

### D3 実機確認の順序
- **(A) RV3〜RV18 の分（§9b〜§9l、RV15・RV17）の実機確認を、RV19 の前に先に行う（推奨）**。画面の見た目を変える Phase の前に、現状が正しいことを確かめておく。
- (B) RV19 を先に行い、確認は後。**出し分けの表のテストで、見た目の変化は単体でも検出できる**ため、(B) でも、リスクは中程度。
- (C) RV19 の後にまとめて 1 回。

### D4 ラチェットの扱い
- **(A) 画面の部品への直接代入と、イベント属性（`ViewCommandBindingBaseline`）の基準を、減った分だけ同じコミットで更新する（推奨）**。新しい直接代入は足さない。
- (B) 基準の更新は、Phase の最後にまとめて。

## Step（D1＝A、D2＝A の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV19-0 | 3 画面の出し分けの表（`PhaseRV19-Visibility-Map.md`）。承認 | 記録のみ |
| RV19-1 | `BindingWindow`: 表のテスト → ViewModel の状態（モード → 見える欄）→ `{Binding}`。`BindingWindowViewModel` を拡張 | Debug ALL、表のテスト、`ViewCommandBindingRatchetTests` |
| RV19-2 | `AssignmentEditor`: 同様（種類 → 見える欄。`ActionEditorViewModel` の `TypeCatalog` を使う） | 同上 |
| RV19-3 | `RecordBox.Keyboard`: 記録中・種類 → 使えるボタン | 同上 |
| RV19-4 | 再採点（`RV19.json`）、`02-Scorecard.md`、計画書の完了記録、`Device-Check-Checklist.md`（3 画面の出し分けの確認項目） | `ModelDiagramGuardTests` |

ブランチは `PhaseRV19`。コミットは Step ごと、完了で `--no-ff` マージ（push なし）。

## 予想（03-Rubric.md §5）
- 直接代入 237→約 190（点 0.54→約 0.62）、結びつけ率 0.608→約 0.62、ViewModel の行の割合 0.596→約 0.60。
- 等級の変化: MVVM A−（86.3%）が **約 87%（A− のまま）**。A（90%）には届かない見込み（残りは `Visualizer`・`Readings` と、結びつけ率の低い画面）。

## リスク
- 出し分けの置き換えは、**見た目の同一性**が最大のリスク。表のテストで、モードの全組み合わせを固定する。表に載らない暗黙の出し分け（初期値、後から別の処理が変える `Visibility`）は、読み取りで拾い、表に加える。
- `Visibility` を `{Binding}` にすると、XAML の `x:Name` が不要になる部品が出る。code-behind がまだ `x:Name` を参照する場合は残す（無理に消さない）。
- 画面の `DataContext` が複数ある画面（`BindingWindow`）は、バインディングの経路を、既存の規則 V3（`{Binding DataContext.…}`）に合わせる。
- `ViewCommandBindingBaseline` の基準は、減らしたら同じコミットで更新（新しいイベント属性は足さない）。

## 実機確認
`Device-Check-Checklist.md` に追記: 出力ピッカー（`BindingWindow`）の各モード（割り当て・スペシャルアクション・マクロ記録）の見える欄、割り当て編集画面（`AssignmentEditor`）の種類ごとの欄、マクロ記録（`RecordBox`）の記録中のボタンの状態。**見た目が変わっていないこと**を見る。

## 次の Phase（見込み）
RV20: 実機確認（§9b〜§9l と RV15・RV17・RV19）の消化と、見つかった不具合の修正。その後、画面の型（`MainWindow`・`ProfileEditor`・`App`）の整理。
