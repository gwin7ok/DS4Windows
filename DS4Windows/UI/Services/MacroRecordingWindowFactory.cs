using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// マクロ記録の窓（<see cref="RecordBox"/>、<see cref="RecordBoxWindow"/>）の生成。<see cref="EditorWindowFactory"/> が窓口として委譲する。
    /// 窓が別の窓を開くための窓口（<paramref name="editors"/>）は、窓口自身を引数で受ける（この工場が窓口を持つと循環するため）。
    /// </summary>
    public sealed class MacroRecordingWindowFactory
    {
        private readonly IMacroRecordingState _macroRecording;
        private readonly IPathService _pathService;
        private readonly IProfileEditViewModelFactory _viewModelFactory;
        private readonly IWindowSystem _windowSystem;   // 画面全体のマウス入力ではなく、ドロップしたウィンドウの取得（FocusWindow の対象）
        private readonly IMouseInputHook _mouseHook;   // 記録で、画面全体のマウス入力（移動・ボタン）を監視する

        public MacroRecordingWindowFactory(IMacroRecordingState macroRecording, IPathService pathService,
            IProfileEditViewModelFactory viewModelFactory, IWindowSystem windowSystem, IMouseInputHook mouseHook)
        {
            _macroRecording = macroRecording ?? throw new ArgumentNullException(nameof(macroRecording));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            _mouseHook = mouseHook ?? throw new ArgumentNullException(nameof(mouseHook));
        }

        public RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, int targetDevice,
            bool showscan, bool repeatable, bool showPlaybackOptions)
            => new RecordBox(deviceNum, controlSettings, targetDevice, showscan, repeatable, showPlaybackOptions,
                _macroRecording, _pathService, _viewModelFactory, _windowSystem, _mouseHook);

        public RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable, bool showPlaybackOptions, IEditorWindowFactory editors)
            => new RecordBoxWindow(deviceNum, settings, targetDevice, editors, repeatable, showPlaybackOptions);
    }
}
