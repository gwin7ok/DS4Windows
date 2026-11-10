using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// 入力デバイスの種類ごとのカートリッジ（第 1 層）。その種類が扱う VID／PID の表と、デバイスの生成を持つ。
    /// 機種（系統）を足すときは、デバイスの実装とこのカートリッジ（と第 2 層の接続フック）を作り、<c>AddInputDevice&lt;TCartridge, TModule&gt;()</c> で 1 行登録する。
    /// </summary>
    public interface IInputDeviceCartridge
    {
        /// <summary>この種類が扱う VID／PID の表（名前、種類、機能フラグ、接続方式の判定を含む）。</summary>
        IReadOnlyList<VidPidInfo> KnownDevices { get; }

        /// <summary>このカートリッジが扱う系統。系統の種類の対応（<see cref="InputDeviceTypeInfo"/>）から、<see cref="Handles"/> が決まる。</summary>
        InputDeviceFamily Family { get; }

        /// <summary>この種類のデバイスをこのカートリッジで作れるか（既定: 種類が <see cref="Family"/> に属するか）。</summary>
        bool Handles(InputDeviceType type) => InputDeviceTypeInfo.IsFamily(type, Family);

        /// <summary>接続時に、機器を一意に見分けるシリアルを読む（機器を開いた後、生成の前に呼ぶ）。読み方は種類ごとに違う。</summary>
        string ReadSerial(VidPidInfo info, IHidTransport hidDevice);

        /// <summary><paramref name="type"/> のデバイスを作る。<see cref="Handles"/> が true の種類だけ渡す。</summary>
        DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet);
    }
}
