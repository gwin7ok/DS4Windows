using System.Collections.Generic;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// Xbox 系コントローラー（XInput で読む）の設定。他の機種と違い、初回の起動では無効（必要な人だけが有効にする）。
    /// 「USB の親を隠す」は、HidHide で元の XInput の機器を他のアプリから隠す（二重入力を防ぐ）。既定はオフ（利用者の HidHide の登録を、勝手に変えない）。
    /// </summary>
    public class XboxDeviceOptions : DeviceOptionsBase
    {
        public const bool DEFAULT_ENABLE = false;

        private IReadOnlyList<ControllerSetting> extraSettings;

        public XboxDeviceOptions() : base(DEFAULT_ENABLE, "XboxSupportSettings")
        {
        }

        /// <summary>HidHide で、Xbox コントローラーの USB の親（XInput の経路）を隠すか。</summary>
        public bool HideUsbParent { get; set; }

        protected override IReadOnlyList<ControllerSetting> ExtraSettings => extraSettings ?? (extraSettings = new[]
        {
            ControllerSetting.Bool("HideUsbParent", this, o => o.HideUsbParent, (o, v) => o.HideUsbParent = v)
                .WithUi("XboxHideUsbParent", "XboxHideUsbParentTip", appliesOnNewConnection: true),
        });
    }
}
