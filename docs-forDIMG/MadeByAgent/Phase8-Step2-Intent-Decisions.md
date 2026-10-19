# Phase8-Step2 W1: 仕様台帳「意図が不明な挙動」の意図決定資料

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2-Plan.md` Q4・W1、`Phase8-Step1-Trigger-Spec-Ledger.md` §8  
状態: **決定案（承認待ち）**。製品コードは変更していない  
方針: **Phase8 では挙動を変えない**（Q4）。各件を「維持」「揃える（挙動が同じになるもの）」「移植しない（到達不能など）」「別途是正（承認が要る）」に分類し、旧実装との比較を添える。

## 0. 比較の方法

- 「上流」は、フォークの最初のコミット（`4319f619`、2025-11-05）の直前の `Mapping.cs`（`4319f619^`）。以降の変更は、フォーク独自（DI 移行前の機能追加、または DI 移行）。
- 該当コードの導入は `git log -S` で特定した。上流のコードとの突き合わせは、同じ構造の有無（行を読んだ結果）に基づく。実機での確認ではない。

## 1. 一覧と決定案

### 1. Stage3 の Button の Press が Fresh-press ガードを見ない（Toggle の反転だけが見る）
- 導入: Button 種別はフォーク独自（2025-12-14、`5513f936`）。出力の一本化は Phase6-Step10-3d（`a865ed93`）、ガードの全型適用は Step10-4-2e（`3ba7b6c9`）。
- 実態: Press は「トリガー成立中のみ出力」で、エッジで 1 回実行する処理がない。プロファイル適用直後の押しっぱなしでも、「その間ボタンが押されている」だけで、誤った実行（連鎖発火）が起きない。ガードが要るのは、エッジで反転する Toggle だけ。
- **決定案: 維持**。新エンジンでは、`TriggerBinding` の設定「Fresh-press ガードを見るか」を、Button の Press は `false`、Toggle は `true` とする（挙動は同じ）。

### 2. 判定ロジックの複製（`IsSpecialActionTriggered` と `MapCustomAction`）
- 導入: `IsSpecialActionTriggered` はフォーク独自（2025-12-15、`b5af5fcc`。Button の同期処理用に `MapCustomAction` から複製）。
- 違い: (a) `delayTime` の分岐で `nowAction`／`oldnowAction` を更新しない（`MapCustomAction` は更新する）。(b) Key の `uTrigger` を見る。(c) タップ判定の分岐がない。
- 実際の影響: 利用者は Stage3 の Button だけ。(a) は、同じフレームで `MapCustomAction` が先に `nowAction` を更新するため、結果は同じ。(b)(c) は Button に関係しない（Key のみ、到達不能）。
- **決定案: 揃える（挙動は同じ）**。新エンジンの `ComboTriggerInputAction`（D2）は、`MapCustomAction` の判定（`delayTime` の基準時刻の更新、`pressRelease` の反転を含む）を正とした 1 つの実装にする。Button も同じ判定を使う。

### 3. タップ判定の分岐（`triggerToBeTapped`）が到達不能
- 導入: 上流の 2015-07-30（Version 1.4.266、`86079b02`）から。外側の条件 `!(action.typeID == None || index < 0)` が `typeID == None` を除外するため、`typeID == None` を要するタップ判定の分岐は、上流の元から到達しない（Key が種別になる前の名残とみられる）。
- **決定案: 移植しない**（到達不能。機能の削除ではない）。旧コードの `MapCustomAction` と一緒に、Step8 で消える。

### 4. Key の `uTrigger` を無視する（`uTriggerCount = 0`）。UI との不整合
- 導入: フォーク独自（2025-12-15、`8115105d`。「トグルのスペシャルアクションは、トリガーキー 1 つだけで ON／OFF する」）。上流では、Key の `uTrigger`（解除キー）を使って、押し続けたキーを解除キーで離していた。
- **不整合**: 実行側は Key の `uTrigger` を無視するが、設定 UI の `PressKeyViewModel` は、Toggle で `ucontrols` が空だと `UnloadError`（「解除トリガーが指定されていない」）を出す。コミットのメッセージは「設定 UI や実行コードの不要な部分」とあるが、変更は `Mapping.cs` だけ。UI の解除トリガーの設定は、Key では何も起こさない。
- **決定案: 実行側は維持**（Phase8 では挙動を変えない。`uTrigger` を使う分岐は移植しない）。UI の不整合は、**別件として記録し、是正には承認を得る**（案: UI から解除トリガーの項目を外す、または検証を外す）。Step2 では扱わない。

### 5. Profile／DisconnectBT の実行後の `return`（フレームの残りを飛ばす）
- 導入: **上流の元から**。Profile の `return`（`Task.Run` の後）、DisconnectBT の `return`（`DisconnectBT()` の後）。
- 意図: プロファイルの適用や切断で、アクションの一覧・状態が置き換わるため、同じフレームで古い一覧を使い続けない。
- **決定案: 維持**。エンジンは、Profile の切替・DisconnectBT を実行したら、そのフレームの走査を打ち切る（「フレーム終了」を出力側から返せる契約にする）。復帰の判定（`UntriggerAction`）も、その場合はそのフレームでは行わない（現行どおり）。

### 6. `UntriggerAction` が未成立の間の `else`（`DispatchOrSetBeingTriggered(false)`）
- **ledger の記述を訂正する**: 台帳 §3・§8 項目 6 は「Profile 以外のとき通らない」と読めたが、実際は、`utriggeractivated && Profile` が**成立していないとき**（復帰条件を待っている間）に毎フレーム通る。上流は同じ位置で `actionDone[index].dev[device] = false` としていた（フォークの `BeingTriggered` に相当）。
- 意味: 復帰条件が満たされるまで、元のプロファイル切替アクションの「実行済み」フラグを下げ続ける。トリガーと解除トリガーが同じボタンのとき（`controls == ucontrols`）の再成立の判定（`!GetBeingTriggered`）に必要。
- **決定案: 維持**。エンジンの復帰ポリシー（Q5）の実装で、復帰待ちの間、切替アクションの `BeingTriggered` を下げる。台帳 §8 項目 6 を訂正する。

### 7. XboxGameDVR／MultiAction が `ActionManager` を通らず、`PlayMacro` を直接呼ぶ
- 導入: **上流の元から**（2017-04-01、`54ffcf02`）。`BeingTriggered` を使わず、`SpecialAction` のフィールド（`tappedOnce`／`firstTouch`／`secondtouchbegin`／`pastTime` など）にタップ状態を持つ。
- **決定案: 維持（最後に移行）**。タップ（150ms＋`DebouncingMs`）・長押し（500ms）・ダブルタップを判定する別の入力アクション（仮称 `MultiTapInputAction`）とし、出力は既存のマクロ出力（`IMacroPlayer`）を使う。実機の `DS4Device`（前フレームの状態）を要するため、Step7 の最後。時間定数はそのまま。

### 8. Macro／Program／Profile／Key の「`handled == false` のフォールバック」（直接実行）
- 導入: フォーク（DI 移行。`c92a3691` 2026-08-27 ほか）。`DispatchInputEdge` が `false` を返したとき（DI 未登録、**またはエッジがない**とき）、`SetBeingTriggeredIf` と直接実行（`PlayMacro`、`LaunchProcessAction`、`ApplyProfile`）に落ちる。
- **注意（推定。未確認）**: `DispatchInputEdge` は `prevInputEstablished`（アクション名のハッシュ＋デバイスをキーとする、前回の入力状態のキャッシュ）でエッジを見る。プロファイル適用でアクションの状態（`BeingTriggered`）が作り直されても、このキャッシュは消えないため、次の成立で「エッジなし」と判定され、`handled == false` のフォールバックで実行される可能性がある。つまり、フォールバックが**実際に使われる経路**になりうる。
- **決定案: 新エンジンでは、フォールバックを持たない**。エッジ検出は、エンジンが持つ状態（`ActionInstanceState.BeingTriggered` と前回の成立状態）だけで行い、`prevInputEstablished` のキャッシュは使わない（プロファイル適用・切断で、状態と同時に作り直す）。この「キャッシュが残る」挙動の有無は、Step3 の単体テスト（適用後の再成立）で確認する。

### 9. 時間定数が別々
- 80ms（Fresh-press ガードの解除、`FreshPressReleaseHoldMs`）、250ms＋`DebouncingMs`（タップ判定。到達不能）、150ms／500ms＋`DebouncingMs`（MultiAction）、100ms（`delayTime` の基準時刻の更新）。
- **決定案: 維持**。値は変えず、名前付き定数として 1 か所に集める（新設のクラス）。挙動は同じ。

### 10. `nowAction`／`oldnowAction` が 4 要素
- **是正済み**（W2b。上流の 2015 年から。8 スロット化の更新漏れ）。対応不要。

### 11. Profile の「離したら復帰」が、適用による状態の掃除に暗黙に依存
- 導入: フォーク（2025-12-18 の `ClearDeviceState` の追加以降。Phase7b W1 で復帰の合図の退避・復元を追加）。上流は `ClearDeviceState` を呼ばず、`untriggeraction` で明示的に復帰していた（離したときの `OnRelease` が復帰する構造ではない）。
- **決定案: 明示的な仕様にする**（Q5）。復帰のポリシー（なし／自動／解除ボタン）を `TriggerBinding` の設定にし、離したときの `OnRelease` では復帰しない。見た目の挙動は現行の本番と同じ。

### 12. 切替後の連鎖処理（`uTrigger.Count == 0 && !automaticUntrigger`）
- 導入: 上流（同じ `controls` を持つ次のアクションを `actionDone = true` にして、離すまで待たせる）。フォークは `DispatchOrSetBeingTriggered(true)` に置き換えたが、`ActionManager` が処理する通常の経路では到達しない（台帳 §5 Profile、Step1 のテスト）。
- 実態: 適用後の状態の作り直しで `RequiresFreshPressAfterReset = true` になるため、80ms 離すまで実行されない（Fresh-press ガードが同じ役割を担う）。
- **決定案: 移植しない**（Fresh-press ガードが代替。到達不能なフォールバック内のため）。旧コードと一緒に Step8 で消える。

## 2. 決定案のまとめ

| 分類 | 項目 |
|---|---|
| 維持（挙動は同じ） | 1、5、6、7、9 |
| 揃える（挙動は同じ） | 2 |
| 移植しない（到達不能・代替あり） | 3、12 |
| 明示的な仕様にする（見た目は同じ） | 11 |
| 設計上の注意（新エンジンの要件） | 8 |
| 是正済み | 10 |
| **別件として記録（UI との不整合。是正には承認が要る）** | 4 |

- **挙動を変える項目はない**（Phase8 で変えない方針を満たす）。承認が要るのは、4 の UI の是正だけ（Phase8 の範囲外）。

## 3. 確認したい点

1. 上の決定案（特に 1「維持」、3・12「移植しない」、8「フォールバックを持たない」）でよいか。
2. 4 の UI の不整合（Toggle の Key で解除トリガーを必須とする検証が残っている）は、別件として記録し、Phase8 の後に扱ってよいか。
3. 8 の「`prevInputEstablished` のキャッシュが残る」の推定は、Step3 のテストで確認する。確認の結果、挙動が現行と食い違うと分かったときは、再度ご相談する。

## 4. 機能廃止の記録（Phase8-Step7-3a。2026-10-19。ユーザーの明示的な承認）

規約 2.2 の例外（機能廃止）として、次を廃止した。計画書: `Phase8-Step7-3a-AltTab-Removal-Plan.md`（Q1〜Q4 を推奨どおり承認）。

- **Alt-Tab のマクロの特別扱い**: 手順が `164/9/9/164` か `18/9/9/18` で始まるマクロを、手順どおりに再生せず、`AltTabSwapping`（押している間、一定間隔で Tab を送る）に切り替える仕組み。`AltTabSwapping`、`AltTabSwappingRelease`、`altTab*` のフィールド、`EndMacro` の Alt-Tab の判定、押している間の毎フレームの `EndMacro` の呼び出しを削除した。
- **記録画面の「Cycle Programs」プリセット**: この特別扱いを起動する手順（`18/9/9/18/1300`）を書き込むためだけにあった。メニュー項目、クリックの処理、`WriteCycleProgramsPreset` を削除した。
- **挙動の変化**: Alt-Tab の並びのマクロは、通常のマクロとして再生される（1 回の Alt+Tab）。保存済みのデータは変更しない。「押している間繰り返す」（`RepeatMacro`）で、近い動作にできる。
- 影響: 項目 8 の `Macro` の「押している間の呼び出し（`EndMacro`）」が不要になった（Step2-Design §14.3 の `IHeldOutputAction` の理由が消えた）。
