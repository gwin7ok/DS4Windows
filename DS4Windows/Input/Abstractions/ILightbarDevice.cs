using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// ライトバーの色の計算（<see cref="ILightbarService"/>）が、入力デバイスから読む値と、計算した状態を書く先。
    /// 実装は <c>InputDeviceCore</c>（第 1 層）。第 2 層の計算は、デバイスの具象ではなくこの契約だけを知る。
    /// </summary>
    public interface ILightbarDevice
    {
        bool IsCharging { get; }

        /// <summary>電池残量（％）。</summary>
        int Battery { get; }

        ConnectionType ConnectionType { get; }

        /// <summary>最後に操作があった時刻（UTC）。無操作でのフェードに使う。</summary>
        DateTime LastActive { get; }

        /// <summary>振動の強いほう（左・重い）の強さ。</summary>
        byte LeftHeavySlowRumble { get; }

        /// <summary>いま機器に設定してあるライトバーの点灯時間（点滅の制御用）。</summary>
        byte LightBarOnDuration { get; }

        void SetLightbarState(ref DS4LightbarState lightState);
    }
}
