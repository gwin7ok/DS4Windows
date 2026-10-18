# Phase8-Step4 計画書: エンジンと出力の型、`Mapping` への空配線

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 Step4）、`Phase8-Step2-Design.md`（§5、§9、§14〜§17、§22〜§23）、`Phase8-Step3-Completion-Report.md` §5  
状態: **実施中（Q1〜Q8 を推奨どおり承認、2026-10-19）。W1 実装済み［ビルド・テスト待ち］**

---

## 1. 目的と位置づけ

- Step3 で作った判定側（`TriggerInput`、`ComboTriggerInputAction`）と束縛（`TriggerBinding`）を使って動く、**エンジンと出力側の型**を実装し、`Mapping` に**空の状態で配線**する。**本番の挙動は変えない**（移行対象の種別が空で、エンジンは何も処理しない）。
- 作るもの: `IFrameOutput` とその実装、`IFrameOutputAction`／`IHeldOutputAction`、`IOutputContext` の拡張、`ButtonOutputAction`、`IActionRegistry` の改訂と実装、`BindingMigrationOptions`、`ITriggerDispatchEngine`／`TriggerDispatchEngine`（排他グループ［シフト］を含む）。
- 配線するもの: `Mapping` へのエンジンの注入、**抑制の集合の所有をエンジンへ一本化**、**`nowAction`／`oldnowAction` を `TriggerInput.Delay` に置き換え**、旧ループへの「`Owns` ならエンジンに任せる」フックと、Stage3 の後の `ApplyFrameOutputs` 呼び出し。
- 範囲外:
  - 束縛の組み立て（`IActionBindingBuilder`、読込・適用の完了後の `Replace`）は **Step5**。切断時の `Clear`／`OnDeviceDisconnected`、`ClearState`／`ClearPendingRestore` の接続は **Step6**。つまり Step4 の `OnProfileApplied`／`OnDeviceDisconnected` は実装とテストのみで、本番からは呼ばない（規約 2.4: TODO を付ける）。
  - **復帰の合図**（`UntriggerAction`／`UntriggerIndex`）と Profile の復帰の処理は、`Mapping.deviceRuntime` に残す（設計書 §23.3。Step7.5 でまとめて移す）。Step4 のエンジンは `UntriggerPolicy.None` の束縛だけを扱う（Automatic／Buttons は、型として持つが処理は Step7.5）。
  - 種別ごとの出力アクションの新設（DisconnectBT ほか）は Step7。
- 配置と規約: `DS4Windows/Actions/`（R3）、名前空間 `DS4Windows.Actions`、1 ファイル 1 型、コンストラクター注入、静的な窓口を新設しない。ラチェットの基準値は変わらない見込み（新設の型は静的参照を持たない。`Mapping` から `suppressedTriggerButtons` などを削除するだけ）。

## 2. 作業内容

### 2.1 契約・値型・登録簿（W1）
- `IFrameOutput`（`ResetTriggerControl`、`PressX360`）、`IFrameOutputAction : IOutputAction`（`ApplyFrame`）、`IHeldOutputAction : IOutputAction`（`WhileHeld`）。
- `IOutputContext` に `Frame`、`RequestEndFrame()` を追加（設計書 §5）。`OutputContextImpl` と、テストの `MockOutputContext` 2 件、`KeyOutputActionTests.Context` を追従させる（既存の出力アクション 3 つは変更しない。`ProfileSwitchAction`／`DisconnectBT` が `RequestEndFrame()` を呼ぶのは Step7.5）。
- `IActionRegistry` の改訂（`GetBindings`／`Replace`／`Clear`。呼び出し元 0 件）と、実装 `ActionBindingRegistry`（`volatile` な参照の配列。要素数＝`ControllerLimits.MaxControllerCount`）。DI 登録（Singleton）。既存の静的クラス `ActionRegistry`（`SpecialAction` の一覧）とは別物なので、名前を分ける。
- `BindingMigrationOptions`（`MigratedTypes`。既定は**空**）。DI 登録（Singleton）。
- `ITriggerDispatchEngine` の契約（設計書 §9／§14-1 の入口 2 つ＋旧ループ用の `Owns`／`ProcessOwned`／`AddSuppressed`／`ReleaseSuppressed`／`IsSuppressed`、`OnProfileApplied`／`OnDeviceDisconnected`）。
- 簡易ベンチマーク（`MapCustom` を固定の入力で N 回呼び、時間と割り当てを出力）を、**配線の前（W1）に**用意する。ユーザーが配線前の基準値を記録できるようにするため。

