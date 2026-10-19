# Phase8-Step5 計画書: 束縛のビルダー（`IActionBindingBuilder`）と、登録簿への `Replace`（移行対象が空のまま）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 の Step5、2026-10-19 追記「Step5 との接続」）、`Phase8-Step2-Design.md` §8・§16・§18・§23、`Phase8-Step4-Completion-Report.md` §5、`Phase8-Step4b-Completion-Report.md` §5  
状態: **完了（W1〜W3 成功、2026-10-19）。完了報告書: `Phase8-Step5-Completion-Report.md`**

---

## 1. 目的と位置づけ

- プロファイルの読込・適用が終わったとき、そのデバイスの**特殊アクション（`ProfileActions`）から `TriggerBinding` のリストを組み立て**、登録簿（`IActionRegistry`）へ**アトミックに差し替える**（`Replace`）。
- 組み立てるのは、`BindingMigrationOptions.MigratedTypes` に含まれる種別だけ。**既定は空なので、本番では常に空のリストが入る**。エンジンの旧ループ内のフックは `Owns` が false のまま素通しで、**本番の挙動は変わらない**（Step4 と同じ「空配線」）。
- Step7 の種別ごとの PR は、「`MigratedTypes` に種別を足す」ことで新経路へ切り替わる。そのとき、ビルダーと差し替えの経路が**すでに本番の読込・適用で動いている**状態にしておく（切替の PR で、初めて動かす部品を減らす）。
- やらないこと: 切断時の `Clear`／`OnDeviceDisconnected`／`ClearState`（**Step6**）、種別ごとの切替（**Step7**）、Controls タブ（`ProcessControlSettingAction`）の取り込み（Step7.8）、`MultiAction` の束縛（Step7.7）。

## 2. 現状の調査（この計画書のための事前確認）

### 2.1 すでにある部品（Step3・4・4b で用意）

| 部品 | 状態 |
|---|---|
| `TriggerBinding`、`BindingPolicy`（`respectsFreshPressGuard`、`blocksWhileExecuting`、`untrigger`、`untriggerButtons`、`suppressesTriggerButtons`、`resetsTriggerOutput`、`stopOnRelease`、`exclusionGroup`） | 実装済み（Step3） |
| `ComboTriggerInputAction(name, triggers, profileSettings, delaySeconds, invertOnRelease)` | 実装済み（Step3） |
| 出力: `KeyOutputAction(sa, profileSettings)`、`ButtonOutputAction(sa, manager)`、`MacroAction(sa, macroPlayer)`、`LaunchProcessAction(sa, launcher)`、`ProfileSwitchAction(sa, switcher)` | 実装済み（Step3・4。Macro／Program／Profile は既存） |
| `IActionRegistry`（`GetBindings`／`Replace`／`Clear`）と `ActionBindingRegistry`、`BindingMigrationOptions`、`ITriggerDispatchEngine`／`TriggerDispatchEngine` | 実装済み・DI 登録済み（Step4）。`Replace` の呼び出し元は 0 件 |
| `ProfileActionsLoadSection`（読込）、`ProfileActionsSaveSection`（保存） | 分離済み（Step4b）。**Step5 のビルダーは読込の節を触らない**（読込済みのメモリ上の設定から作る） |

### 2.2 ビルダーの入力（読込・適用の完了後に揃っているもの）

- `IProfileActionProvider.GetProfileActionsRaw(device)`（名前の一覧）、`GetProfileAction(device, name)`（`SpecialAction`）、`GetProfileActionIndexOf(device, name)`（旧 `Mapping` の復帰の合図が使う添字）。
- `SpecialAction` の関連フィールド: `typeID`、`trigger`（`List<DS4Controls>`）、`delayTime`、`pressRelease`（Macro の RunOnRelease）、`automaticUntrigger`／`uTrigger`（Profile の復帰）、`KeyButtonSwitchMode`／`keyType`（Key・Button の Press／Toggle）。
- 旧ループは `GetProfileActionsRaw` の順序で列挙する。**ビルダーも同じ順序で作る**（フレームの打ち切りで後ろのアクションが飛ぶ順序を保つ。Step2-Design §23.1）。見つからない名前（`GetProfileAction` が null）は飛ばす（旧ループと同じ。警告ログは `EmitMissingActionLogsForDevice` が従来どおり出す）。

