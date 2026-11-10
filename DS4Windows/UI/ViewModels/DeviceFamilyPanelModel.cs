using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Services;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 選んだコントローラーの設定パネル: 系統ごとの設定（先頭の「使う」を除く）と、そのコントローラー固有の設定を、順に並べた項目。
    /// 「使う」が無効の系統では、パネルを隠す。新しいデバイスの接続から効く項目があれば、注意書きを出す。
    /// </summary>
    public sealed class DeviceFamilyPanelModel : INotifyPropertyChanged
    {
        private readonly DeviceOptionsBase familyOptions;

        public DeviceFamilyPanelModel(DeviceOptionsBase familyOptions, IReadOnlyList<ControllerSetting> controllerSettings)
        {
            this.familyOptions = familyOptions ?? throw new ArgumentNullException(nameof(familyOptions));
            Items = familyOptions.Settings.Skip(1)
                .Concat(controllerSettings ?? Array.Empty<ControllerSetting>())
                .Where(s => s.Ui != null)
                .Select(s => new DeviceSettingItem(s))
                .ToList();
            ShowNewConnectionNotice = Items.Any(i => i.AppliesOnNewConnection);
            NewConnectionNotice = LocalizedText.Get("ChangeOnNewDeviceConn");
            familyOptions.EnabledChanged += (sender, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Visible)));
        }

        public IReadOnlyList<DeviceSettingItem> Items { get; }

        public bool ShowNewConnectionNotice { get; }

        public string NewConnectionNotice { get; }

        /// <summary>系統の「使う」が有効か（無効のときパネルを隠す）。</summary>
        public bool Visible => familyOptions.Enabled;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