### 2.2 `FrameOutput` と `ButtonOutputAction`（W2）
- `IFrameOutput` の実装（デバイスごとに再利用。`outputfieldMapping`・`MappedState`・`deviceState.currentClicks` を差し替えて使う。割り当て 0）。現行の Stage3 の `X360Controls` の分岐（TouchpadClick／マウス系のカウンター／ボタン配列）と、`ResetToDefaultValue` 相当をここに移す。
- `ButtonOutputAction : IFrameOutputAction`: `ButtonSpecialActionOutput.ResolveOutputActive` と同じ規則（Press は成立中のみ、Toggle は立ち上がりで反転。Fresh-press ガード中は反転しない）。
- 現行の Stage3 と**同じ出力**になることを、全 `X360Controls` で比較する。

### 2.3 `TriggerDispatchEngine`（W3）
- 設計書 §15.2 の 1 束縛の処理（Fresh-press のタイマー更新、抑制への追加、トリガーのボタンの出力の消去、排他グループ、Fresh-press／実行中のガード、立ち上がりで `Execute`、保持中は `WhileHeld`、離したエッジで `Stop`、例外は束縛ごとに `try/catch`［現行のログ文言を維持］、`RequestEndFrame` での打ち切り）。種別の分岐は持たず、`BindingPolicy` と出力アクションで表す。
- `ProcessOwned`（旧ループのフック用）、`ProcessTriggers`（最終形）、`ApplyFrameOutputs`（同じフレームの評価結果を再利用）、`IsSuppressed`／`AddSuppressed`／`ReleaseSuppressed`（離されたボタンの解除を含む）、`OnProfileApplied`（適用スレッドからは**「次のフレームの先頭でリセット」の要求フラグ**を立てるだけ。§17）、`OnDeviceDisconnected`。
- 状態は `ActionInstanceState`（`IManagedActionManager`）に残す。エンジンが持つのは、デバイスごとの抑制の集合と、束縛ごとの実行時の配列（`PrevEstablished`。リストの参照が変わったときだけ作り直す）。

### 2.4 `Mapping` への配線（W4）
- `Mapping` のコンストラクターに `ITriggerDispatchEngine` を追加（11→12）。`ServiceRegistration`、`TestMappingFactory` ほかを追従。
- **抑制の集合**: `Mapping.suppressedTriggerButtons` を削除し、`MapCustomAction` の冒頭の解除パス・成立時の追加・`CheckForSpecialActionSuppression` を、エンジンの `ReleaseSuppressed`／`AddSuppressed`／`IsSuppressed` の呼び出しに置き換える（挙動は同じ）。テスト（`MappingSpecialActionSuppressionTests`、`Phase7cW2...`、`SpecialActionTestScope`、`Phase8Step1SlotLimitTests` の許可リスト）を追従。
- **遅延の基準時刻**: `Mapping` が `TriggerInput[]`（デバイスごと）を持ち、`MapCustom` の先頭で `Update` する。旧ループの `nowAction`／`oldnowAction`（`MapCustomAction` と `IsSpecialActionTriggered`）を `input.Delay` の `Now`／`Old` に置き換え、配列 2 本を削除（`Phase8Step1SlotLimitTests` を追従）。
- **フック**: 旧ループの各アクションの先頭に `if (_engine.Owns(device, action)) { … continue; }`（設計書 §23.1）、Stage3 のループにも同じフック。Stage3 の後に `engine.ApplyFrameOutputs(...)` を呼ぶ。`Owns` は、`MigratedTypes` が空の間は常に false。
- 旧ループ以外（`MapCustom` の通常の割り当て）の呼び出し順序は変えない。

### 2.5 検証とまとめ（W5〜W6）
- 配線後のベンチマークの比較（許容: 現行の +5% 以内）。フックが効くことの結合テスト（§3）。完了報告書、`Phase8-Status.md` の更新（W6）。

## 3. テスト

