using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class ControllersViewModel : INotifyPropertyChanged
    {
        private readonly IDeviceStateService _deviceStateService;
        private readonly IProfileSettingsService _profileSettingsService;
        private readonly IProfileRepository _profileRepository;

        public event PropertyChangedEventHandler PropertyChanged;

        // Phase6-Step10-2-2（F09）: 依存はすべて必須引数（Pure DI。決定1＝S9-A と同じ方針）。
        // 置換前は任意引数（= null）と AppHost.GetService のフォールバックだった。DI には Singleton で登録済み。
        public ControllersViewModel(
            IDeviceStateService deviceStateService,
            IProfileSettingsService profileSettingsService,
            IProfileRepository profileRepository)
        {
            _deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            _profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        }

        public bool HasControllers => _deviceStateService.ConnectedControllersCount > 0;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
