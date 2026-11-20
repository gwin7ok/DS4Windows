# Phase7b 完了報告書: 持ち越し不具合の是正（Phase8 の前）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7b-Plan.md`（Q1〜Q11 を承認。Q2＝B、Q8＝B［内容を指定］、ほかは推奨どおり）。上位: `Phase7-Completion-Report.md` §5.1  
状態: **完了（一部先送り）**。W1・W2・W3・追加の是正（丸め）のビルド・テストビルド・テスト実行は成功（ユーザーが実行）。実機確認も問題なし。W4・W5 は先送り、W6 は取り下げ。

---

## 1. 結果の要約

- **項目 1（「Unload on trigger release」で元のプロファイルに戻らない）: 是正した。** 実機とログで、復帰が 1 回だけ走ることを確認した。
- **項目 5（K4-1）・項目 6（K7-2）: 是正した。**
- **追加の是正: L2／R2 のデッドゾーンの上下ボタンの丸め誤差**（ユーザー報告。0.20 → 0.29 のように 0.09 しか変わらない所があった）。是正した。
- **項目 2（L2／R2 のデッドゾーンの仕様変更の要望）: コードの変更なし。** 一度モードを追加したが、取り下げた（§3.3）。要望は、デッドゾーンとアンチデッドゾーンを同じ値にする設定で実現できる。
- **項目 3（K7b-1、異常終了）・項目 4（K9-3、右クリックのメニュー）: 先送り**（ユーザー判断）。
- ラチェットの基準値（Global 2／AppHost 3／rootHub 0／ServiceProviderHolder 17）は変わらない。

## 2. Wave ごとの実績

- W1（項目 1）: `IMapping` に `CaptureProfileUntrigger`／`RestoreProfileUntrigger` を追加し（`ProfileUntriggerSnapshot` を新設。メンバー数は 8 → 10）、`DefaultActionManager.ClearDeviceState` と、静的な `ActionManager.ClearDeviceState` が、`HandleDeviceDisconnect` の前後で復帰の合図（`UntriggerAction`／`UntriggerIndex`）を退避・復元する。`Mapping` の復帰処理は、`ProfileSwitchAction.Stop` の前に合図を消す（復帰の適用も `ClearDeviceState` を通り、復元されて毎フレーム復帰を繰り返さないようにするため）。実際の切断（`ControlService` の直接呼び出し）では、合図は消える。回帰テスト `Phase7bW1ProfileUntriggerPreservationTests`。
- W2（項目 5）: `Mouse.SixMouseStick` が、水平軸の設定を、固定の 0 ではなく、受け持つ台（`deviceNum`）から読む。
- W3（項目 6）: `VirtualKBMFactory.DetermineHandler("fakerinput")` が、導入判定が偽なら、ログを出して SendInput にする。回帰テスト `Phase7bW2W3SmallFixesGuardTests`（W2・W3 共通）。
- W4（項目 3）: 先送り。
- W5（項目 4）: コード調査（メニューを閉じる処理は見つからず、原因を特定できず）と、ベースラインの再現手順（`Phase7b-Plan.md` §9）まで。再現確認は先送り。
- W6（項目 2）: 取り下げ（§3.3）。
- 追加（丸め）: `ProfileSettingsViewModel` の L2／R2 のデッドゾーンを、スティックと同じ形（読み出しは小数 2 桁へ丸め、設定は四捨五入）にし、最大ゾーン・アンチデッドゾーン（L2／R2 と左右スティックの 8 か所）の切り捨て `(int)(value * 100.0)` を四捨五入にした。回帰テスト `Phase7bTriggerDeadZoneRoundingTests`。
- W7: 文書（本報告書、Status、チェックリスト U-23、モデル図 03 の注釈）。

## 3. 計画との差と、調査で判明した事項

### 3.1 項目 1 の旧実装との比較
- 上流の元の実装（2019-04-23）は、復帰先を `UntriggerAction.prevProfileName` から読み、`ClearDeviceState` は呼ばない。復帰できていた。
- 壊れた時期: `HandleDeviceDisconnect` の `deviceRuntime` の作り直し（`a892d372`、2025-12-18）と、適用時の `ClearDeviceState` の追加（`7d6b61e0`、2025-12-18）。**DI 移行前のフォーク独自の追加**で、Phase7 の退行ではない。
- Q2＝B（復帰先は、現状どおり、最後に適用した通常のプロファイル）のため、上流と違い、一時 → 一時の切替では、前の一時プロファイルではなく、通常のプロファイルへ戻る（仕様として承認済み）。
- 実機のログ（2026-10-18）: 離したとき `ApplyProfile`（復帰先 `test44`、`isTemp=False`）が 1 回だけ走り、復帰後に `SpecialAction UNTRIGGER check` が止まる。2 回目も同じ。

