namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6c（Phase8-Step7-6-Plan.md Q2＝A・Q6＝A）: BatteryCheck の出力が、実機のコントローラー（<c>DS4Device</c>）と、ライトバーの強制色（静的な <c>DS4LightBar</c>）、
    /// 通知（静的な <c>AppLogger.LogToTray</c>）を操作するための窓口。出力アクションが <c>DS4Device</c> と静的な窓口を直接呼ばないようにし、単体テストでは記録用の偽の実装に差し替える。
    /// </summary>
    public interface IBatteryCheckPort
    {
        /// <summary>そのスロットにコントローラーがいるか。</summary>
        bool HasController(int deviceIndex);

        /// <summary>電池残量の通知（旧 <c>Mapping</c> と同じ: 「Controller N: 残量」を <c>AppLogger.LogToTray(…, true)</c> で出す）。</summary>
        void NotifyBattery(int deviceIndex);

        /// <summary>コントローラーの現在のライトバーの色（<c>DS4Device.LightBarColor</c>）。</summary>
        DS4Color GetLightBarColor(int deviceIndex);

        /// <summary>コントローラーの電池残量（<c>DS4Device.Battery</c>）。</summary>
        int GetBattery(int deviceIndex);

        /// <summary>ライトバーの強制色を使うか（<c>DS4LightBar.forcelight</c>）。</summary>
        void SetForceLight(int deviceIndex, bool on);

        /// <summary>ライトバーの強制色（<c>DS4LightBar.forcedColor</c>）。</summary>
        void SetForcedColor(int deviceIndex, DS4Color color);
    }
}
