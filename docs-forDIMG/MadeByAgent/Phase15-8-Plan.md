# Phase15-8 計画書: Phase15 の持ち越しの片付け

作成日: 2026-11-10
状態: **作業は完了（2026-11-10。C1＝A、C2＝撤去、C3＝この順）。実機確認済み（2026-11-10。振動反転［実機なし］は見送り）**
ブランチ: `master` から `Phase15-8` を作る
前提: `Phase15-Completion-Report.md` §7 の 1・2。3（L2）は Phase16 で行う。

## 1. 目的

Phase15 で意図的に残した 2 件を片付ける。利用者に見える機能は変えない。

| 項目 | 内容 |
|---|---|
| S1 | 特殊アクション一覧の見出し・並べ替えの整理（`ProfileEditor`） |
| S2 | 編集画面の外のスロット配列の直接参照 5 か所を、領域の窓口経由にする |

## 2. 現状

### S1
- 見出し（有効／名前／トリガー／アクション）のテンプレートは、XAML で列に割り当て済みである。
- それにもかかわらず、`ProfileEditor` が、タブを開くたびに `EnsureSpecialActionsHeadersAssigned` で同じテンプレートを列へ割り当て直し、`Loaded` で中の `TextBlock` を探して配列（`specialActionsHeaderTextBlocks`）に保持している。
- 並べ替えのたびに `UpdateSpecialActionsHeaderTexts` が、保持した `TextBlock` へ矢印つきの文字を直接書く。文字を作るのは `ProfileEditor` の公開プロパティ `*SortButtonContent`（`GetSortButtonContent`）。
- 並べ替えの状態（列・方向）は、すでに `SpecialActionsListViewModel` が唯一の実体として持つ。
- `ProfileEditor.xaml.cs` に、この見出しの処理が約 170 行（`Ensure…`、`Update…`、`SetAndLogHeaderText`、`GetSortButtonContent`、カウンターとフラグ 4 個）ある。ログ（Debug）が多い。

### S2
| ファイル | 直接参照 | 置き換え先 |
|---|---|---|
| `CompositeDeviceModel` | `LightbarSettingsInfo[devIndex]` 9 か所 | `ForSlot(devIndex).Lightbar.Info` |
| `RecordBoxViewModel` | `TouchOutMode[…]` 3 か所、`OutContType[…]` 1 か所 | `Touchpad.OutputMode`、`Output.OutContType` |
| `PressKeyViewModel` | `OutContType[…]` 3 か所 | `Output.OutContType` |
| `TouchButtonUserControlViewModel` | `TouchpadButtonMode[…]` 2 か所 | `Touchpad` の窓口に項目を足す |
| `BindingWindow` | `InverseRumbleMotors[…]` 1 か所 | `Output.InverseRumbleMotors` |

窓口に足りないのは `TouchpadButtonMode` の 1 項目だけである。

## 3. 方針

### S1（案）
見出しの文字を、`SpecialActionsListViewModel` の 4 つの文字列プロパティ（`ActiveHeaderText`、`NameHeaderText`、`TriggerHeaderText`、`ActionHeaderText`。並べ替えのたびに変更通知）にし、XAML の `TextBlock` からバインディングで読む。これで、次を撤去できる。
- `EnsureSpecialActionsHeadersAssigned`、`UpdateSpecialActionsHeaderTexts`、`SetAndLogHeaderText`、`specialActionsHeaderTextBlocks` ほか 4 個のフィールド、`*SortButtonContent` と `GetSortButtonContent`
- `SidebarTabControl_SelectionChanged` の見出し初期化の分岐

注意: 見出しの `ContentControl` には `Content` がなく、テンプレートの `DataContext` は null になる。バインディングは `RelativeSource AncestorType=ListView` 経由で書く（動かない場合は ViewModel を `Tag` で渡す）。XAML の読み込み失敗は、ビルドとテストでは見つからないため、`ProfileEditorMarkupLoadTests` で読み込みを確かめ、実機でも確認する。

