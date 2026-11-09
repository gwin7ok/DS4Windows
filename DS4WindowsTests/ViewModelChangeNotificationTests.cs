using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ViewModel の変更通知の共通の土台（<see cref="ObservableObject"/>）と、
    /// 「プロパティ名＋Changed」のイベントから <see cref="INotifyPropertyChanged.PropertyChanged"/> へ集めた結果を守る。
    /// 画面（バインディング）と、画面のコードビハインドの購読は、どちらも PropertyChanged を見る。
    /// </summary>
    public class ViewModelChangeNotificationTests
    {
        private sealed class Sample : ObservableObject
        {
            private int _value;
            public int Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }
        }

        [Fact]
        public void SetProperty_RaisesOnlyWhenTheValueChanges_AndReportsThePropertyName()
        {
            var sample = new Sample();
            var names = new List<string>();
            sample.PropertyChanged += (s, e) => names.Add(e.PropertyName);

            sample.Value = 3;
            sample.Value = 3;
            sample.Value = 4;

            Assert.Equal(new[] { nameof(Sample.Value), nameof(Sample.Value) }, names);
            Assert.Equal(4, sample.Value);
        }

        [Fact]
        public void ViewModelsOfTheMigratedScreens_DeriveFromObservableObject_AndHaveNoNameChangedEvents()
        {
            foreach (Type type in new[] { typeof(SettingsViewModel), typeof(ProfileSettingsViewModel), typeof(AxialStickControlViewModel) })
            {
                Assert.True(typeof(ObservableObject).IsAssignableFrom(type), type.Name + " が ObservableObject から派生していない");

                // 「<プロパティ名>Changed」の EventHandler は、PropertyChanged へ集めた。戻さない
                string[] leftovers = type.GetEvents(BindingFlags.Instance | BindingFlags.Public)
                    .Where(e => e.EventHandlerType == typeof(EventHandler) && e.Name.EndsWith("Changed", StringComparison.Ordinal))
                    .Select(e => e.Name).ToArray();
                Assert.True(leftovers.Length == 0, type.Name + " に残っている: " + string.Join(", ", leftovers));
            }
        }

        [Fact]
        public void ProfileSettingsViewModel_RaisesPropertyChanged_WhenTheDeadZoneChanges()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Sticks;
            double original = vm.LSDeadZone;
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
            try
            {
                vm.LSDeadZone = original >= 0.5 ? 0.25 : 0.75;
                Assert.Contains(nameof(StickSettingsViewModel.LSDeadZone), names);
            }
            finally
            {
                vm.LSDeadZone = original;
            }
        }

        [Fact]
        public void ProfileSettingsViewModel_MainColorChange_RenotifiesTheDerivedDisplayProperties()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Lightbar;
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);

            vm.UpdateMainColor(System.Windows.Media.Color.FromRgb(10, 20, 30));

            foreach (string expected in new[]
            {
                nameof(LightbarSettingsViewModel.MainColorString), nameof(LightbarSettingsViewModel.MainColorR),
                nameof(LightbarSettingsViewModel.MainColorG), nameof(LightbarSettingsViewModel.MainColorB),
                nameof(LightbarSettingsViewModel.LightbarBrush),
            })
            {
                Assert.Contains(expected, names);
            }
        }

        /// <summary>
        /// 画面（コードビハインド）は、ViewModel を自分で作らない。作るのは <c>IViewModelFactory</c> で、画面は受け取るか、ファクトリー経由で得る。
        /// </summary>
        [Fact]
        public void ViewCodeBehind_DoesNotConstructViewModels()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/UI/MainWindow.xaml.cs");
            if (anchor == null) return;

            var offenders = new List<string>();
            foreach (string file in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(anchor), "*.xaml.cs"))
            {
                string text = SourceText.ReadCode("DS4Windows/UI/" + System.IO.Path.GetFileName(file));
                if (text == null) continue;
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"new\s+\w*ViewModel\s*\("))
                    offenders.Add(System.IO.Path.GetFileName(file) + ": " + m.Value);
            }

            Assert.True(offenders.Count == 0, "画面が ViewModel を直接作っている:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }
    }
}
