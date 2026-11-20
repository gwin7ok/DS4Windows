namespace DS4Windows.Core.Abstractions
{
    public interface IProfileXmlStore
    {
        bool LoadProfileXml(int deviceIndex, IProfileLoadTarget control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);
        bool SaveProfileXml(int deviceIndex, string profileName);

        /// <summary>
        /// アプリ全体設定（Profiles.xml）を読み込みます（同一XML排他ロック XmlIoLock で保護）。
        /// </summary>
        bool LoadAppSettingsXml();

        /// <summary>
        /// アプリ全体設定（Profiles.xml）を保存します（同一XML排他ロック XmlIoLock で保護）。
        /// </summary>
        bool SaveAppSettingsXml();

        // ---- 接続時のコントローラー固有設定の読込（Global への薄い委譲）----
        bool LoadControllerConfigsForDevice(IConfigurableDevice device);

        // ---- 切断時のコントローラー固有設定の保存（Global への薄い委譲）----
        bool SaveControllerConfigsForDevice(IConfigurableDevice device);
    }
}