### 2.3 呼び出し位置の候補（プロファイルの読込・適用の経路の調査）

プロファイルを読み込む本番の経路（`LoadProfile`／`LoadProfileXml`／`LoadTempProfile` の呼び出し元）は次のとおり。

| 経路 | 通る場所 | `CompleteProfileApplication` を通るか |
|---|---|---|
| UI からの適用、AutoProfile、特殊アクション（Profile）による切替、復帰、接続時の適用 | `ProfileApplicationService.ApplyCore` → `CompleteProfileApplication` | **通る** |
| 名前付きパイプの「一時プロファイルの読込」コマンド | `MainWindow` が `profileRepo.LoadTempProfile(...)` を直接呼ぶ（Halt の中） | **通らない**（`ApplyProfile` を経由しない） |
| 編集画面（`ProfileEditor`）の読込 | 編集スロット `TestProfileIndex`（コントローラーのスロットではない） | 対象外（束縛を作らない） |

- `CompleteProfileApplication` の掃除は、`ClearAllEntries` → `ClearForDevice` → `ClearDeviceState` → `PreallocateForProfileApply` の順で固定されている（`Phase8Step1ApplyProfileCleanupOrderGuardTests`）。
- `ProfileApplicationService`（`IProfileApplicationService`）← `DefaultProfileSwitcher`（`IProfileSwitcher`）の依存があるため、ビルダーが `IProfileSwitcher` を要求し、適用サービスがビルダーを要求すると**循環**する。

### 2.4 Step4 完了時の引き継ぎ

- `OnProfileApplied` を適用に接続すると、**プロファイル適用のたびに抑制の集合が空になる**（現行の本番には、適用時に集合を空にする処理がない）。挙動の変更になりうる（Step4 完了報告書 §5）。
- `Source`（`ActionInstanceState` のキー）が null の束縛は、エンジンが処理しない。ビルダーは必ず `Source` を渡す。

## 3. 作業内容

### 3.1 5-0 ビルダーの契約と実装（W1）

- `IActionBindingBuilder`（`Build(int device)` → `IReadOnlyList<IActionBinding>`）と `ActionBindingBuilder`（`DS4Windows/Actions/`）を新設する。1 ファイル 1 型。
- 依存: `IProfileActionProvider`、`IProfileSettingsService`、`IManagedActionManager`、`IMacroPlayer`、`IProcessLauncher`、`IProfileSwitcher`、`BindingMigrationOptions`。
- 種別ごとの構成（Step2-Design §6）:

| 種別 | 入力 | 出力 | 方針（`BindingPolicy`） |
|---|---|---|---|
| Button | `ComboTriggerInputAction(trigger, delay)` | `ButtonOutputAction` | Press: `respectsFreshPressGuard=false`／Toggle: `true`。`resetsTriggerOutput`（既定 true）。Stage3 の出力は `IFrameOutputAction` が担う |
| Key | 同上 | `KeyOutputAction` | ガード true |
| Macro | `ComboTriggerInputAction(…, invertOnRelease = pressRelease)` | `MacroAction` | ガード true |
| Program | 同上 | `LaunchProcessAction` | ガード true、`blocksWhileExecuting=true` |
| Profile | 同上 | `ProfileSwitchAction` | ガード true、`blocksWhileExecuting=true`、`untrigger`＝Automatic（`automaticUntrigger`）／Buttons（`uTrigger` あり）／None |

- それ以外の種別（`DisconnectBT`、`BatteryCheck`、`MultiAction`、`XboxGameDVR`、`SASteeringWheelEmulationCalibrate`、`GyroCalibrate`、`None`）は、**束縛を作らない**（旧経路が処理する。Step7 の後ろの PR で追加）。
- 設定の取得順: `BindingMigrationOptions` で種別が移行済みか → そうなら作る。**空の間は、空のリスト（共有の静的な空リストではなく、`Array.Empty` 相当）を返す**。
- 各束縛の `Index` には `GetProfileActionIndexOf` の値、`Source` には `SpecialAction` を渡す。

### 3.2 5-1 適用への接続（W2）

