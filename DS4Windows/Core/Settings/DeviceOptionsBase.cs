using System;
using System.Collections.Generic;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 機種の系統ごとの設定（DS4、DualSense、Switch Pro、Joy-Con、DS3）の共通部分: 「この機種を使う」の有効・無効と、その変更の通知。
    /// 既定値は機種ごとに異なる（各クラスの <c>DEFAULT_ENABLE</c>）ため、コンストラクターで受ける。
    /// アプリ設定（AppSettings.xml）の <c>DeviceOptions</c> の下の節の名前と、機種固有の設定項目（<see cref="ExtraSettings"/>）の定義も持つ。
    /// 保存と読み込みは、この定義の並びで行う（<see cref="DS4Windows.Core.Persistence.Dto.InputDeviceOptions"/>）。
    /// </summary>
    public abstract class DeviceOptionsBase
    {
        private bool enabled;
        private IReadOnlyList<ControllerSetting> settings;

        protected DeviceOptionsBase(bool defaultEnabled, string sectionName)
        {
            enabled = defaultEnabled;
            SectionName = sectionName;
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

        /// <summary>アプリ設定の <c>DeviceOptions</c> の下の節の名前（保存形式。例: DS4SupportSettings）。</summary>
        public string SectionName { get; }

        /// <summary>有効・無効の他に保存する、機種固有の設定項目の定義（保存する順）。</summary>
        protected virtual IReadOnlyList<ControllerSetting> ExtraSettings => Array.Empty<ControllerSetting>();

        /// <summary>保存する設定項目の定義（保存する順。先頭は Enabled）。</summary>
        public IReadOnlyList<ControllerSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    var list = new List<ControllerSetting>
                    {
                        ControllerSetting.Bool("Enabled", this, o => o.Enabled, (o, v) => o.Enabled = v),
                    };
                    list.AddRange(ExtraSettings);
                    settings = list;
                }

                return settings;
            }
        }
    }
}
