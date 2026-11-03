# Phase13 進捗

計画: `Phase13-Plan.md`（承認済み 2026-11-04）。ブランチ: `Phase13`。

| Step | 内容 | 状態 |
|---|---|---|
| 1 | トリガー系クラスの未使用 `profileSettings` 引数の削除 | **実装完了（2026-11-04）。未コミット** |
| 2〜12 | 計画書 §4 のとおり | 未着手 |

## Step1 の結果

- 削除した引数: `ControlButtonInputAction`／`ComboTriggerInputAction`／`MultiTapInputAction`／`ControlMultiTapInputAction`／`TwoStageTriggerProcessor` の `profileSettings`。各クラスの「引数を残した理由」のコメントと `<param>` も削除。
- 連動: `AssignmentBindingBuilder` の `_profileSettings` はこれらでしか使われていなかったため、フィールドと引数を削除（`ActionBindingBuilder`、`ControlBindingBuilder` の呼び出しを更新）。
- テスト: 12 ファイルの `new` を更新。`Phase10Step4W4ControlSpecialTests` の `ADelayWithoutTheSettings_IsRejected_AndNoDelayNeedsNone`（null 引数の拒否を検査していた）は前提が無くなったため、`ADelayWithoutAnyOtherArgument_IsAccepted` に置き換え。`ComboTriggerInputActionTests` の「settings が null なら例外」の 1 行も削除。
- 検証: `Debug ALL` 相当（Remove-bin → Build Debug → Build Test → Test）で警告 0・エラー 0。Actions 3,043 件、Standalone 11 件が合格。
- ラチェット: `Global`／`AppHost`／`ServiceProviderHolder` の件数に変化なし（基準値の更新なし）。
