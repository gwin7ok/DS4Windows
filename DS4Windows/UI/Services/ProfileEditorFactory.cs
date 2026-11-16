using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// プロファイル編集画面（メイン画面に埋め込む <see cref="ProfileEditor"/>）の生成。<see cref="EditorWindowFactory"/> が窓口として委譲する。
    /// 窓が別の窓を開くための窓口（<c>editors</c>）は、窓口自身を引数で受ける。
    /// </summary>
    public sealed class ProfileEditorFactory
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IControllerReadingsSource _readings;
        private readonly IProfileEditViewModelFactory _viewModelFactory;
        private readonly IActionViewModelFactory _actionViewModelFactory;
        private readonly ProfileEditorBackend _backend;

        public ProfileEditorFactory(IAppSettingsService appSettings, IControllerReadingsSource readings,
            IProfileEditViewModelFactory viewModelFactory, IActionViewModelFactory actionViewModelFactory, ProfileEditorBackend backend)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _readings = readings ?? throw new ArgumentNullException(nameof(readings));
            _actionViewModelFactory = actionViewModelFactory ?? throw new ArgumentNullException(nameof(actionViewModelFactory));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public ProfileEditor CreateProfileEditor(int targetDevice, IEditorWindowFactory editors)
            => new ProfileEditor(targetDevice, new EditorLayoutSettingsAdapter(_appSettings), editors, _backend,
                new ControllerReadingsFeedAdapter(_readings), _viewModelFactory, _actionViewModelFactory);
    }
}
