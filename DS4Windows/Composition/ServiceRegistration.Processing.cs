using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;

namespace DS4Windows.Composition
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Phase14c-Step6（R-18）: 第2層（<c>Processing/</c>）の実装の登録。第1層が使う窓口（<c>IDeviceLayerSettings</c>）の実装と、入力機種の部品（<c>IInputDeviceModule</c>）を含む。
        /// <c>IInputDeviceModule</c> は同じサービス型を複数登録するため、解決すると登録順の列になる。並びを変えないこと。
        /// </summary>
        private static void AddProcessingServices(IServiceCollection services)
        {
            // Phase7-Step8 W4: 状態の保持クラス 3 つ（合成入力・マクロ・スティック／ジャイロ／トリガー）。暫定の静的な窓口（Shared）は削除し、
            // コンテナの Singleton を、利用者（Mapping、Mouse、ControlService ほか）がコンストラクターで受ける
            services.AddSingleton<SyntheticInputState>();
            services.AddSingleton<MacroRuntimeState>();
            services.AddSingleton<StickMotionState>();
            // Phase7-Step8 W3: Actions.xml の読み込み後のリセットの購読者（保持クラスをコンストラクターで受ける）
            services.AddSingleton<ActionsLoadedRuntimeReset>();
            // Phase7-Step8 W3: 合成キーの送出。Phase14c-Step3（R-06）: 静的な橋渡し SyntheticDispatcher（Bind）は撤廃し、利用者（キーの操作の部品）は
            // IKeyButtonControllerRegistry・IKeyButtonActionControllerFactory 経由で受け取る
            services.AddSingleton<ISyntheticDispatcher, SyntheticDispatcherService>();
            services.AddSingleton<IDeviceStateService, DeviceStateService>();

            services.AddSingleton<IProfileSlotApplier, ProfileSlotApplier>();
            // Phase14b-Step14（D14）: 保存・読込の後の実行時状態のリセット（Core の契約、第2層の実装）
            services.AddSingleton<ISpecialActionRuntimeReset, SpecialActionRuntimeReset>();

            // プロファイルのアクション取得（ControlService が使用）。Phase6-Step10-4-2d: アクション連鎖サービスは廃止
            services.AddSingleton<IProfileActionProvider>(sp => new ProfileActionProvider(sp.GetRequiredService<BackingStore>()));

            // Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: Mapping の静的フィールド）
            services.AddSingleton<IKeyButtonControllerRegistry, KeyButtonControllerRegistry>();

            // Phase 5 Step 3: プロファイル適用サービス
            services.AddSingleton<IProfileApplicationService, ProfileApplicationService>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション・実行サービス（コレクションの AutoProfileHolder は ServiceRegistration.Core.cs）
            services.AddSingleton<IAutoProfileService, AutoProfileService>();

            // Actions サブシステム（Processing/Actions/）の部品と、実機を操作する窓口（Phase14c-Step6: 旧見出し「第3層: Actions基盤サービス」は、層の名前が誤っていたため改めた）
            services.AddSingleton<IKeyButtonActionControllerFactory, DefaultKeyButtonActionControllerFactory>();
            services.AddSingleton<IRepeater, RepeatHelperToIRepeaterAdapter>();
            services.AddSingleton<IProcessLauncher, DefaultProcessLauncher>();
            services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
            // Phase8-Step7 7.6a: GyroCalibrate が実機のコントローラーを操作する窓口（IDeviceStateAccessor は IDeviceSlotState を直接注入する。Phase13-Step5）
            services.AddSingleton<IGyroCalibrationPort, DefaultGyroCalibrationPort>();
            // Phase8-Step7 7.6b: DisconnectBT が実機のコントローラーを操作する窓口（同上）
            services.AddSingleton<IControllerDisconnectPort, DefaultControllerDisconnectPort>();
            // Phase8-Step7 7.6c: BatteryCheck が実機のコントローラー・ライトバーの強制色・通知を操作する窓口（同上。ControlService も遅延で解決する）
            services.AddSingleton<IDeviceBatteryText, DeviceBatteryText>();   // Phase13-Step6 6-b: 電池残量の文言（旧 ControlService.GetDS4Battery）
            services.AddSingleton<IBatteryCheckPort, DefaultBatteryCheckPort>();
            // Phase8-Step7 7.6d: ステアリング較正が実機のコントローラーの較正の状態を読み書きする窓口（同上）
            // （Phase9 のステアリングの削除［955e7f3b］で、窓口 IWheelCalibrationPort とその登録は削除済み）
            // Phase8-Step7 7.7b: MultiAction が、マクロを文字列で直接実行する窓口（Mapping へ遅延で委譲する）
            services.AddSingleton<IMacroStringPlayer, DefaultMacroStringPlayer>();
            // Phase7-Step8 W3（Q6＝A）: 出力ハンドラ（IVirtualKBM・IVirtualKBMLifecycle）の登録は ServiceRegistration.Output.cs
            services.AddSingleton<IMacroPlayer, DefaultMacroPlayer>();
            // Phase8-Step7 7.8e: Controls タブの Extras が、振動・ライトバーの強制色・ボタン感度を操作する窓口（ControlService は遅延で解決する）
            services.AddSingleton<IControlExtrasPort, DefaultControlExtrasPort>();

            // Phase6-Step9b-2（F02）: IManagedActionManager と IControllerRegistry の再登録。
            // 2026-09-04（31dddc43、自動プロファイル設定コレクションの追加）で、無関係な差分として 3 件（ほかに IActionRegistry）が
            // 巻き込み削除されていた。IManagedActionManager が未登録だと、ActionManager.SetToggledOn が例外を投げ
            // （KeyButtonActionController が握りつぶす）IsToggledOn が true にならず、IControllerRegistry が未登録だと
            // KeyButtonActionController の登録と終了時の破棄が行われない。
            // AppHost.PreallocateActionEntries は、登録された DefaultActionManager を前提にしている。
            // 旧 系統A の静的な ActionRegistry は、本番で未初期化のため Phase14c-Step5（決定 E3）で削除した（IActionRegistry は統一トリガーディスパッチの ActionBindingRegistry）。
            services.AddSingleton<IManagedActionManager, DefaultActionManager>();
            services.AddSingleton<IControllerRegistry, DefaultControllerRegistry>();

            // Phase8-Step4 W1: 統一トリガーディスパッチの束縛の置き場。Phase8-Step8 8d: 種別ごとの移行のスイッチ（BindingMigrationOptions）は削除した（ロールバックは git のみ）。
            services.AddSingleton<IActionRegistry, ActionBindingRegistry>();
            services.AddSingleton<ITriggerDispatchEngine, TriggerDispatchEngine>();
            // Phase8-Step5 W2（Q3＝A）: 束縛のビルダー。ProfileApplicationService は Func で遅延解決する（ビルダー → IProfileSwitcher → IProfileApplicationService の循環を避ける）
            // Phase10-Step4 W2: 種別ごとの出力アクション・方針の作り方（SA タブの ActionBindingBuilder と、Controls タブの束縛を作る側［W3 以降］が共有する）
            services.AddSingleton<ISpecialActionOutputFactory, SpecialActionOutputFactory>();
            services.AddSingleton<IActionBindingBuilder, ActionBindingBuilder>();
            services.AddSingleton<Func<IActionBindingBuilder>>(sp => () => sp.GetRequiredService<IActionBindingBuilder>());
            // Phase8-Step7 7.8a: Controls タブの束縛のビルダー（空の配線。区分ごとの組み立ては 7.8b 以降）
            services.AddSingleton<IControlBindingBuilder, ControlBindingBuilder>();

            // Phase13-Step6 6-e: 旧 Func<ControlService> の登録（Phase6-Step9-4 決定2＝C1。ViewModelFactory などの遅延解決用）は、利用者 9 件がすべて直接注入に替わったため削除した
            // Phase7-Step8 W2（Q1＝A・Q4＝A・Q8＝A）: 入力の評価の窓口（Singleton）。Mapping の ControlService への逆依存は Phase13-Step5b（5b-b）で解消した［IDeviceSlotState／IProfileActionProvider／ISpecialActionRepository をコンストラクターで受ける］。
            // Phase13-Step8: 遅延取得（Func<I…>）で受けるのは、本物の循環がある DefaultActionManager（IMapping）と ProfileApplicationService（IActionBindingBuilder）だけ（Phase13FuncRatchetGuardTests の許可リスト）。
            services.AddSingleton<IStickDeadZoneProcessor, StickDeadZoneProcessor>();   // Phase9 9-A(1) (b): スティックのデッドゾーンの補正（状態なし）
            services.AddSingleton<IOutputCurveProcessor, OutputCurveProcessor>();   // Phase9 9-A(1) (c): スクエアスティックと出力カーブ（状態なし）
            services.AddSingleton<ITriggerDeadZoneProcessor, TriggerDeadZoneProcessor>();   // Phase9 9-A(1) (d): トリガーのデッドゾーンの補正（状態なし）
            services.AddSingleton<IGyroControlsProcessor, GyroControlsProcessor>();   // Phase9 9-A(2)-A: ジャイロ → コントロールの補正（状態なし）
            services.AddSingleton<IStickInputConditioner, StickInputConditioner>();   // Phase9 9-A(2)-B: ドリフト補正・アンチスナップバック・ファズ（状態なし）
            services.AddSingleton<IFlickStickProcessor, FlickStickProcessor>();   // Phase9 9-A(2)-C: フリックスティック（状態なし）
            services.AddSingleton<IButtonMouseMovementCalculator, ButtonMouseMovementCalculator>();   // Phase9 9-B: ボタン等 → マウスの生の移動量（マウス加速の状態をデバイスごとに持つ）
            services.AddSingleton<IMouseMotionQuantizer, MouseMotionQuantizer>();   // Phase9 9-B: マウスの移動量の整数化（端数をデバイスごとに持つ）
            services.AddSingleton<IGyroMouseStickBlender, GyroMouseStickBlender>();   // Phase9 9-B: ジャイロのマウススティック出力の合成（状態なし）
            // Phase9 9-E E-1〜E-4（OSC、HidHide、仮想パッド、UDP）の登録は ServiceRegistration.Output.cs
            services.AddSingleton<ILatencyMonitor, LatencyMonitor>();   // Phase9 9-E E-5: 入力レポートのエラーのログと遅延の監視
            services.AddSingleton<ITouchpadToggleService, TouchpadToggleService>();   // Phase9 9-E E-5: タッチパッドの切り替え・スライドの判定
            services.AddSingleton<IIpcCommandProcessor, IpcCommandProcessor>();   // Phase13-Step11 11-b: IPC コマンドの実行（UI に頼む操作は IIpcCommandHost）
            services.AddSingleton<IRuntimeFeatureSwitch, RuntimeFeatureSwitch>();   // Phase13-Step10 10-c: 画面が使う OSC／UDP／HidHide／タッチパッドの切り替えの窓口
            services.AddSingleton<IConnectionProfileSelector, ConnectionProfileSelector>();   // Phase9 9-E E-6a: 接続時のプロファイルの選択と適用
            // Phase9 9-E E-6c: デバイスの種類ごとの部品（カートリッジ）。新しい種類は、IInputDeviceModule を実装してここへ足すだけ
            services.AddSingleton<IInputDeviceModule, Ds4DeviceModule>();
            services.AddSingleton<IInputDeviceModule, DualSenseDeviceModule>();
            services.AddSingleton<IInputDeviceModule, SwitchProDeviceModule>();
            services.AddSingleton<IInputDeviceModule, JoyConDeviceModule>();
            services.AddSingleton<IInputDeviceModule, Ds3DeviceModule>();
            services.AddSingleton<IInputDeviceModuleRegistry, InputDeviceModuleRegistry>();
            services.AddSingleton<IEventThreadDispatcher, EventThreadDispatcher>();   // Phase13-Step3: 専用のイベントスレッド
            // Phase13-Step3: ブースト・反転つきの振動（IDeviceRumbleService）と KBM 出力の初期化（IOutputKbmInitializer）の登録は ServiceRegistration.Output.cs
            services.AddSingleton<IServiceLifecycle, ServiceLifecycle>();   // Phase9 9-E E-6f2: 開始・停止・ホットプラグと、その状態・イベント
            services.AddSingleton<IDeviceConnector, DeviceConnector>();   // Phase9 9-E E-6f1: 接続の組み立て
            services.AddSingleton<IDeviceDisconnectHandler, DeviceDisconnectHandler>();   // Phase9 9-E E-6e: 切断・同期・シリアル変更の処理
            services.AddSingleton<IJoyConLinker, JoyConLinker>();   // Phase9 9-E E-6d: Joy-Con の結合の手順（実機なしでも偽物のデバイスでテストできる）
            services.AddSingleton<IProfileLoadTarget, ProfileLoadTarget>();   // Phase13-Step6 6-d: プロファイル読込の鎖が control 引数から使う 8 メンバーの窓口（6-e で control の型を置き換える）
            services.AddSingleton<IDeviceConnectionSetup, DeviceConnectionSetup>();   // Phase9 9-E E-6b: 接続時の設定の反映とイベントの配線
            services.AddSingleton<IActiveInputProbe, ActiveInputProbe>();   // Phase9 9-E E-5: 押されている入力の探索（状態なし）
            services.AddSingleton<IMacroStepExecutor, MacroStepExecutor>();   // Phase9 9-C: マクロの 1 つの値の実行（状態は MacroRuntimeState に持つ）
            services.AddSingleton<IMacroPlayback, MacroPlayback>();   // Phase9 9-C: マクロの再生の予約・繰り返し・後始末
            services.AddSingleton<ITwoStageTriggerProcessor, TwoStageTriggerProcessor>();   // Phase9 MapCustom の分割 M2: 2 段階トリガーの状態機械（状態なし。状態は StickMotionState の TwoStageTriggerMappingData）
            services.AddSingleton<ISyntheticInputCommitter, SyntheticInputCommitter>();   // Phase9 MapCustom の分割 M1: 合成入力の確定（クリック・ホイール。ホイールの繰り返しの状態を持つ）
            services.AddSingleton<IControlActivityReader, ControlActivityReader>();   // Phase9 9-B: コントロールが押されているかの判定（状態なし）
            services.AddSingleton<IMapping, Mapping>();
            services.AddSingleton<Func<IMapping>>(sp => () => sp.GetRequiredService<IMapping>());
            // Phase13-Step8 8-c: IAutoProfileService の遅延取得デリゲートの登録は撤廃した（利用者 2 件の UI ファクトリーは、設定の場所の確定後に解決される）

            // Phase7-Step10d W3（Q4＝A）: 第 1 層が読む設定値の窓口
            services.AddSingleton<IDeviceLayerSettings, DeviceLayerSettings>();
            services.AddSingleton<IDs4DeviceRegistry>(sp => new Ds4DeviceRegistryAdapter(sp.GetRequiredService<IDeviceLayerSettings>()));
            services.AddSingleton<ControlService>(sp =>
            {
                // Phase7-Step5 W2（Q2＝A）: 旧 Program.rootHub ?? の分岐を削除した。シングルトンの生成は、このファクトリーだけが行う
                return new ControlService(
                    // Phase13-Step3: 起動引数（-virtualkbm）は OutputKbmInitializer が Initialize() の時点で IStartupArguments から読む（旧: ここで渡していた）
                    sp.GetRequiredService<IDs4DeviceRegistry>(),
                    sp.GetRequiredService<IProfileSettingsService>(),
                    sp.GetRequiredService<IAppSettingsService>(),
                    sp.GetRequiredService<IPathService>(),
                    sp.GetRequiredService<IOutputSlotService>(),   // Phase13-Step7 7-d: 旧: 遅延取得デリゲート（逆依存は Phase9 E-4 で解消済み）
                    sp.GetRequiredService<IProfileRepository>(),
                    sp.GetRequiredService<IDeviceStateService>(),
                    // Phase13-Step5b-5b-e: IVirtualKBM／IAppearanceSettingsService／IProfileActionProvider／ISpecialActionRepository は、使われなくなったため外した
                    sp.GetRequiredService<IElevatedProcessLauncher>(),
                    // Phase7-Step8 W1: 保持クラスをコンストラクターで受ける（旧: ControlService／Mouse が Shared を参照）
                    sp.GetRequiredService<StickMotionState>(),
                    sp.GetRequiredService<IMapping>(),
                    sp.GetRequiredService<IVirtualPadService>(),   // Phase9 9-E E-4
                    sp.GetRequiredService<IInputDeviceModuleRegistry>(),   // Phase9 9-E E-6c
                    sp.GetRequiredService<IServiceLifecycle>(),   // Phase9 9-E E-6f2
                    sp.GetRequiredService<IEventThreadDispatcher>(),   // Phase13-Step3
                    sp.GetRequiredService<IDeviceSlotState>(),   // Phase13-Step5
                    sp.GetRequiredService<IInputReportProcessor>()   // Phase13-Step5b
                );
            });
            // Phase13-Step5: スロット状態の持ち主。IDeviceStateAccessor（GetController）も同じインスタンスを返す
            services.AddSingleton<IDeviceSlotState, DeviceSlotState>();
            services.AddSingleton<IDeviceStateAccessor>(sp => sp.GetRequiredService<IDeviceSlotState>());
            // UI 向けの、接続中のコントローラーの読み取りの窓口（UI は DS4Device と ControlService を持たない）
            services.AddSingleton<IControllerSlotQuery, ControllerSlotQuery>();
            services.AddSingleton<IControllerSlotCommands, ControllerSlotCommands>();
            services.AddSingleton<IControllerReadingsSource, ControllerReadingsSource>();
            services.AddSingleton<IMacroRecordingState, MacroRecordingState>();
            // Phase13-Step5b: 入力レポートの処理（旧 ControlService.On_Report）と、その作業用の状態の持ち主。出力種別の配列は IOutputDeviceTypeState から直接受ける
            services.AddSingleton<IInputReportProcessor>(sp => new InputReportProcessor(
                sp.GetRequiredService<LightbarOverrides>(),   // Phase14c-Step4
                sp.GetRequiredService<IDeviceSlotState>(),
                sp.GetRequiredService<ILatencyMonitor>(),
                sp.GetRequiredService<IPathService>(),
                sp.GetRequiredService<IProfileRepository>(),
                sp.GetRequiredService<IAppearanceSettingsService>(),
                sp.GetRequiredService<IProfileSettingsService>(),
                sp.GetRequiredService<IVirtualKBM>(),
                sp.GetRequiredService<IMapping>(),
                sp.GetRequiredService<ITouchpadToggleService>(),
                sp.GetRequiredService<IProfileActionProvider>(),
                sp.GetRequiredService<IAppSettingsService>(),
                sp.GetRequiredService<IOscService>(),
                sp.GetRequiredService<IVirtualPadService>(),
                sp.GetRequiredService<IOutputDeviceTypeState>()));   // Phase13-Step7 7-c: 旧: 遅延取得デリゲート
        }
    }
}
