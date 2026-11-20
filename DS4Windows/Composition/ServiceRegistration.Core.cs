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
        /// 横断基盤・永続化（<c>Core/</c>）の実装の登録。
        /// <c>BackingStore</c> と <c>IProfileIdentityService</c> は、生成のときに配線をするため <c>AddWiredServices</c>（ServiceRegistration.Wiring.cs）にある。
        /// </summary>
        private static void AddCoreServices(IServiceCollection services)
        {
            services.AddSingleton<LightbarOverrides>();   // ライトバーの強制表示の状態
            // 時間の扱い（マクロの待ち、キーのリピート）の時計。テストは仮想の時計に差し替える
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();

            // 起動引数の保持役。値は AppHost.CreateHost(config, parser) が設定する。
            services.AddSingleton<IStartupArguments, StartupArguments>();

            // 出力スロット永続化・管理サービス（管理の IOutputSlotService は ServiceRegistration.Output.cs）

            // 旧 Func<ControlService>（遅延）をやめ、依存のない IConfigurableDeviceLookup を直接受ける
            services.AddSingleton<ProfileSettingsService>(sp => new ProfileSettingsService(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IEnvironmentService>(), sp.GetRequiredService<IConfigurableDeviceLookup>()));
            services.AddSingleton<IProfileSettingsService>(sp => sp.GetRequiredService<ProfileSettingsService>());   // 具象を 1 つだけ登録する（登録簿を結ぶ配線が、具象を使うため）
            // 設定ファイルの保存・読込を担う型。BackingStore を受ける具象クラスの Singleton
            services.AddSingleton<SpecialActionsXmlSerializer>();
            services.AddSingleton<LinkedProfilesXmlSerializer>();
            services.AddSingleton<ControllerConfigsXmlSerializer>();
            services.AddSingleton<AppSettingsXmlSerializer>();
            services.AddSingleton<LegacyProfileXmlSerializer>();
            services.AddSingleton<ProfilePresets>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            // プリセットの読み込み（Global.Load…Profile への薄い委譲）
            services.AddSingleton<IProfilePresetLoader, ProfilePresetLoader>();
            // 保存・読込の後の実行時状態のリセット ISpecialActionRuntimeReset（Core の契約、第2層の実装）は ServiceRegistration.Processing.cs
            services.AddSingleton<ISpecialActionRepository>(sp => new SpecialActionRepository(
                sp.GetRequiredService<BackingStore>(), sp.GetRequiredService<IPathService>(),
                sp.GetRequiredService<SpecialActionsXmlSerializer>(), sp.GetRequiredService<ISpecialActionRuntimeReset>()));

            // プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();
            // ウィンドウのフォーカス（Win32 の操作と、手順を分ける）
            services.AddSingleton<IWindowSystem, Win32WindowSystem>();
            services.AddSingleton<IWindowFocuser, WindowFocuser>();
            services.AddSingleton<IMouseInputHook, LowLevelMouseHook>();   // マクロの記録で、画面全体のマウス入力を監視する
            services.AddSingleton<IStartupEntryService, StartupEntryService>();   // Windows のスタートアップ登録（旧 静的 StartupMethods）
            // 更新の確認・変更履歴・ドライバーのダウンロードに使う HttpClient（旧 App.requestClient。User-Agent も同じ）と、変更履歴のサービス
            services.AddSingleton<System.Net.Http.HttpClient>(_ =>
            {
                var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "DS4Windows");
                return client;
            });
            services.AddSingleton<IChangelogService, Changelog>();

            // 自動プロファイル設定コレクション（実行サービス IAutoProfileService は ServiceRegistration.Processing.cs）
            services.AddSingleton<AutoProfileHolder>();

            // アプリ全体設定サービス
            // 旧「修正前／修正後」のコメント（完全修飾名の DS4Windows.Core.Settings.AppSettingsService で登録していた）を整理した。同名の型は他にない
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // ログ設定の反映の窓口。LoggerHolder は設定の読み込み後にしか生成できないため、
            // 遅延接続のプロキシを登録し、App が LoggerHolder の生成直後に Attach する。
            services.AddSingleton<LogSettingsApplierProxy>();
            services.AddSingleton<ILogSettingsApplier>(sp => sp.GetRequiredService<LogSettingsApplierProxy>());
        }
    }
}
