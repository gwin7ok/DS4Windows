# Phase10-Step5 計画書: SA 側のアナログ出力（固定の強さ）

作成日: 2026-10-22
状態: **完了（2026-10-22）。S5a・S5b・S5c の全 Wave が完了。`Debug ALL` 成功: 警告 0・エラー 0、テスト 2150 件と 11 件**
前提: Phase10-Step6（統合 Step）、W7・W8 相当が完了。上位の決定: `Phase10-Assignment-UI-Unification-Plan.md` §6 Q2（SA 側にも、マウス移動、ホイール、Extras、アナログ軸を出す。強さは編集画面で 0〜100% を指定し、成立中はその固定値で出力する）。画面は、割り当て編集画面（`AssignmentEditor`）と出力ピッカー（`BindingWindow`）と呼ぶ。

## 1. 目的

SA の組み合わせトリガーはオン／オフのため、Controls のように、入力のアナログ値で出力の強さを決められない。SA の Button（Output Key/Mouse/Button）の出力先が、パッドの軸・トリガー、マウス移動のとき、**利用者が指定した強さ（0〜100%。既定 100）の固定値**で出力する。

## 2. 決定事項（2026-10-22、ユーザー承認）

- 強さは、割り当て編集画面の Press Key のタブで、出力先がアナログ（パッドの軸・トリガー、マウス移動）のときだけ指定する（0〜100 の整数。既定 100）。Controls のモードには出さない（Controls は、入力のアナログ値で強さが決まる）。
- 100% は、Controls のボタンが元のときの出力と同じ量（軸は端［正 255／負 0］、トリガーは 255、マウス移動はボタンが元のときの量）。強さは、これに比例させる（軸は中立 128 から端へ、トリガーは 255 × 強さ、マウス移動は量 × 強さ）。
- **ホイールは今のまま**（デジタルの 1 回クリック分。強さなし）。
- 軸・トリガー・マウス移動を出力先にした既存の SA は、強さの情報がないため 100% として読み込み、**100% のアナログ出力として動く**ようになる（以前は、アナログな出力として扱われず、意図どおりに動いていなかった）。強さは、利用者が変えられる。
- Toggle は、デジタルの Button と同じ規則（立ち上がりで反転し、ON の間は出す）。ログは、`ButtonOutputAction` と同じ文言・レベル。

## 3. Wave と結果

| Wave | 内容 | 結果 |
|---|---|---|
| S5a 保存と画面 | `SpecialAction.analogStrength`（既定 100）、`Actions.xml` の Button の `Strength` 要素（100 以外のときだけ。0〜100 に丸める。古いファイルは 100）、`SpecialActionsXmlSerializer.SetActionAnalogStrength`、`ISpecialActionRepository.SaveActionAnalogStrength`（SA の保存のあとに呼ぶ。SA の保存は SA を作り直すため）、アナログな出力の種類の規則 `AnalogOutputKind`／`AnalogOutputTargets`（出力先 → 軸・トリガー・マウス移動・なし）、`PressKeyViewModel`（`AnalogStrength`、`IsAnalogOutput`、`ShowAnalogStrength`、`StrengthApplies`）、割り当て編集画面の Press Key のタブの強さの入力 | 完了。テスト `Phase10Step5SaAnalogStrengthStorageTests`（23 件） |
| S5b 実行 | `ButtonBindingValues.Strength`、`ButtonOutputAction` にアナログの分岐（軸は `SetAxisPair`、トリガーは `SetTrigger`、マウス移動は `IFixedMouseInput`）、`IFixedMouseInput`＋`TriggerInput.FixedMouseSampler`、`Mapping.SampleFixedMouseMovement`（`getMouseMapping` のボタンが元のときの量［感度と縦倍率］× 強さ） | 完了。テスト `Phase10Step5SaAnalogOutputTests`（25 件） |
| S5c 仕上げ | 台帳 §30、モデル図 `03`、`Phase10-Status.md`、`Phase10-Integration-Target-Model.md` §12、`Phase10-Step6-Completion-Report.md` の次の作業 | 完了 |

## 4. 実機確認（台帳 §30。先送り）

P10-101（軸）、P10-102（トリガー）、P10-103（マウス移動）、P10-104（既存の SA の読み込みと保存）。
