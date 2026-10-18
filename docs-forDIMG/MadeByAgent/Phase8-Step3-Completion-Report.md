# Phase8-Step3 完了報告書: 判定と束縛の型の新設（配線なし）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step3-Plan.md`（Q1〜Q7 を推奨どおり承認）  
結果: **W1〜W5 のビルド・テスト成功（ユーザー実行。DS4Windows.Actions.Tests 920 件成功、StandaloneTests 13 件成功）。本番の挙動は変えていない**

---

## 1. 成果

- 新しい型は `Mapping`・DI のどちらにも配線していない（単体テストからのみ使われる）。本番の挙動は不変。配線は Step4 以降。
- 配置は `DS4Windows/Actions/`（R3）、名前空間 `DS4Windows.Actions`（R6）、1 ファイル 1 型。静的な窓口は新設していない（ラチェットの基準値は不変で、一致検査も成功）。

| Wave | 内容 | ファイル |
|---|---|---|
| W1 | 契約と値型 | `DelayClock`、`TriggerState`、`ITriggerInput`、`IInputAction`（改訂）、`UntriggerPolicy`、`BindingPolicy`、`IActionBinding`（改訂）、`TriggerBinding`、`KeyActionBinding`（追従）。`KeyInputAction` を削除 |
| W2 | 入力のスナップショット | `TriggerInput`（`Mapping.getBoolSpecialActionMapping` と同じ判定。前フレームの表は遅延展開。時刻は注入可能） |
| W3 | トリガー条件 | `ComboTriggerInputAction`（AND 条件、`delayTime`［`input.Delay` を共有］、`InvertOnRelease`［内部の準備状態。Q2＝A］） |
| W4 | Key 出力 | `KeyOutputAction` を `KeyAction` への委譲に書き直し。`ITrigger`／`SingleButtonTrigger` を削除 |
| W5 | 性能の基盤 | 30 束縛 × 2 万フレームで割り当て 0 を厳密に検査、処理時間は出力のみ（Q5＝A） |

## 2. テスト（追加分）

- `Phase8Step3ContractTests`（12）: 値型の不変性、既定値、契約の形。
- `TriggerInputTests`: 全 `DS4Controls` × 400 パターン × ジャイロ制御の有無で、現行の `getBoolSpecialActionMapping`（リフレクション）と一致。境界値、前フレーム、時刻の注入、割り当て 0。
- `ComboTriggerInputActionTests`（9）: AND、遅延［再開始・共有クロック・遅延時は反転しない］、離したときに成立［準備→離して 1 回、Reset］、割り当て 0。実時間のスリープなし。
- `KeyOutputActionTests`（6）: 系統 B（`KeyActionAdapter`）と同じ押下・解放の列（Press／Toggle）、ネイティブキーが `SyntheticDispatcher` と一致（Q7＝A の確認）、`Id`・null 安全。
- `Phase8Step3PerformanceTests`（2）: 割り当て 0。計測結果の例（開発機）: **30 束縛 1 フレーム 1.70µs、1 判定 56.7ns**（Step7 の各 PR の前後で比較する基準）。

## 3. 計画との差・判断の記録

1. **`TriggerInput.Update` の引数**: 計画書の記載順と異なり `(current, previous, exposed, mouse, fields)`。前フレームの表を遅延展開するのに必要な値を渡すため。
2. **`ComboTriggerInputAction` にも設定サービスを注入**: デバウンス（`DebouncingMs[device]`）が遅延の基準時刻の更新に必要なため。
3. **`InvertOnRelease` の挙動差（意図した差）**: 現行は押下フレームで成立として扱い、実行側が `firstTouch` で「準備」に落とす。新型は押下フレームを未成立として返し、離した 1 フレームだけ成立にする。結果（離したとき 1 回実行）は同じ。Step4 のエンジンは、この「準備中は未成立」を前提にできる。ただし Fresh-press ガードの更新［未成立の連続時間］は、準備中は押し続けても「成立中」と見えない点に注意（Step7.3 の Macro の PR で再確認する）。
4. **`KeyActionBinding` の構築を変更**: 出力を内部で生成する `populateOutputs` ctor を廃止し、`KeyActionBinding(sa, output)` で受ける形に。旧 ctor の呼び出し元は 0 件だった。Step5 で `TriggerBinding` に置き換えて削除予定（TODO を更新済み）。
5. **ネイティブキー（Q7）**: `OutputKBMMapping` が未設定（null）のときは論理キーのまま（`SyntheticDispatcherService.ResolveNativeKey` と同じ規則）。テストのホストでは null のため、テスト側で一時的に `SendInputMapping` を設定している。
6. **失敗とその原因（テストの書き方）**: ①定数 `-1` の列挙型への変換（`unchecked` で解決）、②`IVirtualKBM` の using 不足、③ホストの `OutputKBMMapping` が null、④計測内側で `Stopwatch` を作り 40 バイトを数えた。いずれも本体のコードの不具合ではない。

## 4. 完了条件の確認

- [x] W1〜W5 のビルド・テストが成功（ラチェットの一致検査、1 ファイル 1 型のガードも成功）。
- [x] 新しい型は `Mapping`・DI のどちらにも配線されていない。
- [x] `KeyInputAction`、`ITrigger`／`SingleButtonTrigger` を削除した。
- [x] 本報告書。

## 5. 残課題・次の Step への引き継ぎ

- **Step4**: `ITriggerDispatchEngine`（`ProcessTriggers`／`ApplyFrameOutputs`）、`ButtonOutputAction`、`IFrameOutput`、`Mapping` への空配線、抑制の集合の所有をエンジンへ。`TriggerInput` を `Mapping` がデバイスごとに保持して毎フレーム `Update` する（`PopulateFieldMapping` 済みの表を渡す）。
- 上記 §3-3（準備中は未成立）と Fresh-press ガードの関係を、Step4 の設計で確認する。
- `ActionControllerTests` ほかテスト 3 ファイルは、W1 の契約改訂の際に追従済み（`KeyActionBinding(sa)` の単引数 ctor は残している）。
- 実機確認（W2b-5 ほか）は Step9 に集約のまま。