- `ServiceRegistration` に `IActionBindingBuilder` → `ActionBindingBuilder`（Singleton）を登録する。
- `ProfileApplicationService.CompleteProfileApplication` の**掃除（`PreallocateForProfileApply`）と Toggle／Press の確認ログの後**に、`registry.Replace(device, builder.Build(device))` を呼ぶ。範囲外のデバイス（`ControllerSlotLimit` 以上）では呼ばない。
- 例外は握って `[DI]` 付きのログを出し、**そのデバイスの束縛は空のリストにして**適用を続ける（適用の失敗にしない。Q5）。
- パイプの「一時プロファイルの読込」経路には、`LoadTempProfile` の後に同じ差し替えを行う入口（`IProfileApplicationService` の小さなメソッド）を足し、`MainWindow` から呼ぶ（Q6）。
- 循環依存は `ProfileApplicationService` が `Func<IActionBindingBuilder>` を受け取って解く（Q3）。

### 3.3 5-2 検証（W3）

- 適用 → 登録簿の確認（`MigratedTypes` が空なら空、種別を足すと該当の束縛が入る）、読込の失敗では差し替えない、適用を繰り返すたびに新しいリストへ置き換わる（古い参照は読み取り中でも壊れない）。
- ホストが循環せずに解決できること（`Phase7Step8W2MappingInstanceGuardTests` と同様の検査）。
- 既存の全テストが期待値を変えずに成功すること。ラチェットの一致検査（新しいファイルに `Global.*`／`AppHost`／`Program.rootHub` を増やさない）。

### 3.4 5-3 まとめ（W4）

- 完了報告書、`Phase8-Status.md` の更新、モデル図の更新案の追記（変更はユーザーの指示があるまで行わない）。

## 4. テスト方針

- **ビルダー単体**（`SpecialActionTestScope` を流用）: 種別ごとに、入力のボタン・遅延・`invertOnRelease`、出力の型、`BindingPolicy` の各値（上の表）、`Index`、`Source`。順序（`ProfileActions` の順）、見つからない名前の読み飛ばし、移行対象が空／一部のときのフィルタ、未対応の種別を作らないこと。
- **適用との結合**: `ProfileApplicationService` に偽のビルダー・登録簿を渡して、呼び出しの順序（掃除の後）と回数、読込の失敗時に呼ばないこと、例外時に空へ差し替えること、範囲外のデバイスで呼ばないこと。
- **本番の経路で動くこと**: 実物のホスト（`TestHost`）で、プロファイルを適用したあと、`MigratedTypes` が空なら `GetBindings` が空、足すと束縛が入ること。
- **ホットパス**: ビルダーと `Replace` は適用時の 1 回だけで、毎フレームの処理には入らない（`Phase8Step4MapCustomBenchmarkTests` の割り当て 0・オーバーヘッドの結果が変わらないことを再確認）。

## 5. Wave 構成

- W1: 5-0（`IActionBindingBuilder`／`ActionBindingBuilder`、ビルダー単体テスト）。
- W2: 5-1（DI 登録、`CompleteProfileApplication` への接続、パイプ経路の入口）。
- W3: 5-2（結合テスト、ホストの解決、既存テストの確認）。
- W4: まとめ（完了報告書、Status）。

## 6. 質問（推奨は A）

