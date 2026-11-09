# Phase15 完了報告書: UI 共通化

作成日: 2026-11-09
ブランチ: `Phase15`（master c0c266f4 から作成。実機確認のあと master へ fast-forward で取り込む予定）
計画書: `Phase15-Plan.md`（決定 P1〜P10 承認済み）、`Phase15-6-Plan.md`
根拠: `Repository-Review-2026-11-05.md`（R-09・R-10・R-12・R-14）、`FurtherImprovements/02`・`03`・`06`、`Phase9-Deferred-Ledger.md`（F-4・G-1）

## 1. 結果の要約

- UI が `ControlService`（具象）を経由して第 1 層・第 3 層へ抜ける経路を、第 2 層の小さな契約へ置き換えた。UI → 第 1 層・第 3 層の依存（記録のみの規則 L7）は 80 件から 5 件になった。
- テーマの色の名前と直書き、数値ラベル・数値入力の寸法、コントローラーの図のボタンを、共有の定義（トークン、共有スタイル、配置表）にまとめた。見た目は変えていない。
- ViewModel の変更通知を共通の基底（`ObservableObject`）に集め、画面のコードビハインドの ViewModel の生成を `IViewModelFactory` に集めた。
- プロファイル編集画面（`ProfileEditor`、`ProfileSettingsViewModel`）を、8 つの領域（ジャイロ、タッチパッド、トリガー、スティック、ライトバー、出力、一般、6 軸）に分けた。各領域は、設定サービスの窓口（`ForSlot(slot)` が返す `…SlotSettings`）、サブ ViewModel、パネル（UserControl）を持つ。
- 機能・設定の保存形式・利用者に見えるログは変えていない。**実機確認は未実施**（P9: 最後に 1 回）。

## 2. 主な変更

| Step | 内容 |
|---|---|
| 15-1 | UI 向けの窓口（R-09）: `IControllerSlotView`／`IControllerSlotQuery`／`IControllerSlotCommands`／`IControllerReadingsSource`／`IMacroRecordingState`／`IOutputSlotView`／`IServiceLifecycle`。機器の入れ子の列挙型 4 つと `ConnectionType` を `Core/Model` へ。`MainWindow` の引数 28 → 21。規則 L8（UI → `ControlService`）を凍結し、のちに 0 件 |
| 15-2 | テーマ: ブラシ 15 個を `…Brush` へ改名、Dark だけのキーの調査と対称化、直書きのトークン化。`ThemeTokenRuleTests` |
| 15-3 | 数値入力: 読み取り値ラベルの共有スタイル、`NumberInputSmall`／`Medium`／`Large`／`Wide`。`SettingAxisRow` は実在しない形のため作らなかった |
| 15-4 | `UI/Controls`（規約 R1 に追加）に `ControllerVisualizerControl` と配置表（Xbox360・DualShock4・DualShock4Editor）。`BindingWindow`・`ProfileEditor` のボタン 26＋34 個を生成に置き換え |
| 15-5 | R-12: `ObservableObject`（`SetProperty`）。R-14: コードビハインドの `new …ViewModel(` 28 か所を `IViewModelFactory` へ |
| 15-6a〜e | ジャイロ、タッチパッド、トリガー、スティック、ライトバーの窓口・サブ ViewModel・パネル |
| 15-6f | 出力（仮想コントローラー、振動）。種類の選択は保存値と出力ピッカー用の一時の種類の両方へ書く。振動のテストとマッピング一覧の更新は `ProfileEditor` が持ち、パネルはイベントで知らせる |
| 15-6g | 一般（入力の表示、ボタンによるマウス移動、プログラムの同時起動、無操作での切断、ポーリング間隔） |
| 15-6h | 6 軸（SX／SZ）。`ProfileEditor` の DataContext の付け外しを 2 つのメソッドに集約 |
| 15-6Z | 編集画面からスロット配列の直接参照を 0 に。`SettingsSlotWindowUsageTests`。規約 §3.1 に窓口を追記 |
| 15-7 | 規約・設計図 03・ロードマップ・台帳・`CHANGELOG.md` の更新、Debug ALL、Release ビルド |

## 3. 判断した点

