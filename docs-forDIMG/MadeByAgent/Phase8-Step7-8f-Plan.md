# Phase8-Step7-8f 計画書: Controls タブの Button（X360 のボタン・軸・トリガー、マウス、ホイール）の新経路への移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.3 の F、§10。Q8＝B［実機確認は Step9］、Q9＝A［値渡し］）、`Phase8-Step7-8a-Plan.md`（土台）、`Phase8-Step7-8b-Plan.md`（Key。Default と Unbound の表）、`Phase8-Step7-8e-Plan.md`（Extras。1 ボタン複数束縛）、`Phase8-Step7-1-Plan.md`（SA の Button）  
状態: **Q1〜Q7 を承認済み（2026-10-20。ユーザー。Q5＝B、他は推奨）。W0（調査）完了［§5.1］。Q8＝A も承認済み。7.8f-1 の W1（旧経路の特性化テスト）を実装［ビルド・テストの結果待ち。§5.2］。製品コードは未着手**

---

## 1. 目的と範囲

Controls タブの「Button」の割り当て（ボタン・軸・トリガー・ジャイロなどに、X360 のボタン、軸、トリガー、マウスのボタン・ホイール・移動、タッチパッドクリックを割り当てる）を、旧経路（`Mapping.ProcessControlSettingAction` の Button の枝と、`customMapQueue`）から統一エンジンへ切り替える。これで、**Controls タブの割り当て（Default 以外）は、すべて新経路に載る**。旧コードは Step8 で削除する。

**範囲（ボタン単位の所有。7.8b〜e と同じ）**: dcs（`DS4ControlSettings`）が Button で、出力先（X360 のコントロール）の区分が、移行済みのものだけを新経路が処理する。Extras は 7.8e で移行済みなので、Button＋Extras も新経路に入る。

**やらないこと**: 旧コード（`Mapping` の Button の枝、`customMapQueue`、`GetBoolMapping` ほか）の削除（Step8）、動作の改善（§4 の承認なしには変えない）、実機確認（Step9。U-45）。

## 2. 現状の調査（2026-10-20。`Mapping.cs`）

### 2.1 旧経路の Button の枝（dcs ごと、毎フレーム）

出力先（`actionBtn`）の区分ごとに、動きが違う。割り当てたボタンの通常の出力は、毎フレーム `ResetToDefaultValue` で消す（出力先が Unbound や None でも、消す）。

| 区分 | 出力先 | 押下の判定 | 書き込み先 | 備考 |
|---|---|---|---|---|
| A. パッド | X360 の軸（`LXNeg`〜）、トリガー、ボタン（`Start` まで） | **キューに積み、全 dcs の処理のあと（Stage3 の前）に処理**。軸は `GetXYAxisMapping`（アナログ。元がボタンなら 0／255）、トリガーは `GetByteMapping`（アナログ）、ボタンは `GetBoolMapping`（スティック ±55、トリガー 100 超の SA 風の判定） | `outputfieldMapping` の軸・トリガー・ボタン | 軸は、反対側の向きにも同じ値を書く |
| B. タッチパッドクリック | `TouchpadClick` | `GetBoolMapping` | `outputTouchButton` | |
| C. マウスのボタン | 左・右・中・4・5 | `GetBoolActionMapping`（Controls の判定） | `currentClicks` の各カウンター | |
| D. ホイール（デジタル） | `WUP`／`WDOWN`。元がボタン | `GetBoolActionMapping` | `currentClicks.wUpCount／wDownCount` | |
| E. ホイール（アナログ） | `WUP`／`WDOWN`。元がスティック・トリガー・ジャイロ | `GetBoolActionMapping` で成立 → `GetMouseWheelMapping` | 直接 `VirtualKBM.PerformMouseWheelEvent`（10ms ごと、残りの端数を持つ） | **Mapping 全体で 1 つの状態**（`oldnow`、`pressagain`、`stickWheelRemainder`、`stickWheelDownDir`）。向きが変わると端数を捨てる |
| F. マウスの移動 | `MouseUp`／`Down`／`Left`／`Right` | 判定なし。`getMouseMapping`（アナログ。感度・加速・デッドゾーン・経過時間を使う） | 局所変数 `tempMouseDeltaX／Y` へ（0 のときだけ書く＝先に書いた方が勝つ）。フレームの最後に `calculateFinalMouseMovement` → `MoveRelativeMouse` | スティックのマウス・フリックスティックも、同じ局所変数に足す |
| G. Toggle | A〜F のどれかに `Toggle` のフラグ | `GetBoolActionMapping` | `currentClicks.toggle`／`toggleCount`（状態は `GetIsToggledOn(keyvalue=256〜)`） | |