- **エンジンの単体テスト**（偽の `IInputAction`／`IOutputAction`、実物の `DefaultActionManager`、時刻の注入）: 立ち上がりで 1 回だけ `Execute`、保持中は `WhileHeld`、離したエッジで `Stop`、Fresh-press ガード（80ms の連続した未成立で解除）、`BlocksWhileExecuting`、`ResetsTriggerOutput`、抑制の追加・解除（一部のボタンだけ離した場合を含む）、排他グループ（宣言順で先に成立した束縛だけ）、`RequestEndFrame` で後ろの束縛が処理されない、例外が他の束縛に波及しない、`OnProfileApplied` の要求フラグ（次のフレームの先頭でリセット）、`OnDeviceDisconnected`、束縛が空のときに何もしない。
- **`ButtonOutputAction`／`FrameOutput`**: 全 `X360Controls` で、現行の Stage3 と同じ出力（ボタン配列、マウスのカウンター、タッチパッドクリック、Press／Toggle、ガード中の Toggle）。
- **`ActionBindingRegistry`**: 空のリスト（null を返さない）、`Replace` のアトミック性（読み取り中の差し替えで古い一覧が壊れない）、`Clear`、範囲外のデバイス。
- **配線**: 既存の全テスト（特性化テスト 38 件ほか）が**変わらず成功**すること。`Owns` が false のとき旧ループの挙動が不変であること。テストでだけ `MigratedTypes` に種別を入れ、偽の束縛を登録して、旧ループが該当アクションを**通らず**、エンジンの出力アクションが 1 回だけ呼ばれること（二重ディスパッチの検出。設計書 §23.4）。`Delay` の置き換え後も、`delayTime` の既存のテスト（`Phase8Step1SlotLimitTests`）が成功すること。
- **性能**: 割り当て 0 のテスト（エンジンの 1 フレーム、`ProcessOwned`、`ApplyFrameOutputs`）。`MapCustom` のベンチマークは配線の前後で比較（出力のみ。Q5＝A と同じ扱い）。
- ラチェットの一致検査、1 ファイル 1 型のガード、`MappingHotPathAllocationTests` が成功すること。

## 4. Wave 構成（案）

- W1: 契約・値型・登録簿・`BindingMigrationOptions`・エンジンの契約、`IOutputContext` の拡張と追従、ベンチマークの用意（**ここで基準値を記録**）。
- W2: `FrameOutput`、`ButtonOutputAction` ＋ Stage3 との一致テスト。
- W3: `TriggerDispatchEngine` ＋ 単体テスト。
- W4: `Mapping` への配線（注入、抑制の一本化、`Delay` の置き換え、フック、`ApplyFrameOutputs`）＋ 結合テスト。
- W5: ベンチマークの比較、割り当て 0 のテスト、全テストの確認。
- W6: 完了報告書、`Phase8-Status.md` の更新。

## 5. 決定してほしい点（質問と推奨）