並べ替えの結果（行の順序）と、矢印の表示は変えない。

### S2
窓口に `TouchpadButtonMode` を足し、5 か所を置き換える。`SettingsSlotWindowUsageTests` の検査の対象に、この 5 つのファイルを加える（以後、直接参照を戻せなくなる）。

## 4. 決定してほしい点

| ID | 内容 | 推奨 |
|---|---|---|
| C1 | S1 の方式。A: バインディングに置き換える（上記）。B: 現状の仕組みのまま、別クラスに切り出すだけにする | A（撤去が大きく、タイミング依存がなくなる。B は複雑さを移すだけ） |
| C2 | S1 で、見出しの Debug ログ（`[SpecialActions] …`、`[EnsureSpecialActionsHeadersAssigned] …`、約 25 行）を撤去してよいか。利用者向けではなく、見出しの割り当てのタイミングを追うための開発用ログ。規約 2.3 のログ維持の例外に当たるため承認が要る | 撤去する（並べ替えの要点の 1〜2 行だけ残す） |
| C3 | 進め方。S2 を先に行い、コミット。続けて S1。実機確認は S1 のあとに 1 回 | この順 |

## 5. Step

| Step | 内容 | 確認 |
|---|---|---|
| 15-8-1 | S2: `TouchpadButtonMode` を窓口へ。5 か所の置き換え。検査の対象を広げる | Debug ALL |
| 15-8-2 | S1: ViewModel に見出しの文字 4 つとテスト。XAML をバインディングへ。`ProfileEditor` の撤去 | Debug ALL。`ProfileEditorMarkupLoadTests` |
| 15-8-3 | 文書の更新（完了報告、`Phase15-Completion-Report.md` §7、ロードマップ）。Release ビルド | 実機確認（特殊アクション一覧: 見出しの文字と矢印、各列のクリックで並べ替え、タブの行き来、言語の切り替え、プロファイルの読み込み直し） |

## 6. 実機確認の項目（Step 3 のあとに 1 回）

1. プロファイル編集画面の特殊アクションのタブで、見出しの文字（「名前」「トリガー」「アクション」。「有効」は空）が出る。
2. 各見出しをクリックして並べ替わり、矢印が出る。もう一度で逆順になる。
3. 他のタブへ移って戻っても、並べ替えと矢印が保たれる。
4. 言語を切り替えると、見出しの文字が追従する。
5. ライトバーの色（メイン画面の一覧のカスタム色）、出力の種類、マクロの記録、タッチパッドのボタンの動作の設定が従来どおり動く（S2 の確認）。

## 7. 結果

- Step 15-8-1: スロット配列の直接参照 5 か所を窓口経由にした。`TouchpadSlotSettings.ButtonMode` を追加。`CompositeDeviceModel` は窓口を一度作って使い回す（`ForSlot` は呼ぶたびに作るため）。`SettingsSlotWindowUsageTests` の対象は 2 → 7 ファイル。
- Step 15-8-2: `SpecialActionsListViewModel` に見出しの文字 4 つ（`…HeaderText`）と `RefreshHeaderTexts` を追加し、XAML の `TextBlock` から `RelativeSource AncestorType=ListView` 経由でバインドした。`ProfileEditor.xaml.cs` から約 300 行（`EnsureSpecialActionsHeadersAssigned`、`UpdateSpecialActionsHeaderTexts`、`SetAndLogHeaderText`、`*SortButtonContent`、保持用のフィールド、開発用ログ約 25 行）を除いた。最初に特殊アクションのタブを開いたときの並べ替えは一度だけ適用する（`specialActionsSorted`）。編集画面は開いたまま設定タブへ移れず、言語を切り替えられないため、言語変更の通知は受けない（開くたびに表示言語で作る）。`RefreshHeaderTexts` は並べ替えの通知で使う。
- Debug ALL: 警告 0・エラー 0、3,267 件合格。
