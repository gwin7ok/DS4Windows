# Phase13 進捗

計画: `Phase13-Plan.md`（承認済み 2026-11-04）。ブランチ: `Phase13`。

| Step | 内容 | 状態 |
|---|---|---|
| 1 | トリガー系クラスの未使用 `profileSettings` 引数の削除 | 完了（`d0729a8c`） |
| 2 | `ControlService` の未使用引数 6 個の削除（34→28） | 完了（`668bdea6`） |
| 3 | 状態を持たないホストの分離（イベントスレッド・振動・KBM 初期化） | 完了（`b8ba2fca`） |
| 4 | `ServiceProviderHolder` の撤廃（静的 `ActionManager` の廃止と注入） | **実装完了（2026-11-04。コミット待ち）。** 静的 `ActionManager`・`ServiceProviderHolder`・`ButtonSpecialActionOutput`（とそのテスト）を削除し、`IManagedActionManager`／`IKeyButtonControllerRegistry` をコンストラクターで注入。Debug ALL: 警告 0・エラー 0、Actions.Tests 3054 件・Standalone 11 件すべて合格。残り: テストの `[Collection("ServiceProviderHolder")]` の改名（別コミット） |
| 5 | スロット状態の持ち主の新設（`IDeviceSlotState`。`Func<IUdpMotionHost>`／`Func<IDeviceStateAccessor>` の撤廃） | `Phase13-Step5-Plan.md` 承認済み（2026-11-04）。**5a（`2befe9d9`）・5b（`c1e7b452`）・5c（`89c61b78`）完了、5d（`080879ac`）完了、**5e 完了（コミット待ち）で Step5 の実装は完了**。5d: `UdpMotionService` が `IDeviceSlotState` を直接注入、`GetPadDetailForIdx` を移設、`IUdpMotionHost` 削除。5e: `Func<IDeviceStateAccessor>` の 5 利用者を直接注入に。5c の内容：`ServiceLifecycle`／`DeviceDisconnectHandler`／`DeviceConnector` が `IDeviceSlotState` を直接注入。`IDeviceSlotHost` は `ActiveControllers`＋`OnReport` に縮小）**。Step5 の残り: `Func<IDeviceSlotHost>` が `DeviceConnector` の 1 件（Step5b で撤廃）。実機確認は Step5b の後 |
| 5b | `On_Report` の `InputReportProcessor` への分離、`Mapping` の逆依存の解消、`IDeviceSlotHost` の削除 | `Phase13-Step5b-Plan.md` 承認済み（2026-11-04）。5b-a 完了（`5c63b43e`）: `On_Report` の特性テスト 6 件、before 計測（約 570〜690 ns／回、割り当て 0）。**5b-b 完了（コミット待ち）**: `IMapping.MapCustom` から `ControlService ctrl` を廃止、`Mapping` のコンストラクターに `IDeviceSlotState`／`IProfileActionProvider`／`ISpecialActionRepository` を追加（25 → 28）、`_sampleControl` を削除。次: 5b-c（ゲート） |
| 6〜12 | 計画書 §4 のとおり | 未着手 |

## Step1 の結果

- 削除した引数: `ControlButtonInputAction`／`ComboTriggerInputAction`／`MultiTapInputAction`／`ControlMultiTapInputAction`／`TwoStageTriggerProcessor` の `profileSettings`。各クラスの「引数を残した理由」のコメントと `<param>` も削除。
- 連動: `AssignmentBindingBuilder` の `_profileSettings` はこれらでしか使われていなかったため、フィールドと引数を削除（`ActionBindingBuilder`、`ControlBindingBuilder` の呼び出しを更新）。
- テスト: 12 ファイルの `new` を更新。`Phase10Step4W4ControlSpecialTests` の `ADelayWithoutTheSettings_IsRejected_AndNoDelayNeedsNone`（null 引数の拒否を検査していた）は前提が無くなったため、`ADelayWithoutAnyOtherArgument_IsAccepted` に置き換え。`ComboTriggerInputActionTests` の「settings が null なら例外」の 1 行も削除。
- 検証: `Debug ALL` 相当（Remove-bin → Build Debug → Build Test → Test）で警告 0・エラー 0。Actions 3,043 件、Standalone 11 件が合格。
- ラチェット: `Global`／`AppHost`／`ServiceProviderHolder` の件数に変化なし（基準値の更新なし）。

## Step2 の結果

- 削除: `profileXmlStore`（アクセサ `ProfileXmlStore` ごと。利用者 0 件）、`profileSlotApplier`、`syntheticInput`、`connectionProfileSelector`、`joyConLinker`、`deviceConnector`。`ControlService` の引数は 34 → 28。
- `ServiceRegistration.cs` の `new ControlService(…)` から対応する 6 行を削除。
- テスト: 新規 `Phase13Step2ControlServiceConstructorGuardTests`（引数 28 個・削除した型の不在・6 型の Singleton 解決）。削除した引数を前提にしていた既存テストの行（`ControlServicePr2DiWiringTests` の 2 行、`ControlServiceStep3Step2DiWiringTests` の 1 行、`Phase7Step8W4NoSharedGuardTests` の `_syntheticInput` 検査 1 行）を削除。
- 検証: 警告 0・エラー 0。Actions 3,042 件、Standalone 11 件が合格。ラチェットの基準値の変更なし。

## Step3 の結果

- 新設: `IEventThreadDispatcher`／`EventThreadDispatcher`（スレッド名・優先度・`IsBackground`・破棄の順序は旧 `ControlService` と同じ。停止後の呼び出しは何もしない。生成直後の使用に備えて Dispatcher の確定を待つ）、`IDeviceRumbleService`／`DeviceRumbleService`（式は `Adjust` に切り出し、テスト可能に）、`IOutputKbmInitializer`／`OutputKbmInitializer`（起動引数は `Initialize()` の時点で `IStartupArguments` から読む）。
- 廃止: `IVirtualPadHost`、`IServiceLifecycleHost`、`Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>`、`IUdpMotionHost.QueueOnEventThread`、`ControlService` の `QueueOnEventThread`／`EventDispatcher`／`SetDevRumble`／`RefreshOutputKBMHandler`（呼び出し元 0 件の残骸）／`InitOutputKBMHandler`。
- `ControlService` の引数: 28 のまま（`cmdParser`・`kbmLifecycle` を削除、`eventThread`・`rumble` を追加）。
- テスト: 新規 `EventThreadDispatcherTests`、`DeviceRumbleServiceTests`、`Phase13Step3HostRemovalGuardTests`、`FakeEventThreadDispatcher`。更新: `Phase9EVirtualPadServiceTests`／`Phase9EVirtualPadGuardTests`／`Phase9E6LifecycleGuardTests`／`Phase9EUdpMotionServiceTests`／`FakeUdpMotion`／`ControlServicePr3DiWiringTests`／`ServiceRegistrationArgumentParserTests`（`-virtualkbm` の経路の検査を、`OutputKbmInitializer` の `ResolveParser` に付け替えた）。
- 検証: 警告 0・エラー 0。Actions 3,053 件、Standalone 11 件が合格。ラチェットの基準値の変更なし。
- 計画書からの差: `OutputKbmInitializer` に `ResolveParser`（private）を足した（テストのため）。`OutputKbmInitializer` の生成・実機での KBM 出力の確認は Phase 末の H9。
- モデル図 03・04 の更新は、Step3 では未実施（図の更新は Step 群のまとまりごとに行い、Step12 で最終確認する）。
