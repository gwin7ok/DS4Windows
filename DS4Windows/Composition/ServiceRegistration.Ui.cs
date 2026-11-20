#nullable enable

using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.Composition
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// 4-a（<c>UI/</c>）の実装と、App へ委譲するアダプター（<c>Composition/</c>）の登録。
        /// </summary>
        private static void AddUiServices(IServiceCollection services)
        {
            services.AddSingleton<INotificationService, AppNotificationService>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();   // 画面のスレッドへの受け渡し（ViewModel が Dispatcher を直接使わない）
            services.AddSingleton<IProcessPriorityControl, ProcessPriorityControl>();   // 自分のプロセスの優先度の設定
            services.AddSingleton<IShellLauncher, ShellLauncher>();   // フォルダー・プログラム・Web ページを OS に開かせる窓口
            services.AddSingleton<IDialogService, WpfDialogService>();   // 確認・通知・ファイル選択の窓の窓口

            // UI層 ViewModel ファクトリの登録
            services.AddSingleton<SettingsViewModelFactory>();
            services.AddSingleton<AutoProfilesViewModelFactory>();
            services.AddSingleton<IShellViewModelFactory, ShellViewModelFactory>();
            services.AddSingleton<IProfileEditViewModelFactory, ProfileEditViewModelFactory>();
            services.AddSingleton<IActionViewModelFactory, ActionViewModelFactory>();

            // 補助ウィンドウ（ボタン割り当て、マクロ記録、プリセット、スティック校正、スペシャルアクション編集）の生成の窓口
            services.AddSingleton<AssignmentWindowFactory>();   // 割り当ての窓（出力ピッカー、割り当て編集画面、プリセット、校正）の生成
            services.AddSingleton<MacroRecordingWindowFactory>();   // マクロ記録の窓の生成
            services.AddSingleton<ProfileEditorBackend>();   // プロファイル編集画面が使う業務のサービスの窓口
            services.AddSingleton<ProfileEditorFactory>();   // プロファイル編集画面の生成
            services.AddSingleton<IEditorWindowFactory, EditorWindowFactory>();

            // メイン画面の生成の窓口。MainWindow のコンストラクターで AppHost.GetService を使わないための窓口
            services.AddSingleton<IMainWindowFactory, MainWindowFactory>();
            services.AddSingleton<AutoProfileRunner>();
            services.AddSingleton<MainWindowLayoutViewModel>();   // メイン画面の配置（位置・大きさ・列幅）
            services.AddSingleton<UpdateCheckPolicy>();   // 更新の確認の判断
            services.AddSingleton<IpcCommandDispatcher>();   // CLI のコマンドの実行の窓口
            services.AddSingleton<TrayAppearanceController>();   // メイン画面のトレイ・通知・テーマの切り替え
            services.AddSingleton<ProfileSelectionCoordinator>();   // メイン画面のプロファイルの選択・編集後の同期
            // 画面のテーマ・言語の切り替え（App へ委譲するアダプター）
            services.AddSingleton<AppThemeAndCultureService>(sp => new AppThemeAndCultureService(sp.GetRequiredService<DS4Windows.Core.Settings.BackingStore>()));
            services.AddSingleton<IUiThemeAndLanguage>(sp => sp.GetRequiredService<AppThemeAndCultureService>());
            services.AddSingleton<DS4Windows.Core.Platform.IpcMemoryMap>();   // CLI との通信のメモリマップトファイル（本体側が、クラス名の公開と問い合わせの結果の書き込みに使う）
            services.AddSingleton<IIpcResultWriter, IpcResultWriterAdapter>();   // CLI の問い合わせの結果の書き込み

            // Pattern A ViewModel (Transient)
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<AboutViewModel>();

            // Pattern B ViewModel (Singleton)
            services.AddSingleton<MainWindowsViewModel>();
        }
    }
}
