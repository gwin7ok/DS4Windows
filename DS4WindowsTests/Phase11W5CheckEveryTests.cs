using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W5（Issue #4 の 4、Q4）: 「更新確認」が OFF のとき、更新確認の間隔（数値）の変更は保存されない。
    /// これは仕様であり、不具合ではない: OFF のときは、数値を入力する行ごと無効（<c>IsEnabled</c>）で、利用者は数値を操作できない。
    /// setter が OFF のとき何もしないのは、無効な入力欄のバインディングの再評価が、更新確認を ON に戻してしまうのを防ぐための安全弁
    /// （<c>SettingsViewModel.CheckEvery</c> のコメント）。この意図を、テストで固定する。
    /// </summary>
    public class Phase11W5CheckEveryTests
    {
        private const string MainWindowXaml = "DS4Windows/UI/MainWindow.xaml";

        static Phase11W5CheckEveryTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }
        }

        /// <summary>保存（<c>SaveAppSettingsXml</c>）の回数を数えるだけの XML ストア</summary>
        private class CountingXmlStore : IProfileXmlStore
        {
            public int SaveCallCount { get; private set; }

            public bool LoadProfileXml(int deviceIndex, bool launchProgram, DS4Windows.Processing.Abstractions.IProfileLoadTarget control,
                string overridePath = "", bool xinputChange = true, bool postLoad = true) => true;
            public bool SaveProfileXml(int deviceIndex, string profileName) => true;
            public bool LoadControllerConfigsForDevice(DS4Device device) => true;
            public bool SaveControllerConfigsForDevice(DS4Device device) => true;
            public bool LoadAppSettingsXml() => true;

            public bool SaveAppSettingsXml()
            {
                SaveCallCount++;
                return true;
            }
        }

        private static (SettingsViewModel Vm, IAppSettingsService Service, CountingXmlStore Store) Create(bool checkUpdates, int everyValue)
        {
            var store = new CountingXmlStore();
            IAppSettingsService service = TestRepositoryFactory.CreateAppSettingsService(store, store: new BackingStore(new EnvironmentService()));
            service.CheckUpdateStartupEnabled = checkUpdates;
            service.CheckEveryValue = everyValue;
            return (SettingsViewModelTestFactory.Create(service), service, store);
        }

        [Fact]
        public void WhenUpdateChecksAreOff_ChangingTheNumber_IsIgnored_ByDesign()
        {
            var (vm, service, store) = Create(checkUpdates: false, everyValue: 3);
            using (vm)
            {
                vm.CheckEvery = 9;

                Assert.Equal(3, service.CheckEveryValue);
                Assert.Equal(3, vm.CheckEvery);
                Assert.False(service.CheckUpdateStartupEnabled);   // 数値の更新が、更新確認を ON に戻さない
                Assert.Equal(0, store.SaveCallCount);
            }
        }

        [Fact]
        public void WhenUpdateChecksAreOn_TheNumberIsStored_AndSaved()
        {
            var (vm, service, store) = Create(checkUpdates: true, everyValue: 3);
            using (vm)
            {
                vm.CheckEvery = 9;

                Assert.Equal(9, service.CheckEveryValue);
                Assert.Equal(9, vm.CheckEvery);
                Assert.Equal(1, store.SaveCallCount);
            }
        }

        [Fact]
        public void TheNumberRow_IsDisabledAsAWhole_WhenUpdateChecksAreOff()
        {
            var document = XamlTestHelper.Load(MainWindowXaml);
            if (document == null) return;

            var row = XamlTestHelper.FindByName(document, "checkEveryOptionsStack");

            // 数値の入力欄を含む行ごと、「更新確認」の値で有効／無効になる（OFF なら操作できない）
            Assert.Equal("{Binding CheckForUpdates}", XamlTestHelper.Attr(row, "IsEnabled"));
            var numberInput = row.Descendants().Single(e => e.Name.LocalName == "IntegerUpDown");
            Assert.Contains("CheckEvery", XamlTestHelper.Attr(numberInput, "Value"));
            // 入力欄自身は IsEnabled を上書きしない（上書きすると、行の無効が効かなくなる）
            Assert.Null(XamlTestHelper.Attr(numberInput, "IsEnabled"));
        }

        [Fact]
        public void TheRowBinding_FollowsTheCheckBox_AtRuntime()
        {
            // 設定画面と同じ形（チェックボックスと、行の IsEnabled が、同じ VM の同じプロパティを指す）を、実物の VM で動かし、
            // チェックを外すと、行の中の入力欄が操作できなくなることを確かめる。
            // VM は INotifyPropertyChanged を実装しない。バインディングの書き戻し（PropertyDescriptor 経由）が、他のバインディングを更新する。
            RunOnSta(() =>
            {
                var (vm, _, _) = Create(checkUpdates: true, everyValue: 3);
                using (vm)
                {
                    var checkBox = new CheckBox();
                    var input = new TextBox();
                    var row = new StackPanel { Children = { input } };
                    var root = new StackPanel { DataContext = vm, Children = { checkBox, row } };
                    checkBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding("CheckForUpdates"));
                    row.SetBinding(UIElement.IsEnabledProperty, new Binding("CheckForUpdates"));

                    Assert.True(input.IsEnabled);

                    checkBox.IsChecked = false;

                    Assert.False(vm.CheckForUpdates);
                    Assert.False(row.IsEnabled);
                    Assert.False(input.IsEnabled);   // 行が無効なら、中の入力欄も操作できない

                    checkBox.IsChecked = true;

                    Assert.True(input.IsEnabled);
                }
            });
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                if (failure is Xunit.Sdk.XunitException) throw failure;
                throw new InvalidOperationException("STA スレッドで失敗: " + failure.Message, failure);
            }
        }
    }
}
