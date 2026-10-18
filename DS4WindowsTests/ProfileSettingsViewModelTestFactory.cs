using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-4: <see cref="ProfileSettingsViewModel"/> のコンストラクタの依存が必須引数になったため、
    /// テストでの生成を 1 か所にまとめる。引数を省略した場合は、従来のコンストラクタのフォールバックと同じ解決をする
    /// （profileSettings は Global.ProfileSettingsServiceInstance、outputSlotService は DI（なければ Global の実体）、
    /// ControlService は null）。引数が増えてもテスト側の修正が不要になる。
    /// </summary>
    internal static class ProfileSettingsViewModelTestFactory
    {
        // Phase7c: ProfileSettingsViewModel のコンストラクターは pack://application:,,, の URI を作る。pack スキームと
        // リソースのアセンブリは、WPF の Application が作られて初めて使える。以前は、Application を作る別のテストクラス
        // （ProfileSettingsViewModelTests など）が先に動いたときだけ通る、実行順に依存したテストがあった
        // （Phase7bTriggerDeadZoneRoundingTests が UriFormatException: Invalid port specified で失敗した）。
        // ファクトリーを通る生成なら、どのクラスが先でも通るよう、ここで同じ初期化を行う。
        static ProfileSettingsViewModelTestFactory()
        {
            if (System.Windows.Application.Current == null)
            {
                try { _ = new System.Windows.Application(); }
                catch { }
            }

            if (System.Windows.Application.ResourceAssembly == null)
            {
                try { System.Windows.Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly; }
                catch { }
            }
        }

        public static ProfileSettingsViewModel Create(
            int device,
            IProfileSettingsService profileSettings = null,
            IOutputSlotService outputSlotService = null,
            int targetDevice = -1)
        {
            DS4WinWPF.AppHost.CreateHost();

            return new ProfileSettingsViewModel(
                device,
                profileSettings ?? TestGlobals.ProfileSettingsServiceInstance,
                null,
                outputSlotService ?? TestHost.GetService<IOutputSlotService>(),
                TestHost.GetService<IProfileRepository>(),
                TestHost.GetService<IProfilePresetLoader>(),
                TestHost.GetService<IEnvironmentService>(),
                TestHost.GetService<IPathService>(),
                targetDevice);
        }
    }
}
