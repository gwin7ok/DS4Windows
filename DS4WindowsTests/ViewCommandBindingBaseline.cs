#nullable enable
using System.Collections.Generic;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="ViewCommandBindingRatchetTests"/> の基準値。画面（XAML）ごとのイベント属性（<c>Click="…"</c> など）の数。
    /// 減らしたら、同じコミットでここも更新する（増やす方向の更新は、是正の代わりにしない）。
    /// </summary>
    internal static class ViewCommandBindingBaseline
    {
        public static readonly Dictionary<string, int> EventAttributes = new Dictionary<string, int>
        {
            ["AssignmentEditor.xaml"] = 5,
            ["AutoProfiles.xaml"] = 2,
            ["BindingWindow.xaml"] = 2,
            ["ChangelogWindow.xaml"] = 1,
            ["ControllerRegisterOptionsWindow.xaml"] = 1,
            ["FirstLaunchUtilWindow.xaml"] = 2,
            ["GyroSettingsPanel.xaml"] = 3,
            ["LanguagePackControl.xaml"] = 1,
            ["LanguageSelectDialog.xaml"] = 3,
            ["LightbarSettingsPanel.xaml"] = 5,
            ["LogMessageDisplay.xaml"] = 1,
            ["MainWindow.xaml"] = 10,
            ["OutputSlotManagerControl.xaml"] = 1,
            ["PresetOptionWindow.xaml"] = 1,
            ["ProfileEditor.xaml"] = 7,
            ["RecordBox.xaml"] = 16,
            ["RenameProfileWindow.xaml"] = 1,
            ["StickCalibrationWindow.xaml"] = 2,
            ["TouchpadSettingsPanel.xaml"] = 2,
            ["UpdaterWindow.xaml"] = 1,
            ["WindowTargetEditor.xaml"] = 3,
        };

        /// <summary>画面の <c>Command</c> のバインディングの総数の下限（減らさない）。</summary>
        public const int CommandBindingFloor = 257;
    }
}