- P8＝C（全領域）、P9＝B（実機確認は最後に 1 回）は、推奨と異なるユーザー判断。不具合の原因の Step を特定しにくくなるため、Binding 名を XAML から集めて ViewModel の公開プロパティと突き合わせる検査を領域ごとに足した（`…SettingsViewModelTests`）。
- パネルの XAML の根には `lex:` の 3 属性（`DefaultAssembly` など）を付けない。付けると WPFLocalizeExtension が既存の全リスナーへ通知し、別スレッドで作られた古い部品の Dispatcher を待って、全件実行のテストが止まった（`dotnet-dump` で特定）。パネルを読み込むテストは `WpfPanelTestRunner`（共有の STA スレッド）と `[Collection(SharedHostStateCollection.Name)]` で動かす。
- `IProfileSettingsService` の配列プロパティは削除しなかった。157 メンバーのうち窓口以外から使われないのは 17 件で、15 件は窓口自身が使う。残る `ReverseX360ButtonMapping`／`UseTempProfileArray` は規約 2.4 の未接続の記録がある。他の層（処理・永続化）が使う配列が大半。
- 記録のみの規則 L2（Core → 第1層・第3層）が 29 → 30 になった。`OutputSlotSettings` が `DualSenseDevice.RumbleEmulationMode`（第 1 層の入れ子の型）を参照するため。列挙型を `Core/Model` へ移せば消える（TODO コメントあり）。

## 4. 検証

- Debug ALL（`Remove-bin` から）: ビルド 警告 0・エラー 0、`DS4Windows.Actions.Tests` 3,257 件合格（Benchmark を除く）、`StandaloneTests` 11 件合格。
- Release ビルド 2 か所を作成（2026-11-09 20:18）: `DS4Windows\bin\x64\Release\`（publish）、`DS4Windows\bin\x64\DS4Windows\`（build。`DS4Windows_4.3.1_x64.zip` を含む）。警告 0、エラー 0。
- 実機確認: 実施中。確認項目は §6。1 項目めのあと、2 項目めで下記の不具合が見つかり、直した（§8）。

## 5. 件数

| 指標 | 開始 | 完了 |
|---|---|---|
| UI → 第 1 層・第 3 層（L7、記録のみ） | 80（Phase14b 後。15-1-0 で 28 に更新） | 5 |
| Core → 第 1 層・第 3 層（L2、記録のみ） | 29 | 30 |
| 禁止の規則の違反 | 0 | 0 |
| テスト（Actions） | 3,150 | 3,257 |

## 6. 実機確認の項目（P9: 最後に 1 回。Release ビルド 2 か所）

1. 起動、コントローラーの接続・切断、スロットの表示。
2. ダーク・ライトの両テーマで、全タブの表示（色、数値入力の見切れがないこと）。
3. 割り当て編集・出力ピッカーのコントローラーの図で、ボタンのホバー・クリックがずれないこと（DS4 と Xbox360 の表示）。
4. プロファイル編集画面の全タブ: ジャイロ、タッチパッド、L2/R2、スティック（軸別デッドゾーンを含む）、6 軸、ライトバー（色の選択中に実機が光ること）、その他（仮想コントローラーの種類、振動のテストの開始・停止、マウス、プログラムの同時起動、無操作での切断、ポーリング間隔）。
5. 保存・適用・キャンセル・プリセット適用・プロファイルの読み込み直しで、値が正しく表示され、保存されること（特に出力の種類の変更と、マッピング一覧のボタン名の表記）。
6. ログの表示と言語の切り替え。

## 7. 残り（次の候補）

- 特殊アクション一覧の見出し・並べ替え（`UpdateSpecialActionsHeaderTexts` ほか）の整理。表示の生成のタイミングに依存し、見え方の確認が要る。
- 編集画面の外のスロット配列の直接参照 5 か所（`CompositeDeviceModel`、`RecordBoxViewModel`、`PressKeyViewModel`、`TouchButtonUserControlViewModel`、`BindingWindow`）。
- L2 の整理（`DualSenseDevice.RumbleEmulationMode` ほか、列挙型の置き場所）。

## 8. 作業上の記録

- コミット 30 件（この報告書のコミットを含む）。1 領域＝1 コミットなので、実機確認で問題が出た領域だけ戻せる。
- Bash の heredoc で Python を書くと、バックスラッシュが解釈されて失敗することがあった。スクリプトはファイルに書いてから実行した。
- **実機確認で見つかった不具合（2026-11-09）**: 接続したコントローラーのプロファイル編集画面を開くと、`InvalidCastException`（`ProfileEditor` の `IComponentConnector.Connect`）で落ちた。原因は、`ControllerVisualizerControl.Resources` の中の `ContextMenu`（`presetMenu`）の `MenuItem` に付けた `x:Name` と `Click` が、読み込み時に接続番号とずれたこと。ビルドと既存のテストは、XAML を実際に読み込まないため通っていた。対処: リソースの中の部品から `x:Name`・`Click` を外し、`ContextMenu` の `MenuItem.Click`（バブリング）で一括して受ける。再発防止: `ProfileEditorMarkupLoadTests`（XAML の読み込み、およびリソースの中に名前・イベントを持つ部品がないことの検査）。
