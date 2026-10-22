# Phase9 Exclusive Buttons の「戻る途中で浅い引きが出る」挙動の修正 計画書

作成日: 2026-10-22
状態: ユーザーが修正を承認（2026-10-22。「B でお願い」）。実装済み・ビルド、テスト、実機確認は待ち

## 1. 症状（実機確認 P9M2 で発見）
L2 の 2 段階トリガーを Exclusive Buttons にして、L2＝A、L2 Full Pull＝B とし、ゆっくり押し込んで離すと次のようになる。
1. 浅い引きで A が出る。
2. 深い引き（生値 255）で B だけが出る。
3. **離す途中（生値 255 未満、値 0 でない）で、B が止まり、A が出る。**
4. 値 0 で何も出なくなる。

## 2. 原因
`ExclusiveButtons` で、状態が「深い引きのみ」のときに生値が 255 を割ると、どの分岐にも入らず、入力された浅い引きの割り当てが `null` にされないまま出力される。これは変更前の `Mapping.ProcessTwoStageTrigger` から引き継いだ取りこぼしで、Phase9 の分割による劣化ではない（差分テストで旧コードと一致）。

## 3. 修正
`TwoStageTriggerProcessor` の `ExclusiveButtons` に、状態が FullPullOnly で値が 0 でない場合は `dcsTemp = null` とする分岐を足す。`HipFireExclusiveButtons` の戻りと同じ規則（離す＝値 0 まで何も出さない）。値 0 の `Reset()` と、その後の浅い引きは変えない。

## 4. 検証
- 差分テスト（`Phase9MapCustomTwoStageDifferentialTests`）は、旧コードの逐語コピーとの比較のまま。唯一の意図した差（Exclusive の戻りで浅い引きが null）だけを期待値に反映した。
- `Phase9MapCustomTwoStageCharacterizationTests` に、浅い → 深い → 戻り → 離す → 再度の浅い、の通しのテストを足した。
- 実機: Exclusive Buttons で押し込んで離し、戻る途中で A が出ないこと。Normal／Hip Fire などが変わらないこと。
