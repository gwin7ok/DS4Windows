using System;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase9 9-E E-6b（Phase9-E6-Plan.md）: コントローラーをスロットへつなぐときの、プロファイルの設定のデバイスへの反映と、イベントの配線。
    /// 以前は <c>ControlService.CheckProfileOptions</c>／<c>CheckLauchProfileOption</c>／<c>SetupInitialHookEvents</c>／<c>TouchPadOn</c> だった。
    /// <c>ControlService</c> の <c>CheckProfileOptions</c>／<c>TouchPadOn</c> は、外部の呼び出し（BackingStore）があるため、薄い委譲として残す。
    /// </summary>
    public interface IDeviceConnectionSetup
    {
        /// <summary>
        /// プロファイルの設定（出力データ、アイドル切断、BT のポーリング、デバウンス、トラックボール、トリガー効果、振動の自動停止、ライトバー、DualSense の設定）を
        /// デバイスへ反映する。<paramref name="startUp"/> が false のときは、プロファイルの「起動するプログラム」も扱う。
        /// </summary>
        void ApplyProfileOptions(int ind, DS4Device device, Mouse touchPad, bool startUp = false);

        /// <summary>UDP のスムージング、フリックスティック、トリガー効果の変更イベントを、このスロットのデバイスへ配線し直す。</summary>
        void WireInitialHooks(int ind, DS4Device device);

        /// <summary>タッチパッドと 6 軸のイベントを、スロットのマウス（<paramref name="touchPads"/>[ind]）へ配線する。</summary>
        void AttachTouchpad(int ind, DS4Device device, Mouse[] touchPads);
    }
}
