using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 特殊アクション（割り当て編集画面の各アクション、マクロ、プログラム起動など）の ViewModel を作る。
    /// 窓口は <see cref="DS4Windows.UI.Abstractions.IActionViewModelFactory"/>。依存は、ここで作る ViewModel が使うものだけを受ける。
    /// </summary>
    public sealed class ActionViewModelFactory : DS4Windows.UI.Abstractions.IActionViewModelFactory
    {
        private readonly IProfileRepository _profileRepo;
        private readonly ISpecialActionRepository _actionRepo;
        private readonly IEnvironmentService _environmentService;
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public ActionViewModelFactory(IProfileRepository profileRepo,
            ISpecialActionRepository actionRepo,
            IEnvironmentService environmentService,
            DS4Windows.Core.Model.LightbarOverrides lightbar)
        {
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _actionRepo = actionRepo ?? throw new ArgumentNullException(nameof(actionRepo));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _lightbar = lightbar ?? throw new ArgumentNullException(nameof(lightbar));
        }

        public MacroViewModel CreateMacroViewModel() => new MacroViewModel(_actionRepo);

        public LaunchProgramViewModel CreateLaunchProgramViewModel() => new LaunchProgramViewModel(_actionRepo);

        public LoadProfileViewModel CreateLoadProfileViewModel(ProfileList profileList)
            => new LoadProfileViewModel(profileList, _actionRepo);

        public SpecialActionViewModel CreateSpecialActionViewModel(int actionIndex)
            => new SpecialActionViewModel(actionIndex, _actionRepo);

        public CheckBatteryViewModel CreateCheckBatteryViewModel()
            => new CheckBatteryViewModel(_lightbar, _actionRepo, _environmentService);

        public MultiActButtonViewModel CreateMultiActButtonViewModel() => new MultiActButtonViewModel(_actionRepo);

        public FocusWindowViewModel CreateFocusWindowViewModel() => new FocusWindowViewModel(_actionRepo);

        public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActEditorViewModel for Device {device}");
            return new SpecialActEditorViewModel(device, action, _actionRepo);
        }

        public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActionsListViewModel for Device {device}");
            return new SpecialActionsListViewModel(device, _actionRepo, _profileRepo);
        }
    }
}
