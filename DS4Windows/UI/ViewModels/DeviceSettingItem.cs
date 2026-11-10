using System.Collections.Generic;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Services;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 機種の設定の項目 1 つ（チェックボックスまたは選択肢の一覧）。見せ方は <see cref="SettingUi"/>、値の読み書きは <see cref="ControllerSetting"/> が持つ。
    /// 値は保存と同じ文字列（真偽値は True／False、列挙型は名前）で読み書きするため、機種ごとの型を知らずに画面が作れる。
    /// </summary>
    public sealed class DeviceSettingItem
    {
        private readonly ControllerSetting setting;

        public DeviceSettingItem(ControllerSetting setting)
        {
            this.setting = setting;
            SettingUi ui = setting.Ui;
            Label = LocalizedText.Get(ui.LabelKey);
            Tooltip = ui.TooltipKey == null ? null : LocalizedText.Get(ui.TooltipKey);
            AppliesOnNewConnection = ui.AppliesOnNewConnection;
            Choices = ui.Choices;
        }

        public string Label { get; }

        public string Tooltip { get; }

        public bool AppliesOnNewConnection { get; }

        /// <summary>列挙型の項目の選択肢（真偽値の項目は null）。</summary>
        public IReadOnlyList<SettingChoice> Choices { get; }

        public bool IsChoice => Choices != null;

        public bool IsBool => Choices == null;

        public bool BoolValue
        {
            get => bool.TryParse(setting.Read(), out bool value) && value;
            set => setting.Parse(value.ToString())?.Invoke();
        }

        public string ChoiceValue
        {
            get => setting.Read();
            set => setting.Parse(value)?.Invoke();
        }
    }
}
