#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using DS4Windows.UI.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>INotifyPropertyChanged</c> を実装した ViewModel は、WPF が「<c>XxxChanged</c> イベント」の旧来の通知を見なくなる。
    /// 値が変わったら、<c>PropertyChanged</c> で必ず通知する（設定した直後に画面へ出ない不具合の再発防止）。
    /// </summary>
    public class ViewModelChangeNotificationTests
    {
        private static List<string?> Watch(INotifyPropertyChanged vm)
        {
            var names = new List<string?>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
            return names;
        }

        [Fact]
        public void LaunchProgram_ChoosingAFile_NotifiesThePathNameAndIcon()
        {
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());
            var names = Watch(vm);

            vm.Filepath = @"C:\Tools\game.exe";

            Assert.Contains(nameof(LaunchProgramViewModel.Filepath), names);
            Assert.Contains(nameof(LaunchProgramViewModel.ProgramName), names);
            Assert.Contains(nameof(LaunchProgramViewModel.ProgramIconPath), names);
        }

        [Fact]
        public void SimpleProperties_NotifyWhenTheyChange()
        {
            var vm = new SpecialActionViewModel(0, new RecordingSpecialActionRepository());
            var names = Watch(vm);

            vm.HoldInterval = 1.5;

            Assert.Contains(nameof(SpecialActionViewModel.HoldInterval), names);
        }
    }
}