- **Default の割り当て（`else` 枝）**: 軸（`AxisDir`）のボタンだけ、`ControlToXInput(control, control)` をキューに積む（素通し）。割り当てたボタンの消去が、軸の反対側の向きも消すため、Default 側の向きを戻す役目を持つ（7.8b W1 の発見）。トリガー・ボタンの Default は、何もしない。
- **キューの処理（`customMapQueue`）**: 全 dcs のあと、SA の Button の Stage3 の前。**読むのは入力の表（`fieldMapping`）、書くのは出力の表**。したがって、書き込みは、消去（出力の表）の後でも前でも、結果は同じ（入力は消去されない）。ボタンは OR、軸・トリガーは「0／128 でなければ書く」。
- 2 段トリガー（L2／R2）、ジャイロスワイプ: dcs の選択は `Mapping` に残す（Step2b §3.1）。ジャイロスワイプは 7.8b から対象外（前の向きを先に離す順序のため）。7.8f で扱えるかは W0 で確かめる（Q7）。

### 2.2 SA の Button（7.1 で新経路化済み）との違い

`ButtonOutputAction` は、デジタルの出力だけ（`PressX360`: ボタン配列・マウスのカウンター・ホイールのデジタル・タッチパッドクリック）。**アナログの値（軸・トリガー）、アナログのホイール、マウスの移動、Default の素通し、局所変数の移動量**は、新経路にまだない。

## 3. 設計（案）

### 3.1 層の割り振り

| 層 | 担当 |
|---|---|
| 入力監視層 | `TriggerInput`（アナログの値の読み取りを足す: 軸［`GetXYAxisMapping` 相当］、トリガー／バイト［`GetByteMapping` 相当］、マウス移動用の値） |
| 信号変換層 | `ControlBindingBuilder`（Button の出力先の区分の解釈、判定の種類の選択、束縛の組み立て）、エンジン |
| 出力層 | 区分ごとの出力（`ControlPadOutputAction`、`ControlMouseButtonOutputAction` ほか）。静的な状態・直接の送出は、ポート越し |

### 3.2 追加・変更するもの（案）

- **`IFrameOutput` の拡張**: 軸・トリガー・ボタンの書き込み（出力の表への直接の書き込み。旧キューの処理と同じ規則）、マウスの移動量の加算（局所変数の置き換え。Q6）。
- **アナログ値の読み取り**: `ITriggerInput`（または `IControlTriggerInput`）に、`GetAxisValue(control, alt)`、`GetByteValue(control)` を足す（旧 `GetXYAxisMapping`／`GetByteMapping` の移植。`TriggerInputTests` と同じ方式で、全コントロールを旧と比べる）。
- **束縛**: Button の dcs 1 つにつき、出力先の区分に応じた束縛を 1 つ（Extras があれば、その前に Extras の束縛。7.8e）。方針は `erasesOutputEveryFrame: true`（割り当てたボタンの通常の出力を毎フレーム消す）、Fresh-press ガードなし、抑制なし。
- **判定の種類**: A のボタン・B は SA 風（`ControlJudgement.Trigger`。7.8c の前例。旧 `GetBoolMapping` との差は、ジャイロの閾値だけ）、C・D・E・G は Controls の判定（`Assignment`）。A の軸・トリガー、F は、判定ではなくアナログ値を毎フレーム読む。
- **Default の素通し**: Q2。
- **移行のスイッチ**: Q1。

## 4. 動作が変わる点（W1 の実測のあと、確定して承認を得る）

初版の見立て。**変えないことを原則とする**（規約 2.2／2.3）。

1. A のボタンと B の判定（`GetBoolMapping` → SA と同じ判定）: ジャイロの閾値だけが変わる（7.8c で承認済みの考え方。Q3）。
2. E のホイールの状態（Mapping 全体で 1 つ）: 変えない（Q5）。
3. 実行の順序: キューの処理は、Stage3 の前だった。新経路は、Stage3 と同じ段階（`ApplyFrameOutputs`）で書く。入力は消えないため、結果は同じ（W1 で確かめる）。
4. ログ: 旧経路の Button の枝にログはない。新経路も出さない。

