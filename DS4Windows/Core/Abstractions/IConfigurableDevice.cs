using System;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// プロファイルの読み込み・設定の保存・設定値の参照（<c>Core</c>）が、接続中の入力デバイスに対して必要とする操作だけを切り出した狭い契約。
    /// <c>Core</c> が第 1 層の具象（<c>InputDeviceCore</c>）を参照しないために置く。実装するのは <c>InputDeviceCore</c>（と、その派生）だけ。
    /// </summary>
    public interface IConfigurableDevice
    {
        /// <summary>MAC アドレス（コントローラー固有の設定の保存先のキー）。</summary>
        string getMacAddress();

        /// <summary>デバイスの種類（設定の保存先の属性、DualSense かの判定）。</summary>
        InputDeviceType DeviceType { get; }

        /// <summary>機種固有の設定の入れ物（なければ null）。</summary>
        ControllerOptionsStore OptionsStore { get; }

        bool isSynced();

        bool IsAlive();

        /// <summary>デバイスのイベントスレッドで実行する処理を積む。</summary>
        void queueEvent(Action act);

        /// <summary>ミリ秒。0 以下で自動停止を無効にする。</summary>
        int RumbleAutostopTime { get; set; }
    }
}
