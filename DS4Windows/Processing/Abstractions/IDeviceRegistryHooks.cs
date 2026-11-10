namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// デバイスの登録簿（<see cref="IDs4DeviceRegistry"/>）が、列挙・初期化の途中で呼び返すフックを結ぶ。
    /// 対応する種類の確認（設定が有効か）、初期化の直後の処理（種類ごとの部品）、排他モードで開けなかったときの昇格の要求を受け持つ。
    /// </summary>
    public interface IDeviceRegistryHooks
    {
        /// <summary>登録簿へフックを結ぶ。列挙の前に、1 回呼ぶ（<c>ControlService</c> のコンストラクターが呼ぶ）。</summary>
        void Attach();
    }
}