## 5. 作業（Wave と PR の分け方。Q1）

3 つの PR に分ける案（それぞれ W0〜W4。Extras と同じ進め方）:

- **7.8f-1 パッド（区分 A）と Default の素通し**: アナログ値の読み取り、`IFrameOutput` の拡張（軸・トリガー・ボタン）、パッドの出力、素通し。スイッチ `ButtonPad`。
- **7.8f-2 マウスのボタン・タッチパッドクリック・ホイール（デジタル）・Toggle（区分 B・C・D・G）**: 既存の `PressX360` を使う。スイッチ `ButtonMouse`。
- **7.8f-3 アナログのホイール・マウスの移動（区分 E・F）**: 状態の置き場（Q5）、移動量の置き場（Q6）、`getMouseMapping`／`GetMouseWheelMapping` の移植。スイッチ `ButtonAnalogMouse`。

- **W0（調査。完了。結果は §5.1）**: ① `GetBoolMapping` と `IsActive` の全コントロールでの差（ジャイロ）。② 素通しの値の書き方（反対側の向きにも同じ値を書く）が、素通しなしの結果と変わらない条件。③ ジャイロスワイプ・2 段トリガーの dcs を新経路に入れられるか。④ `currentClicks` の Toggle の規則。⑤ マウス移動の入力（`lastTimeElapsedDouble`、加速、デッドゾーン）の取り方。⑥ ホイールの状態（`pressagain` ほか）の使われ方。
- **W1**: 旧経路の特性化テスト（旧・新の両経路の Theory。各区分の出力、アナログの値、消去、Toggle、押したままの適用・切断）。
- **W2**: 実装。**W3**: 割り当て・ロールバック・ガード。**W4**: 文書（U-45）。

### 5.1 W0 の結果（2026-10-20）

1. **`GetBoolMapping` と `TriggerInput.IsActive` の差**: ボタン・スティック（±55）・トリガー（100 超）・タッチ・スワイプは同じ。**ジャイロだけ**違う（旧は 0 より大きい／小さい、`IsActive` は ±67）。→ Q3＝A（SA の判定に合わせる）で、変わるのはジャイロの閾値だけ。
2. **Default の素通し**: 軸の Default だけ `(control, control)` をキューに積む。キューの処理は、軸のペアの**両方の向きに同じ値を書く**（`GetXYAxisMapping` の値が 128 でないとき）。割り当てたボタンの消去も、軸のペアの両方を 128 にする。したがって、素通しは「反対側の向きが割り当て済みのとき」に意味がある。割り当てがないペアでは、入力と同じ値を書き直すだけ（W1 で確かめる）。→ Q2＝A（反対側が割り当て済みの軸だけ束縛を作る）で足りる見込み。
3. **ジャイロスワイプ・2 段トリガー**: `Mapping` が、dcs ごとに `ProcessControlSettingAction` を呼ぶ（選択は `Mapping`）。離れた向き・解除する段は、`fieldMapping` を未成立にして呼ぶ。エンジンの束縛は「呼ばれたときに評価する」ため、同じ呼び方で離したエッジ（Stop）が起きる。→ 新経路に入れられる見込み（7.8b の除外は保守的だった）。W1 のテストで確かめ、Q7＝A のとおり入れる。
4. **Toggle（旧経路の不具合の疑い。Q8）**: 旧経路は、Toggle の割り当てが押されている間、`GetIsToggledOn(keyvalue, null, device)` を呼ぶが、`action` が null のため、常に偽を返す。→ **押している間、毎フレーム `currentClicks.toggle` を反転する**（`toggleCount` が 0 → 非 0 になる立ち上がりのフレームで `Commit` が使う値は、押していた期間のフレーム数の偶奇で決まる）。SA の Button の Toggle（立ち上がりで反転）とは違う。W1 で実測する。
5. **マウスの移動**: `getMouseMapping` は、`ctrl.ProfileSettingsService.LSModInfo／RSModInfo`（デッドゾーン）、`ButtonMouseInfos[device].activeButtonSensitivity`（Extras の感度の一時変更が書く値）、`deltaAccelProcessors[device]`（`Mapping` が持つ、スティックの加速の処理）、`ctrl.DS4Controllers[device].lastTimeElapsedDouble`（経過時間）を使う。→ 7.8f-3 で、これらを渡す窓口（ポート）が要る。
6. **ホイールの状態**: `oldnow` は、アナログのホイールの 10ms の間隔と、`Commit` のデジタルのホイールの 150ms の繰り返しで**共有**されている（アナログのホイールが動くと、デジタルのホイールの繰り返しが遅れる）。`pressagain` は、どこにも true を代入しておらず、常に偽（実質、使われていない）。ホイールを直接送る（`PerformMouseWheelEvent`）ため、出力の文脈の `OutputHandler` で足りる（ポートは不要）。→ Q5＝B（束縛ごとに持つ）で、この共有はなくなる。

