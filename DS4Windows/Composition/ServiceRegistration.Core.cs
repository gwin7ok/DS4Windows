using DS4Windows.Output.Services;
using DS4Windows.Output.Abstractions;
using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Composition
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Phase14c-Step6（R-18）: 横断基盤・永続化（<c>Core/</c>）の実装の登録。
        /// <c>BackingStore</c> と <c>IProfileIdentityService</c> は、生成のときに配線をするため <c>AddWiredServices</c>（ServiceRegistration.Wiring.cs）にある。
        /// </summary>
        private static void AddCoreServices(IServiceCollection services)
        {
            services.AddSingleton<LightbarOverrides>();   // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
            // 時間の扱い（マクロの待ち、キーのリピート）の時計。テストは仮想の時計に差し替える
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();

            // Phase6-Step7-4（決定7＝案H）: 起動引数の保持役。値は AppHost.CreateHost(config, parser) が設定する。
            services.AddSingleton<IStartupArguments, StartupArguments>();

            // Phase 5 Step 12: 出力スロット永続化・管理サービス（管理の IOutputSlotService は ServiceRegistration.Output.cs）

            // Phase13-Step6 6-b: 旧 Func<ControlService>（遅延）をやめ、依存のない IDeviceStateAccessor を直接受ける
            services.AddSingleton<IProfileSettingsService>(sp => new ProfileSettingsService(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IEnvironmentService>(), sp.GetRequiredService<IDeviceStateAccessor>()));
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
            // Phase14b-Step14（D14）: 保存・読込の後の実行時状態のリセット ISpecialActionRuntimeReset（Core の契約、第2層の実装）は ServiceRegistration.Processing.cs
            services.AddSingleton<ISpecialActionRepository>(sp => new SpecialActionRepository(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IPathService>(),
                sp.GetRequiredService<SpecialActionsXmlSerializer>(), sp.GetRequiredService<ISpecialActionRuntimeReset>()));

            // Phase 3 Step 3-6: プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();
            // Phase12-Step2 2b-2（R-4）: ウィンドウのフォーカス（Win32 の操作と、手順を分ける）
            services.AddSingleton<IWindowSystem, Win32WindowSystem>();
            services.AddSingleton<IWindowFocuser, WindowFocuser>();
            services.AddSingleton<IMouseInputHook, LowLevelMouseHook>();   // Phase12-Step3 3e: マクロの記録で、画面全体のマウス入力を監視する
            services.AddSingleton<IStartupEntryService, StartupEntryService>();   // Phase14b-Step15（D12）: Windows のスタートアップ登録（旧 静的 StartupMethods）
            // Phase14b-Step17（決定 D10）: 更新の確認・変更履歴・ドライバーのダウンロードに使う HttpClient（旧 App.requestClient。User-Agent も同じ）と、変更履歴のサービス
            services.AddSingleton<System.Net.Http.HttpClient>(_ =>
            {
                var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "DS4Windows");
                return client;
            });
            services.AddSingleton<IChangelogService, Changelog>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション（実行サービス IAutoProfileService は ServiceRegistration.Processing.cs）
            services.AddSingleton<AutoProfileHolder>();

            // Phase 5 Step 6: アプリ全体設定サービス
            // Phase14c-Step6: 旧「修正前／修正後」のコメント（完全修飾名の DS4Windows.Core.Settings.AppSettingsService で登録していた）を整理した。同名の型は他にない
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // Phase6-Step10-1-5（決定D7＝A）: ログ設定の反映の窓口。LoggerHolder は設定の読み込み後にしか生成できないため、
            // 遅延接続のプロキシを登録し、App が LoggerHolder の生成直後に Attach する。
            services.AddSingleton<LogSettingsApplierProxy>();
            services.AddSingleton<ILogSettingsApplier>(sp => sp.GetRequiredService<LogSettingsApplierProxy>());
        }
    }
}
