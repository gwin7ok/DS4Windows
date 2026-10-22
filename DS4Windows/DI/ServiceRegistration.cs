using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Actions;
using DS4Windows.Services;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase 3 / Phase 4 / Phase 5: アプリケーション全体のDIコンテナ初期登録を一元管理する。
    /// </summary>
    public static class ServiceRegistration
    {
        // Phase7-Step8 W3: BackingStore.ActionsLoaded の購読者（コンテナの作り直しで重複購読しないための記憶）
        private static ActionsLoadedRuntimeReset _subscribedReset;

        // Phase7-Step10d W5（Q3＝A）: existingStore は、コンテナの構築より前に作った BackingStore（本体の起動のブートストラップ。起動直後の設定の読み取りが使う）。
        // 省略（null）のときは、コンテナが BackingStore を作る（テストなど）。どちらも、IProcessInspector の設定と ActionsLoaded の購読は、この登録が行う。
        public static IServiceCollection RegisterServices(this IServiceCollection services, BackingStore existingStore = null)
        {
            // === 第4層: 4-c DIサービス群 (Singleton) ===
            // Phase7-Step3 W1: BackingStore（設定の唯一の実体）を登録する。
            // Phase7-Step10d W5: 旧 Global.store（静的）をやめた。ブートストラップの BackingStore があればそれを、なければコンテナが作る。
            // IProcessInspector はプロパティで設定する（プロファイル読込の「起動するプログラム」の稼働確認で使う。BackingStore を
            // コンテナより前に作る場合があるため、コンストラクターでは渡せない）。
            services.AddSingleton<BackingStore>(sp =>
            {
                var store = existingStore ?? new BackingStore(sp.GetRequiredService<IEnvironmentService>());
                store.ProcessInspector = sp.GetRequiredService<IProcessInspector>();
                // Phase7-Step7 W2（Q4＝A）: Actions.xml の読み込み後の実行時状態のリセット。ブートストラップの BackingStore はコンテナより長く生きるため、
                // コンテナが作り直されても購読が 1 つになるよう、前回の購読者を外してから登録する（Phase7-Step8 W3: instance の購読者）
                var reset = sp.GetRequiredService<ActionsLoadedRuntimeReset>();
                if (_subscribedReset != null) store.ActionsLoaded -= _subscribedReset.Reset;
                store.ActionsLoaded += reset.Reset;
                _subscribedReset = reset;
                return store;
            });
            // Phase7-Step8 W4: 状態の保持クラス 3 つ（合成入力・マクロ・スティック／ジャイロ／トリガー）。暫定の静的な窓口（Shared）は削除し、
            // コンテナの Singleton を、利用者（Mapping、Mouse、ControlService ほか）がコンストラクターで受ける
            services.AddSingleton<SyntheticInputState>();
            services.AddSingleton<MacroRuntimeState>();
            services.AddSingleton<StickMotionState>();
            // Phase7-Step8 W3: Actions.xml の読み込み後のリセットの購読者（保持クラスをコンストラクターで受ける）
            services.AddSingleton<ActionsLoadedRuntimeReset>();
            // Phase7-Step8 W3（Q6＝A）: 合成キーの送出。Actions サブシステム（静的な呼び出し）が使う橋渡し SyntheticDispatcher へ、作成時に 1 度だけ束縛する（Phase8 で削除）
            services.AddSingleton<ISyntheticDispatcher>(sp =>
            {
                var dispatcher = new SyntheticDispatcherService(
                    sp.GetRequiredService<SyntheticInputState>(), sp.GetRequiredService<OutputKBMHandlerAdapter>(), sp.GetRequiredService<IProfileSettingsService>());
                SyntheticDispatcher.Bind(dispatcher);
                return dispatcher;
            });
            services.AddSingleton<IPathService, PathService>();
            // Phase10-Step4d W1: プロファイルの固定 ID と名前（ファイル名）を結ぶ登録簿
            // W5b: 登録簿を作るとき、BackingStore のスロットの保管（ProfileSlotNames）へ結ぶ（ID ↔ 名前を引く）
            services.AddSingleton<IProfileIdentityService>(sp =>
            {
                var service = new ProfileIdentityService(sp.GetRequiredService<IPathService>());
                sp.GetRequiredService<BackingStore>().AttachProfileIdentity(service);
                (sp.GetRequiredService<IProfileSettingsService>() as ProfileSettingsService)?.AttachProfileIdentity(service);   // W5c: 一時プロファイルの名前
                return service;
            });
            services.AddSingleton<IDeviceStateService, DeviceStateService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();
            services.AddSingleton<INotificationService, AppNotificationService>();

            // Phase6-Step7-4（決定7＝案H）: 起動引数の保持役。値は AppHost.CreateHost(config, parser) が設定する。
            services.AddSingleton<IStartupArguments, StartupArguments>();

            // Phase 5 Step 12: 出力スロット永続化・管理サービス
            services.AddSingleton<IOutputSlotStore, OutputSlotStore>();
            // Phase6-Step10-4-3: スロット管理の実体は ControlService が所有する。循環を避けるため、遅延で取得する
            // Phase7-Step6 W1: 出力種別の配列は OutputSlotService が持ち、BackingStore へは本サービスの生成時に渡す。
            // BackingStore の登録側から IOutputSlotService を解決してはならない（IOutputSlotStore → IPathService → BackingStore と循環し、
            // 実行時にスタックオーバーフローになる）。配列を書き込むのは ControlService だけで、IOutputSlotService を通すため、書き込みの前に必ず接続済みになる。
            services.AddSingleton<IOutputSlotService>(sp =>
            {
                var service = new OutputSlotService(
                    () => sp.GetRequiredService<IVirtualPadService>().OutputSlotManager,   // Phase9 9-E E-4: スロット管理の実体は VirtualPadService が持つ（旧: ControlService.OutputslotMan）
                    sp.GetRequiredService<IOutputSlotStore>(),
                    sp.GetRequiredService<Func<IVirtualPadService>>());
                sp.GetRequiredService<BackingStore>().ActiveOutDevType = service.ActiveOutDevType;
                return service;
            });

            // ControlService は遅延解決（循環の回避）
            services.AddSingleton<IProfileSettingsService>(sp => new ProfileSettingsService(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IEnvironmentService>(), sp.GetRequiredService<Func<ControlService>>()));
            // Phase7-Step10c W2: 設定ファイルの保存・読込を担う型（旧: BackingStore のメソッド）。BackingStore を受ける具象クラスの Singleton
            services.AddSingleton<SpecialActionsXmlSerializer>();
            services.AddSingleton<LinkedProfilesXmlSerializer>();
            services.AddSingleton<ControllerConfigsXmlSerializer>();
            services.AddSingleton<AppSettingsXmlSerializer>();
            services.AddSingleton<LegacyProfileXmlSerializer>();
            services.AddSingleton<ProfilePresets>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            // Phase6-Step10-2（決定 E2＝A）: プリセットの読み込み（Global.Load…Profile への薄い委譲）
            services.AddSingleton<IProfilePresetLoader, ProfilePresetLoader>();
            services.AddSingleton<IProfileSlotApplier, ProfileSlotApplier>();
            services.AddSingleton<ISpecialActionRepository>(sp => new SpecialActionRepository(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IPathService>(),
                sp.GetRequiredService<SyntheticInputState>(), sp.GetRequiredService<SpecialActionsXmlSerializer>()));

            // Phase 3 Step 3-6: プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();


            // プロファイルのアクション取得（ControlService が使用）。Phase6-Step10-4-2d: アクション連鎖サービスは廃止
            services.AddSingleton<IProfileActionProvider>(sp => new ProfileActionProvider(sp.GetRequiredService<BackingStore>()));

            // Phase7-Step8 W2（Q3＝A）: デバイス × アクション名ごとの KeyButtonActionController の登録簿（旧: Mapping の静的フィールド）
            services.AddSingleton<IKeyButtonControllerRegistry, KeyButtonControllerRegistry>();

            // Phase 5 Step 3: プロファイル適用サービス
            services.AddSingleton<IProfileApplicationService, ProfileApplicationService>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション・実行サービス
            services.AddSingleton<AutoProfileHolder>();
            services.AddSingleton<IAutoProfileService, AutoProfileService>();

            // Phase 5 Step 6: アプリ全体設定サービス
            // 修正前:
            // services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // 修正後（完全修飾名で確実にバインド）:
            services.AddSingleton<IAppSettingsService, DS4Windows.Services.AppSettingsService>();

            // Phase 5 Step 10: UDP サーバーサービス（Cemuhook モーションサーバー境界化）
            // Phase9 9-E E-3: 旧 IUdpServerService／UdpServerService（未接続の並行実装）は削除した（ユーザー承認）。UDP の窓口は IUdpMotionService

            // === 第3層: Actions基盤サービス ===
            services.AddSingleton<IKeyButtonActionControllerFactory, DefaultKeyButtonActionControllerFactory>();
            services.AddSingleton<IRepeater, RepeatHelperToIRepeaterAdapter>();
            services.AddSingleton<IProcessLauncher, DefaultProcessLauncher>();
            services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
            // Phase8-Step7 7.6a: GyroCalibrate が実機のコントローラーを操作する窓口（IDeviceStateAccessor は遅延で解決する）
            services.AddSingleton<IGyroCalibrationPort, DefaultGyroCalibrationPort>();
            // Phase8-Step7 7.6b: DisconnectBT が実機のコントローラーを操作する窓口（同上）
            services.AddSingleton<IControllerDisconnectPort, DefaultControllerDisconnectPort>();
            // Phase8-Step7 7.6c: BatteryCheck が実機のコントローラー・ライトバーの強制色・通知を操作する窓口（同上。ControlService も遅延で解決する）
            services.AddSingleton<IBatteryCheckPort, DefaultBatteryCheckPort>();
            // Phase8-Step7 7.6d: ステアリング較正が実機のコントローラーの較正の状態を読み書きする窓口（同上）
            // Phase8-Step7 7.7b: MultiAction が、マクロを文字列で直接実行する窓口（Mapping へ遅延で委譲する）
            services.AddSingleton<IMacroStringPlayer, DefaultMacroStringPlayer>();
            // Phase7-Step8 W3（Q6＝A）: 出力ハンドラの実体を持つ OutputKBMHandlerAdapter を 1 つ登録し、IVirtualKBM（送出）と IVirtualKBMLifecycle（決定・破棄）が共有する
            services.AddSingleton<OutputKBMHandlerAdapter>();
            services.AddSingleton<IVirtualKBM>(sp => sp.GetRequiredService<OutputKBMHandlerAdapter>());
            services.AddSingleton<IVirtualKBMLifecycle, OutputKBMHandlerLifecycle>();
            services.AddSingleton<IMacroPlayer, DefaultMacroPlayer>();
            // Phase8-Step7 7.8e: Controls タブの Extras が、振動・ライトバーの強制色・ボタン感度を操作する窓口（ControlService は遅延で解決する）
            services.AddSingleton<IControlExtrasPort, DefaultControlExtrasPort>();

            // Phase6-Step9b-2（F02）: IManagedActionManager と IControllerRegistry の再登録。
            // 2026-09-04（31dddc43、自動プロファイル設定コレクションの追加）で、無関係な差分として 3 件（ほかに IActionRegistry）が
            // 巻き込み削除されていた。IManagedActionManager が未登録だと、ActionManager.SetToggledOn が例外を投げ
            // （KeyButtonActionController が握りつぶす）IsToggledOn が true にならず、IControllerRegistry が未登録だと
            // KeyButtonActionController の登録と終了時の破棄が行われない。
            // AppHost.PreallocateActionEntries は、登録された DefaultActionManager を前提にしている。
            // IActionRegistry（系統A）は、実装が静的クラスで呼び出し元もないため再登録しない。
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

            // Phase6-Step9-4（決定2＝C1）: ViewModelFactory が ControlService を生成のたびに遅延解決するための Func
            // （ControlService は Singleton。明示的なホスト構築より前に解決しないため、構築時には解決しない）
            services.AddSingleton<Func<ControlService>>(sp => () => sp.GetRequiredService<ControlService>());
            // Phase7-Step8 W2（Q1＝A・Q4＝A・Q8＝A）: 入力の評価の窓口（Singleton）。Mapping は IDeviceStateAccessor（＝ControlService）を遅延で受け、
            // DefaultActionManager／DefaultMacroPlayer は Func<IMapping> で受ける（ホスト構築時に Mapping とその依存を作らないため。Phase5-Plan §5.4）。
            services.AddSingleton<IStickDeadZoneProcessor, StickDeadZoneProcessor>();   // Phase9 9-A(1) (b): スティックのデッドゾーンの補正（状態なし）
            services.AddSingleton<IOutputCurveProcessor, OutputCurveProcessor>();   // Phase9 9-A(1) (c): スクエアスティックと出力カーブ（状態なし）
            services.AddSingleton<ITriggerDeadZoneProcessor, TriggerDeadZoneProcessor>();   // Phase9 9-A(1) (d): トリガーのデッドゾーンの補正（状態なし）
            services.AddSingleton<IGyroControlsProcessor, GyroControlsProcessor>();   // Phase9 9-A(2)-A: ジャイロ → コントロールの補正（状態なし）
            services.AddSingleton<IStickInputConditioner, StickInputConditioner>();   // Phase9 9-A(2)-B: ドリフト補正・アンチスナップバック・ファズ（状態なし）
            services.AddSingleton<IFlickStickProcessor, FlickStickProcessor>();   // Phase9 9-A(2)-C: フリックスティック（状態なし）
            services.AddSingleton<IButtonMouseMovementCalculator, ButtonMouseMovementCalculator>();   // Phase9 9-B: ボタン等 → マウスの生の移動量（マウス加速の状態をデバイスごとに持つ）
            services.AddSingleton<IMouseMotionQuantizer, MouseMotionQuantizer>();   // Phase9 9-B: マウスの移動量の整数化（端数をデバイスごとに持つ）
            services.AddSingleton<IGyroMouseStickBlender, GyroMouseStickBlender>();   // Phase9 9-B: ジャイロのマウススティック出力の合成（状態なし）
            services.AddSingleton<IOscTransportFactory, SharpOscTransportFactory>();   // Phase9 9-E E-1: OSC の通信（SharpOSC）の生成
            services.AddSingleton<IOscService, OscService>();   // Phase9 9-E E-1: OSC（受信の解釈、送信、入力への重ね合わせ、変化の送信）
            services.AddSingleton<IHidHideApiFactory, HidHideApiFactory>();   // Phase9 9-E E-2: HidHide ドライバーの窓口の生成
            services.AddSingleton<IHidHideService, HidHideService>();   // Phase9 9-E E-2: HidHide（ホワイトリストの確認、影響を受けるデバイスの判定）
            services.AddSingleton<IVirtualPadService, VirtualPadService>();   // Phase9 9-E E-4: 仮想パッド（ViGEm）の出力。OutputSlotManager、割り当て、振動・ライトバーの戻り
            services.AddSingleton<Func<IVirtualPadService>>(sp => () => sp.GetRequiredService<IVirtualPadService>());   // OutputSlotService が遅延で取得する（循環を避ける）
            services.AddSingleton<Func<IVirtualPadHost>>(sp => () => sp.GetRequiredService<IVirtualPadHost>());   // VirtualPadService が遅延で取得する（実体は ControlService）
            services.AddSingleton<Func<IOutputSlotService>>(sp => () => sp.GetRequiredService<IOutputSlotService>());   // VirtualPadService が遅延で取得する（循環を避ける）
            services.AddSingleton<IUdpMotionServerFactory, UdpMotionServerFactory>();   // Phase9 9-E E-3: UDP サーバー（UdpServer）の生成
            services.AddSingleton<IUdpMotionService, UdpMotionService>();   // Phase9 9-E E-3: UDP モーションの送出（サーバーの作成・開始・停止、ハンドラー、スムージング）
            services.AddSingleton<Func<IUdpMotionHost>>(sp => () => sp.GetRequiredService<IUdpMotionHost>());   // ControlService が部品を受けるため、遅延で取得（循環を避ける）
            services.AddSingleton<ILatencyMonitor, LatencyMonitor>();   // Phase9 9-E E-5: 入力レポートのエラーのログと遅延の監視
            services.AddSingleton<ITouchpadToggleService, TouchpadToggleService>();   // Phase9 9-E E-5: タッチパッドの切り替え・スライドの判定
            services.AddSingleton<IConnectionProfileSelector, ConnectionProfileSelector>();   // Phase9 9-E E-6a: 接続時のプロファイルの選択と適用
            // Phase9 9-E E-6c: デバイスの種類ごとの部品（カートリッジ）。新しい種類は、IInputDeviceModule を実装してここへ足すだけ
            services.AddSingleton<IInputDeviceModule, Ds4DeviceModule>();
            services.AddSingleton<IInputDeviceModule, DualSenseDeviceModule>();
            services.AddSingleton<IInputDeviceModule, SwitchProDeviceModule>();
            services.AddSingleton<IInputDeviceModule, JoyConDeviceModule>();
            services.AddSingleton<IInputDeviceModule, Ds3DeviceModule>();
            services.AddSingleton<IInputDeviceModuleRegistry, InputDeviceModuleRegistry>();
            services.AddSingleton<IServiceLifecycle, ServiceLifecycle>();   // Phase9 9-E E-6f2: 開始・停止・ホットプラグと、その状態・イベント
            services.AddSingleton<Func<IServiceLifecycleHost>>(sp => () => sp.GetRequiredService<IServiceLifecycleHost>());   // ServiceLifecycle が遅延で取得する（実体は ControlService）
            services.AddSingleton<IDeviceConnector, DeviceConnector>();   // Phase9 9-E E-6f1: 接続の組み立て
            services.AddSingleton<IDeviceDisconnectHandler, DeviceDisconnectHandler>();   // Phase9 9-E E-6e: 切断・同期・シリアル変更の処理
            services.AddSingleton<Func<IDeviceSlotHost>>(sp => () => sp.GetRequiredService<IDeviceSlotHost>());   // DeviceDisconnectHandler が遅延で取得する（実体は ControlService）
            services.AddSingleton<IJoyConLinker, JoyConLinker>();   // Phase9 9-E E-6d: Joy-Con の結合の手順（実機なしでも偽物のデバイスでテストできる）
            services.AddSingleton<IDeviceConnectionSetup, DeviceConnectionSetup>();   // Phase9 9-E E-6b: 接続時の設定の反映とイベントの配線
            services.AddSingleton<IActiveInputProbe, ActiveInputProbe>();   // Phase9 9-E E-5: 押されている入力の探索（状態なし）
            services.AddSingleton<IMacroStepExecutor, MacroStepExecutor>();   // Phase9 9-C: マクロの 1 つの値の実行（状態は MacroRuntimeState に持つ）
            services.AddSingleton<IMacroPlayback, MacroPlayback>();   // Phase9 9-C: マクロの再生の予約・繰り返し・後始末
            services.AddSingleton<ISyntheticInputCommitter, SyntheticInputCommitter>();   // Phase9 MapCustom の分割 M1: 合成入力の確定（クリック・ホイール。ホイールの繰り返しの状態を持つ）
            services.AddSingleton<IControlActivityReader, ControlActivityReader>();   // Phase9 9-B: コントロールが押されているかの判定（状態なし）
            services.AddSingleton<IMapping, Mapping>();
            services.AddSingleton<Func<IMapping>>(sp => () => sp.GetRequiredService<IMapping>());
            services.AddSingleton<Func<IDeviceStateAccessor>>(sp => () => sp.GetRequiredService<IDeviceStateAccessor>());
            // Phase6-Step10-2-3: IAutoProfileService の遅延解決（AutoProfileHolder は設定の場所が決まった後でなければ生成できない）
            services.AddSingleton<Func<IAutoProfileService>>(sp => () => sp.GetRequiredService<IAutoProfileService>());

            // Phase 4: UI層 ViewModel ファクトリの登録
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // Phase6-Step10-2（決定 E1＝A）: 補助ウィンドウ（ボタン割り当て、マクロ記録、プリセット、スティック校正、スペシャルアクション編集）の生成の窓口
            services.AddSingleton<IEditorWindowFactory, EditorWindowFactory>();

            // Phase7-Step5 W5（Q4＝A）: メイン画面の生成の窓口。MainWindow のコンストラクターで AppHost.GetService を使わないための窓口
            services.AddSingleton<IMainWindowFactory, MainWindowFactory>();

            // Phase6-Step10-1-5（決定D7＝A）: ログ設定の反映の窓口。LoggerHolder は設定の読み込み後にしか生成できないため、
            // 遅延接続のプロキシを登録し、App が LoggerHolder の生成直後に Attach する。
            services.AddSingleton<LogSettingsApplierProxy>();
            services.AddSingleton<ILogSettingsApplier>(sp => sp.GetRequiredService<LogSettingsApplierProxy>());

            // Phase 4: Pattern A ViewModel (Transient)
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<AboutViewModel>();

            // Phase 4: Pattern B ViewModel (Singleton)
            services.AddSingleton<MainWindowsViewModel>();

            // === 既存Singletonインスタンスの取得登録 ===
            // Phase7-Step10d W3（Q4＝A）: 第 1 層が読む設定値の窓口
            services.AddSingleton<IDeviceLayerSettings, DeviceLayerSettings>();
            services.AddSingleton<IDs4DeviceRegistry>(sp => new Ds4DeviceRegistryAdapter(sp.GetRequiredService<IDeviceLayerSettings>()));
            services.AddSingleton<ControlService>(sp =>
            {
                // Phase7-Step5 W2（Q2＝A）: 旧 Program.rootHub ?? の分岐を削除した。シングルトンの生成は、このファクトリーだけが行う
                return new ControlService(
                    // Phase6-Step7-4（決定5＝A、決定7＝案H）: AppHost.CreateHost(config, parser) が IStartupArguments に
                    // 設定した実際の起動引数を渡す。2026-09-04（4c89cd91）以降は常に空の new ArgumentParser() を渡していたため、
                    // 起動引数 -virtualkbm（キーボード・マウス出力方式の指定）が無視されていた。
                    // ホストが起動引数なしに先に作られる順序（-driverinstall 分岐など）でも動くよう、コンテナへの登録
                    // （AddSingleton(parser)）ではなく、登録済みの保持役を経由する（Phase6-Step7-Plan.md §0.4・決定7）。
                    // 起動引数が設定されていない場合（引数なしの CreateHost()、テスト）は、従来どおり空のパーサーを使う。
                    // 注意: ControlService はこの時点の値を保持するため、CreateHost(config, parser) より前に解決してはならない
                    // （現在は App.CreateControlService で、明示的なホスト構築の後に解決している）。
                    sp.GetRequiredService<IStartupArguments>().Parser ?? new ArgumentParser(),
                    sp.GetRequiredService<IDs4DeviceRegistry>(),
                    sp.GetRequiredService<IProfileSettingsService>(),
                    sp.GetRequiredService<IAppSettingsService>(),
                    sp.GetRequiredService<IEnvironmentService>(),
                    sp.GetRequiredService<IPathService>(),
                    () => sp.GetRequiredService<IOutputSlotService>(), // 決定D1: 遅延解決（OutputSlotService → ControlService の逆依存を回避）
                    sp.GetRequiredService<IProfileRepository>(),
                    sp.GetRequiredService<IDeviceStateService>(),
                    sp.GetRequiredService<IProfileXmlStore>(),
                    sp.GetRequiredService<IProfileSlotApplier>(),
                    sp.GetRequiredService<IVirtualKBM>(),
                    sp.GetRequiredService<IVirtualKBMLifecycle>(),
                    sp.GetRequiredService<IAppearanceSettingsService>(),
                    sp.GetRequiredService<IProfileActionProvider>(),
                    sp.GetRequiredService<ISpecialActionRepository>(),
                    sp.GetRequiredService<IElevatedProcessLauncher>(),
                    // Phase7-Step8 W1: 保持クラスをコンストラクターで受ける（旧: ControlService／Mouse が Shared を参照）
                    sp.GetRequiredService<StickMotionState>(),
                    sp.GetRequiredService<SyntheticInputState>(),
                    sp.GetRequiredService<IMapping>(),
                    sp.GetRequiredService<IOscService>(),   // Phase9 9-E E-1
                    sp.GetRequiredService<IHidHideService>(),   // Phase9 9-E E-2
                    sp.GetRequiredService<IUdpMotionService>(),   // Phase9 9-E E-3
                    sp.GetRequiredService<IVirtualPadService>(),   // Phase9 9-E E-4
                    sp.GetRequiredService<ILatencyMonitor>(),   // Phase9 9-E E-5
                    sp.GetRequiredService<ITouchpadToggleService>(),   // Phase9 9-E E-5
                    sp.GetRequiredService<IActiveInputProbe>(),   // Phase9 9-E E-5
                    sp.GetRequiredService<IConnectionProfileSelector>(),   // Phase9 9-E E-6a
                    sp.GetRequiredService<IDeviceConnectionSetup>(),   // Phase9 9-E E-6b
                    sp.GetRequiredService<IInputDeviceModuleRegistry>(),   // Phase9 9-E E-6c
                    sp.GetRequiredService<IJoyConLinker>(),   // Phase9 9-E E-6d
                    sp.GetRequiredService<IDeviceDisconnectHandler>(),   // Phase9 9-E E-6e
                    sp.GetRequiredService<IDeviceConnector>(),   // Phase9 9-E E-6f1
                    sp.GetRequiredService<IServiceLifecycle>()   // Phase9 9-E E-6f2
                );
            });
            services.AddSingleton<IDeviceStateAccessor>(sp => sp.GetRequiredService<ControlService>());
            services.AddSingleton<IUdpMotionHost>(sp => sp.GetRequiredService<ControlService>());   // Phase9 9-E E-3
            services.AddSingleton<IVirtualPadHost>(sp => sp.GetRequiredService<ControlService>());
            services.AddSingleton<IDeviceSlotHost>(sp => sp.GetRequiredService<ControlService>());   // Phase9 9-E E-6e
            services.AddSingleton<IServiceLifecycleHost>(sp => sp.GetRequiredService<ControlService>());   // Phase9 9-E E-6f2   // Phase9 9-E E-4

            return services;
        }
    }
}