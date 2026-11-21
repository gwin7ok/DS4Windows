#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// はじめの案内画面の、導入の操作の使える／使えない。導入の実行中は、すべて止める。
    /// 実行が終わったら、OS の条件（HidHide は Windows 10 x64 以上、FakerInput は Windows 8.1 以上）で使えるものだけを戻す。
    /// </summary>
    public sealed class WelcomeDialogViewModel : ObservableObject
    {
        private readonly bool hidHideCompatible;
        private readonly bool fakerInputCompatible;
        private bool installRunning;

        public WelcomeDialogViewModel(bool hidHideCompatible, bool fakerInputCompatible)
        {
            this.hidHideCompatible = hidHideCompatible;
            this.fakerInputCompatible = fakerInputCompatible;
        }

        /// <summary>導入を実行中か。</summary>
        public bool InstallRunning
        {
            get => installRunning;
            set
            {
                if (!SetProperty(ref installRunning, value)) return;
                OnPropertyChanged(nameof(ViGEmEnabled));
                OnPropertyChanged(nameof(HidHideEnabled));
                OnPropertyChanged(nameof(FakerInputEnabled));
            }
        }

        public bool ViGEmEnabled => !installRunning;
        public bool HidHideEnabled => !installRunning && hidHideCompatible;
        public bool FakerInputEnabled => !installRunning && fakerInputCompatible;
    }
}