- **Q1（呼び出しの位置）**: A（推奨）: `CompleteProfileApplication` の掃除の後（Step2-Design §8 のとおり）。適用の経路（UI、AutoProfile、特殊アクション、復帰、接続）がすべて通る。B: `LegacyProfileXmlSerializer.LoadProfile` の末尾。すべての読込を通るが、永続化の層が第2層のアクションの登録簿を知ることになり、依存の向き（規約 §1）に反する。
- **Q2（作る種別の範囲）**: A（推奨）: Key／Button／Macro／Program／Profile の 5 種を Step5 で作り、テストで構成を固定する（本番は `MigratedTypes` が空なので出番はない）。Step7 の PR は、スイッチを入れ、特性化テストを新旧で走らせることに集中できる。B: 空のビルダーだけを作り、種別は Step7 の各 PR で足す（Step5 で動かす部品が少ないが、Step7 の各 PR が大きくなる）。
- **Q3（循環依存の解消）**: A（推奨）: `ProfileApplicationService` が `Func<IActionBindingBuilder>` を受ける（`Func<ControlService>` と同じ遅延の作法）。B: `ProfileSwitchAction` 側に `Func<IProfileSwitcher>` を渡す（既存のアクションと Adapter のコンストラクターを変える範囲が広い）。
- **Q4（`OnProfileApplied` の接続）**: A（推奨）: Step5 では**接続しない**（`Replace` だけ）。登録簿の `Replace` でエンジンの束縛ごとの実行時配列は作り直される。抑制の集合の扱い（適用時に空にするか）は、最初の種別の切替（Step7.1）で、特性化テストとあわせて決める。B: Step5 で接続し、抑制の集合の消去を含めて受け入れる（本番の挙動が、適用の瞬間にボタンを押していた場合だけ変わる可能性がある）。
- **Q5（ビルドの失敗時）**: A（推奨）: 例外を握り、`[DI]` 付きのログを出して、そのデバイスは空のリストにする（プロファイルの適用は成功のまま）。B: 例外を呼び出し元へ伝える（適用の失敗になり、切替が止まる）。
- **Q6（パイプの「一時プロファイルの読込」経路）**: A（推奨）: Step5 で差し替えの入口を足し、`MainWindow` から呼ぶ。Step7 で空でなくなったときに、この経路だけ古い束縛が残る事故を防ぐ。B: 触らず、Step7.1 の前に対応する（Step5 は小さいが、忘れる危険がある）。
- **Q7（編集スロット・範囲外のデバイス）**: A（推奨）: `ControllerSlotLimit` 以上のデバイス（編集スロット `TestProfileIndex` を含む）では何もしない。B: 作って登録簿に入れる（登録簿の配列の大きさに依存し、意味のない束縛が溜まる）。
- **Q8（テストの粒度）**: A（推奨）: ビルダー単体、適用との結合（偽の協力者）、実ホストでの確認、ホットパスの再確認の 4 本立て。B: ビルダー単体と実ホストのみ。

## 7. リスクと対応

- **適用時の競合**: `Replace` は適用のスレッド（UI、`Task.Run`、AutoProfile のタイマー）から呼ばれる。参照の差し替えはアトミックで、入力スレッドはフレームの先頭で 1 回だけ参照を取る（Step4 で検証済み）。`ApplyFromAction`／`ApplyProfileWithoutHalt` の Halt の区間内で呼ぶため、位置は現行の適用と同じ。
- **掃除の順序の崩れ**: `Phase8Step1ApplyProfileCleanupOrderGuardTests` が固定している順序を変えない。差し替えは**最後**に足す。
- **ホストの循環**: Q3 の `Func` で解く。ホストの解決テストで常時確認する。
- **未使用コードの扱い（規約 2.4）**: 空の間は呼ばれる結果が常に空になる。ビルダーの各種別の構成には、接続予定の Step（7.x）を明記した TODO を付ける。
- **`SpecialAction` の共有**: 束縛は `SpecialAction`（可変のオブジェクト）を `Source` として持つ。編集画面で保存し、再適用するたびに作り直される（Phase6 の再適用の経路）。束縛を長く持ち回らない。

## 8. 完了条件

- [x] W1〜W4 のビルド・テストが成功する（ユーザーが実行）。既存の全テストが期待値を変えずに成功。ラチェットの一致検査、1 ファイル 1 型のガードも成功。
- [x] プロファイルを適用すると、`MigratedTypes` が空のとき `GetBindings` は空、足すと該当の束縛が入る（実ホストで確認）。
- [x] 掃除の順序の検査が不変。パイプ経路も同じ差し替えを通る。
- [x] 本番の挙動が変わらない（`MigratedTypes` が空）。ベンチマークの割り当て 0 が不変。
- [x] 完了報告書、`Phase8-Status.md` の更新。

## 9. 次の Step

- Phase8-Step6（切断時の `Clear`／`OnDeviceDisconnected`、`ClearState`／`ClearPendingRestore` の意味の調査と接続、接続時の紐付けの要否の確定）。計画書は、Step5 の完了後に作成する。
