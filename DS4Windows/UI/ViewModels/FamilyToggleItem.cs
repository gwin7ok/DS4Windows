using System;
using System.Reflection;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Services;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 「この機種を使う」のチェックボックス 1 つ（系統ごと）。文言は <see cref="InputDeviceFamilyUiAttribute"/> のキーから、値は系統の設定の有効・無効から得る。
    /// </summary>
    public sealed class FamilyToggleItem
    {
        private readonly DeviceOptionsBase options;

        public FamilyToggleItem(InputDeviceFamily family, DeviceOptionsBase options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            Family = family;
            var ui = typeof(InputDeviceFamily).GetField(family.ToString()).GetCustomAttribute<InputDeviceFamilyUiAttribute>()
                ?? throw new InvalidOperationException("InputDeviceFamily." + family + " に [InputDeviceFamilyUi] がない");
            SupportLabel = LocalizedText.Get(ui.SupportLabelKey);
            FirstLaunchLabel = LocalizedText.Get(ui.FirstLaunchLabelKey);
            FirstLaunchText = LocalizedText.Get(ui.FirstLaunchTextKey);
            HelpUrl = ui.HelpUrl;
        }

        public InputDeviceFamily Family { get; }

        /// <summary>設定画面のチェックボックスの文言。</summary>
        public string SupportLabel { get; }

        public string FirstLaunchLabel { get; }

        public string FirstLaunchText { get; }

        /// <summary>はじめの起動画面の補足のリンク（なければ null）。</summary>
        public string HelpUrl { get; }

        public bool HasHelpUrl => HelpUrl != null;

        public bool Enabled
        {
            get => options.Enabled;
            set => options.Enabled = value;
        }
    }
}
