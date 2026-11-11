namespace DS4Windows.Core.Settings
{
    /// <summary>Xbox 系コントローラー（XInput で読む）の設定。他の機種と違い、初回の起動では無効（必要な人だけが有効にする）。</summary>
    public class XboxDeviceOptions : DeviceOptionsBase
    {
        public const bool DEFAULT_ENABLE = false;

        public XboxDeviceOptions() : base(DEFAULT_ENABLE, "XboxSupportSettings")
        {
        }
    }
}
