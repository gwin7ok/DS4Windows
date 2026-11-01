using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W5: <see cref="SettingsViewModel"/> の生成を 1 か所にまとめる（設定画面の setter の検証用）。
    /// 設定は、渡された <see cref="IAppSettingsService"/> だけを読み書きする。省略したときは、他のテストと状態を共有しないよう、
    /// 専用の <see cref="BackingStore"/> を持つ <see cref="AppSettingsService"/> を作る（保存先の XML ストアは null のため、
    /// <c>Save()</c> を呼ぶ setter の検証では、保存を数える偽の実装を渡すこと）。
    /// </summary>
    internal static class SettingsViewModelTestFactory
    {
        public static SettingsViewModel Create(IAppSettingsService appSettings = null)
        {
            return new SettingsViewModel(
                appSettings ?? TestRepositoryFactory.CreateAppSettingsService(store: new BackingStore(new EnvironmentService())),
                TestRepositoryFactory.CreateAppearanceSettingsService(),
                new EnvironmentService(),
                new PathService(),
                new LogSettingsApplierProxy());
        }
    }
}