- **Q1（`IActionRegistry` の改訂を Step4 で行うか）**: A（推奨）: 行う（`GetBindings`／`Replace`／`Clear` と `ActionBindingRegistry`）。エンジンが束縛を読むのに必要で、旧インターフェースの呼び出し元は 0 件。Step5 は「ビルダーと `Replace` の呼び出し」だけになる。B: エンジンに束縛の一覧を引数で渡し、登録簿は Step5。理由: A は設計書 §22 の Step5 の項目を前倒しするが、小さく、テストしやすい。
- **Q2（`IOutputContext` の拡張の仕方）**: A（推奨）: 設計どおり `IOutputContext` に `Frame`／`RequestEndFrame()` を足し、実装 1 件とテストの偽物 3 件を追従させる。B: 別の継承インターフェース（`IFrameOutputContext`）にして既存を触らない。理由: 設計書 §13-3 で承認済み。B は既存を変えないが、出力アクションが毎回型判定を要する。
- **Q3（`nowAction`／`oldnowAction` の置き換えを Step4 で行うか）**: A（推奨）: 設計書 §23.3 のとおり Step4 で行い、旧ループも `input.Delay` を使う（配列 2 本を削除）。B: 最初に `delayTime` を扱う種別の PR（Step7.1／7.2）まで延期。理由: A は旧ループの変更が増えるが、新旧のどちらが処理しても基準時刻が食い違わない。変更前後の一致は、既存の `delayTime` のテストとベンチマークで確認する。
- **Q4（抑制の集合の所有）**: A（推奨）: エンジンが所有し、`Mapping.suppressedTriggerButtons`（public フィールド）を削除する。テスト 4 件を追従。B: `Mapping` に転送プロパティを残してテストを変えない。理由: A は新旧が別々の集合を持つ事故を構造的に防ぐ。B は差分が小さいが、公開面が残る。
- **Q5（`BindingMigrationOptions` を Step4 で導入するか）**: A（推奨）: 導入する（空の既定）。`Owns` の判定に必要。ビルダー（Step5）も同じ設定を使う。B: Step5 で導入し、Step4 の `Owns` は常に false にする。
- **Q6（排他グループ［シフト］を Step4 で実装するか）**: A（推奨）: 実装してテストする（設計書 §6・§15.2）。Step7.8（Controls タブ）まで使われないが、エンジンの契約に含めておくため。B: Step7.8 まで延期。
- **Q7（`Mapping` のコンストラクター引数の増加）**: A（推奨）: 11→12 を受け入れる（台帳 §17.4 の整理は Phase9）。B: 引数をまとめた型を作る（Phase9 の範囲を先取りするため非推奨）。
- **Q8（エンジンのスレッドの安全）**: A（推奨）: 旧ループのフック（`MapCustomAction` は `async void` で、`await EnsureActionDoneInitialized()` の後の継続が別のスレッドで走りうる）に備え、`ProcessOwned`／`ApplyFrameOutputs`／`ReleaseSuppressed` を**デバイスごとのロック**で囲む（競合のない通常時のコストは数十 ns）。B: ロックなし（設計書 §17 の「入力スレッドのみ」の前提を信頼）。理由: 現行の継続のスレッドは未検証で、ロックなしだと、まれなフレームの重なりで状態が壊れる恐れがある。

## 6. リスクと対応

- **配線で旧経路の挙動が変わる**（フック、`Delay` の置き換え、抑制の移管）: `Owns` は空の間 false。既存の全テストと、配線の前後のベンチマーク、`delayTime` のテストで確認。差分は小さく分け、W4 を細かい手順で進める。
- **`OnProfileApplied` を呼ぶと挙動が変わる**: 現行では、プロファイル適用時に抑制の集合を空にする処理が本番にない（テストの後始末のみ）。Step5 で `OnProfileApplied` を適用に接続するときは、「抑制の集合を空にする」ことが挙動の変更になりうるため、そのときの計画書で是非を判断する（Step4 では接続しない）。
- **`async void` ＋ `await` の継続のスレッド**: Q8 のロックで対応。`MapCustomAction` の `async void` 自体の廃止は、最終形（Step7 の全種別の切替後。Step8）。
- **`IOutputContext` の拡張による型の追従漏れ**: ビルドで検出（実装 1 件、テストの偽物 3 件）。
- **未接続のメンバー（`OnProfileApplied`、`OnDeviceDisconnected`、`ProcessTriggers`、`Replace`）**: 規約 2.4 に従い、接続予定の Step（5・6・7）を書いた TODO コメントを付け、`Phase7Step10eW4OrphanMembersGuardTests` に準じたガードに追加するかを W1 で判断する。

## 7. 完了条件

- [ ] W1〜W5 のビルド・テストが成功する（ユーザーが実行）。ラチェットの一致検査、1 ファイル 1 型のガードも成功する。
- [ ] 既存の全テストが、期待値を変えずに成功する（本番の挙動が変わっていない）。
- [ ] `Mapping` の `suppressedTriggerButtons`、`nowAction`、`oldnowAction` が削除され、エンジンと `input.Delay` に置き換わっている。
- [ ] 配線の前後のベンチマークの比較結果が記録されている（許容: +5% 以内）。
- [ ] 完了報告書、`Phase8-Status.md` の更新。モデル図の更新が必要な箇所は、案として完了報告書に記載する（図の変更は、ユーザーの指示があるまで行わない）。

## 8. 次の Step

- Phase8-Step4b（`LoadProfile`／`SaveProfile` の分解ほか。計画書は Step4 の完了後に作成）、その後 Step5（ビルダーと `Replace` の呼び出し）。