### 3.2 項目 5・6・丸めの旧実装
- 項目 5（K4-1）: 上流の元から `GetGyroMouseStickHorizontalAxis(0)`（2015 年の最初のコミットから）。
- 項目 6（K7-2）: 上流の元から。引数なしの通常起動では影響がない。
- 丸め: 上流の取り込み時点（2019-12-18、`906946a7`）から同じ切り捨て。スティックのデッドゾーンは、読み出し・設定とも丸めていて、問題がなかった。

### 3.3 項目 2（デッドゾーン）の経緯
- 要望: 「デッドゾーンを超えたら、押し込み量そのまま（アンチデッドゾーンとの大きい方）を出力したい」。現行は「超えた点を 0% として引き伸ばす」（Scaled。上流の元から。L2／R2 のほか、スティック［Radial／Axial］、6 軸の傾き、タッチパッドの Mouse Joystick も同じ性質。ジャイロのマウス系は「デッドゾーンを差し引く」）。
- 実施: L2／R2 に `Scaled`／`Cutoff` のモードを追加し、ビルド・テスト・実機確認は成功した（`a972e153`）。
- 取り下げ（ユーザー判断）: デッドゾーンとアンチデッドゾーンに**同じ値**を設定し、最大ゾーン・最大出力を 1.00 のままにすれば、Scaled のままで `出力 = (1 − a) × (入力 − d) ／ (1 − d) + a ＝ 入力`（`a = d` のとき）となり、Cutoff と同じ動作になる。L2／R2 もスティック（Radial の各軸でも成立）も同じ。誤差は、整数への丸めによる約 ±1%。設計を単純に保つため、モードを削除し、W6 の前の状態に戻した（`888ea26b`）。

### 3.4 ログで気づいた観察事項（Phase7b の範囲外）
- メイン画面のプロファイル選択（`source=Manual`）の切替のとき、`[WARN] Synchronous Button SA handling error: Collection was modified; enumeration operation may not execute.` が 1 件出た。例外は処理の内側で捕まえられていて、切替は成功している（`result=True`）。画面のスレッドでの切替が、入力のスレッドが一覧を列挙している最中に重なる競合とみられる。今回の 1 件だけでは、いつから出ているかは判断できない。再び出るかを様子見する（頻繁なら、別途調査）。
- Gamepad Tester に「Unknown Gamepad（Vendor 1532 Product 0244）」が出る件は、Razer Tartarus Pro（キーパッド）で、DS4Windows とは無関係（調査のみ。対応不要）。

### 3.5 再実行の原因（いずれもテストの問題。本体の回帰ではない）
- W1: `StandaloneTests` の `IMapping` を実装した 2 つのスタブに、追加した 2 メンバーが足りず、コンパイルエラー。`IMapping` のメンバー数を固定するテスト（`Phase7Step8W2MappingInstanceGuardTests`）の期待値の更新漏れ。

## 4. 起動順序・スレッド・ガードレールへの影響

- 起動順序: 変更なし。
- 5.2（適用時 Halt）: 変更なし。`ClearDeviceState` の前後の退避・復元は、プロファイル適用のスレッドで行う。入力のスレッドが合図を読む瞬間に `null` を見ることがあっても、次のフレームで復元され、害はない。
- 安全弁（Fresh-press ガード、`IsExecuting`）: 変更なし。

## 5. 未完了・持ち越し

- **K7b-1（異常終了することがある）**: 先送り。`Phase7b-Plan.md` §2 項目 3 の方針（まず現在のビルドで再現するかを確認）のとおり、再現確認から始める。原因はメモリ破壊の可能性がある（`Phase6-Status.md` §6.5）。
- **K9-3（右クリックのプリセットメニューのサブメニューが消える）**: 先送り。ベースラインの再現手順は `Phase7b-Plan.md` §9。
- 観察事項: §3.4 の `Collection was modified`。
- 実機確認: U-23（チェックリスト）の項目 1（復帰）は確認済み。項目 5（複数台）・6（FakerInput 未導入の明示指定）は、機材・環境がなく未確認。
- Phase8 への引き継ぎ: `Phase7-Completion-Report.md` §5.2 のとおり（変更なし）。

## 6. 次の Step

- Phase8（`Phase8-Unified-Trigger-Dispatch-Plan.md` の D1〜D5）の着手前に、Phase7・Phase7b の引き継ぎ（§5）を取り込む（ユーザーの指示を待つ）。
- 先送りした項目（K7b-1、K9-3）は、任意のタイミングで、`Phase7b-Plan.md` の手順で再開できる。
