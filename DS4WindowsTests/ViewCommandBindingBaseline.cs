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
            ["BindingWindow.xaml"] = 6,
            ["ChangelogWindow.xaml"] = 1,
            ["ControllerRegisterOptionsWindow.xaml"] = 1,
            ["DupBox.xaml"] = 2,
            ["FirstLaunchUtilWindow.xaml"] = 2,
            ["GeneralSettingsPanel.xaml"] = 2,
            ["GyroSettingsPanel.xaml"] = 11,
            ["LanguagePackControl.xaml"] = 1,
            ["LanguageSelectDialog.xaml"] = 3,
            ["LightbarSettingsPanel.xaml"] = 5,
            ["LogMessageDisplay.xaml"] = 1,
            ["MainWindow.xaml"] = 10,
            ["OutputSettingsPanel.xaml"] = 2,
            ["OutputSlotManagerControl.xaml"] = 4,
            ["PluginOutDevWindow.xaml"] = 2,
            ["PresetOptionWindow.xaml"] = 5,
            ["ProfileEditor.xaml"] = 23,
            ["RecordBox.xaml"] = 28,
            ["RenameProfileWindow.xaml"] = 3,
            ["SaveWhere.xaml"] = 2,
            ["SixAxisSettingsPanel.xaml"] = 2,
            ["StickCalibrationWindow.xaml"] = 2,
            ["StickSideSettingsView.xaml"] = 2,
            ["TouchpadSettingsPanel.xaml"] = 32,
            ["TriggerSettingsPanel.xaml"] = 2,
            ["UpdaterWindow.xaml"] = 4,
            ["WelcomeDialog.xaml"] = 6,
            ["WindowTargetEditor.xaml"] = 5,
        };

        /// <summary>画面の <c>Command</c> のバインディングの総数の下限（減らさない）。</summary>
        public const int CommandBindingFloor = 58;
    }
}
