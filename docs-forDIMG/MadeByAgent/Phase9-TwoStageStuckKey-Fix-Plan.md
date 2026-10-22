# Phase9 2 段階トリガーの「キーが押されたままになる」不具合の修正 計画書

作成日: 2026-10-22
状態: 修正を実装済み。ビルド・テスト・実機確認は待ち（ユーザーが原因の調査と修正を依頼）

## 1. 症状（実機確認 P9M3-1 で発見）
L2／R2 の 2 段階トリガーを Hip Fire（遅延 100ms）にして、L2＝キー a、L2 Full Pull＝キー b とし、素早く押して離すと、浅い引き・深い引きのどちらのキーも押されたままになることがある（離しても連続で出力され続け、もう一度押すと止まる）。L2・R2 の両方で起きる。

## 2. 切り分け
- M1 の直後（8742734、M2・M3 より前）でも発生する。→ **Phase9 の分割で生じたものではない**。Phase8 でトリガーの割り当てを新経路（エンジン）へ移した後から潜在していた不具合と見られる（P9M2-2〜4［Hip Fire ほか］は、これまで実機確認をしていなかった）。
- ログ（`ds4windows_log_20261022_37.txt`）: 浅い引きのキー（`Synthetic_Key_0_10`）が 22:17:11.1993 に押された後、`Trigger released` が一度も出ず、`RepeatHelper` が 33ms ごとに押し続けている（切断まで）。深い引きのキー（`Synthetic_Key_0_43`）は、押下と解除が同じフレームで繰り返されている。

## 3. 原因
`Mapping` の L2／R2 の処理は、「前のフレームで出力していたボタン」（`previousActiveButtons`）を見て、出力が止まったフレームでも割り当ての処理（`ProcessControlSettingAction`）を 1 回呼び、離した値を渡して、Key などの解除を行う。
ところが `ProcessTwoStageTrigger`（`TwoStageTriggerProcessor.Process`）は、トリガーを離したフレーム（値 0、出力が有効だった）で `triggerData.Reset()` を呼び、その中で `previousActiveButtons` も消す。呼び出し側は、`Process` を呼んだ後に `previousActiveButtons` を読んでいたため、離したフレームでは「前は出力していなかった」と見なし、割り当ての処理を呼ばなかった。その結果、新経路（エンジン）に離した値が渡らず、Key が押されたままになる。
Hip Fire は、短い押下で出力が 1 フレームだけ有効になるため、起きやすい。Hair Trigger と Exclusive Buttons も、同じ構造（値 0 で Reset）なので、同じ条件で起こりうる。

## 4. 修正
`Mapping.ProcessTriggerWithTwoStage` で、`Process` を呼ぶ**前**に `previousActiveButtons` を退避し、Soft Pull／Full Pull の「前のフレームで出力していた」判定には、退避した値を使う。`Process` と状態機械は変えない（旧コードとの差分テストは、そのまま通る）。フレームの終わりに `previousActiveButtons = tempButtons` を保存する処理も同じ。
離したフレームでは、出力が null でも、退避した値に Soft／Full のビットが立っているため、入力（`fieldMapping`）の値を 0 にしたうえで割り当ての処理を呼び、Key が解除される。

## 5. 検証
- `Phase9MapCustomTwoStageKeyReleaseTests`: Hip Fire／Hair Trigger／Exclusive Buttons の各モードで、L2・R2 の浅い引きのキー（a）を押した後、トリガーを離すと、キーの解除が記録されることを調べる（修正前は解除されない）。
- 実機: Hip Fire（遅延 100ms）で、素早く押して離す操作を繰り返し、キーが押されたままにならないこと。Hair Trigger・Exclusive Buttons・Normal でも同様に確認する。