### 5.2 7.8f-1 W1 の実装（2026-10-20。ビルド・テストの結果待ち）

- `ControlsKeyTestScope.MapButton`（Button の割り当て）を足した。`Phase8Step78fW1ControlsPadCharacterizationTests`（旧・新の両経路）: ボタン → パッドのボタン［移して元を消す］、スティックの向き［幅 55］・トリガー［100 超］の判定、ボタン → 軸［両端へ］、アナログの軸 → 軸［値を保つ］、ボタン／トリガー → トリガー、Unbound［消すだけ］、割り当てた軸の反対側の Default の素通し、割り当てなしの素通し、Button＋Extras。
- 範囲外（7.8f-1 の W2 で確かめる）: 2 段トリガー、ジャイロスワイプ、ジャイロの閾値（Q3＝A の変更点）。

## 6. 質問と決定

承認: **2026-10-20（ユーザー）。Q5＝B、他は推奨（A）。**

- **Q1（分け方とスイッチ）【承認: A】**: 3 つの PR に分け、スイッチも `ButtonPad`／`ButtonMouse`／`ButtonAnalogMouse` に分ける（`Button` は置き換える）。
- **Q2（Default の素通し）【承認: A】**: 新経路の束縛にする（反対側の向きが割り当て済みの軸だけ）。
- **Q3（A のボタン・B の判定）【承認: A】**: SA と同じ判定（`Trigger`）に合わせる。ジャイロの閾値だけが変わる。
- **Q4（アナログ値の読み取り）【承認: A】**: `TriggerInput` に旧関数の移植を足し、旧と全コントロールで比べる。
- **Q5（ホイールの状態）【承認: B。推奨の A から変更】**: 束縛ごとに持つ（端数・向き・間隔の時刻）。Mapping 全体で 1 つの状態をやめる。複数のアナログ元をホイールに割り当てたときの干渉と、`Commit` のデジタルのホイールの繰り返しが遅れる癖がなくなる（承認済みの動作変更）。
- **Q6（マウスの移動量）【承認: A】**: 局所変数 `tempMouseDeltaX／Y` を、デバイスごとの加算器に置き換える。
- **Q7（ジャイロスワイプ・2 段トリガー）【承認: A】**: 新経路に入れられるものは入れる（W0 の見込みは §5.1 の 3）。

### 6.1 W0 で見つかった質問【承認済み。2026-10-20。ユーザー】

- **Q8（Toggle。§5.1 の 4）【承認: A】**: A: SA の Button の Toggle と同じにする（立ち上がりで 1 回反転。押している間は反転しない）。旧経路の「押している間、毎フレーム反転する」不具合を直す（動作変更。W1 の実測で不具合を確認してから確定する）。B: 旧経路の動きを再現する（フレーム数の偶奇で状態が決まる、予測できない動き）。

## 7. 完了条件

- [x] Q1〜Q7 をユーザーが承認（2026-10-20）。W0 の結果を追記（§5.1）。
- [x] Q8 の承認（2026-10-20）。7.8f-1〜3 の各 PR で、W0〜W4 を完了（ビルド・テストが成功。割り当て［新 ≦ 旧］、ロールバック、全ガード）。
- [ ] 実機確認は Step9（U-45）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-Plan.md` の更新。

## 8. 次

Step8（旧コードの削除: `ProcessControlSettingAction` と `customMapQueue` ほか、旧 `Synthetic` の仕組み、`FindSpecialActionForLogicalKey`、互換のコンストラクター、旧 `PlayMacro` のボタンごとのフラグ、Extras の `held`／`extrasRumbleActive`）。そのあと Step9（総合確認。U-35〜U-45 の実機確認）。
