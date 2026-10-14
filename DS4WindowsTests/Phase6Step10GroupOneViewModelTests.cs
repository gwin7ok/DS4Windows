using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-1-3: AboutViewModel／UpdaterWindowViewModel／LogViewModel が、依存を必須引数で受け取り
    /// （null を拒否）、Global と同じ値・動作を保つこと。
    /// </summary>
    public class Phase6Step10GroupOneViewModelTests
    {
        [Fact]
        public void AboutViewModel_RequiresEnvironmentService()
        {
            Assert.Throws<ArgumentNullException>(() => new AboutViewModel(null));
        }

        [Fact]
        public void AboutViewModel_VersionText_MatchesGlobalExeVersion()
        {
            var vm = new AboutViewModel(new EnvironmentService());
            Assert.Equal($"v{Global.exeversion}", vm.VersionText);
        }

        // UpdaterWindowViewModel は internal のため、リフレクションで生成・呼び出す（本番コードの可視性は変えない）。
        private static readonly Type UpdaterVmType = typeof(AboutViewModel).Assembly
            .GetType("DS4WinWPF.DS4Forms.ViewModels.UpdaterWindowViewModel", throwOnError: true);

        private static object CreateUpdaterVm(string version, IAppSettingsService settings)
        {
            try
            {
                return Activator.CreateInstance(UpdaterVmType, version, settings);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        [Fact]
        public void UpdaterWindowViewModel_RequiresAppSettingsService()
        {
            Assert.Throws<ArgumentNullException>(() => CreateUpdaterVm("1.2.3", null));
        }

        [Fact]
        public void UpdaterWindowViewModel_SetAndBlankSkippedVersion_UpdateGlobalLastVersionChecked()
        {
            string original = Global.LastVersionChecked ?? string.Empty;
            try
            {
                object vm = CreateUpdaterVm("9.8.7", new DS4Windows.Services.AppSettingsService());

                UpdaterVmType.GetMethod("SetSkippedVersion").Invoke(vm, null);
                Assert.Equal("9.8.7", Global.LastVersionChecked);

                UpdaterVmType.GetMethod("BlankSkippedVersion").Invoke(vm, null);
                Assert.Equal(string.Empty, Global.LastVersionChecked);
            }
            finally
            {
                Global.LastVersionChecked = original;
            }
        }

        [Fact]
        public void LogViewModel_RequiresEnvironmentService()
        {
            Assert.Throws<ArgumentNullException>(() => new LogViewModel(null, null));
        }
    }
}
