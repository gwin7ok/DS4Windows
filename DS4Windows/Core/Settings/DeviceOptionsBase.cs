using System;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 機種ごとの設定（DS4、DualSense、Switch Pro、Joy-Con、DS3）の共通部分: 「この機種を使う」の有効・無効と、その変更の通知。
    /// 既定値は機種ごとに異なる（各クラスの <c>DEFAULT_ENABLE</c>）ため、コンストラクターで受ける。
    /// </summary>
    public abstract class DeviceOptionsBase
    {
        private bool enabled;

        protected DeviceOptionsBase(bool defaultEnabled)
        {
            enabled = defaultEnabled;
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value) return;
                enabled = value;
                EnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler EnabledChanged;
    }
}
