using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// 入力機種ごとの「プロトコル」: 共通のデバイス（<see cref="DS4Windows.Input.Devices.ProtocolDevice"/>）が使う、機種固有の取り決めだけを持つ契約。
    /// 読み取りのループ、切断、電池の更新、出力の書き込みの時機は共通のデバイスが受け持ち、プロトコルは、バイト列との変換だけを書く。
    /// 新しい単純な機種（ボタン・スティック・トリガーと、任意の電池・出力）は、プロトコルとカートリッジを書けば足せる。
    /// ジャイロ・タッチパッド・複数の接続方式を持つ高機能な機種は、従来どおり <c>InputDeviceCore</c> の派生で書く。
    /// </summary>
    public interface IDeviceProtocol
    {
        /// <summary>このプロトコルが扱うデバイスの種類。</summary>
        InputDeviceType DeviceType { get; }

        /// <summary>接続方式（USB／Bluetooth）の判定。</summary>
        ConnectionType DetermineConnectionType(IHidTransport hidDevice);

        /// <summary>
        /// Bluetooth 接続のとき、機器の Bluetooth アドレス（<c>AA:BB:CC:DD:EE:FF</c>）を返す。取れない機種は null（既定。擬似のシリアルを使い、Bluetooth の切断は使えない）。
        /// 切断の手順そのものは、DS4 などと共通の <c>InputDeviceCore.DisconnectBT</c> で、アドレスだけをここで教える。
        /// </summary>
        string TryGetBluetoothAddress(IHidTransport hidDevice) => null;

        /// <summary>入力レポートの解析（状態を持たない）。</summary>
        IInputReportParser Parser { get; }

        /// <summary>入力レポートから電池の残量（0〜100）と充電中かを読む。電池の情報がない機種は false を返す（残量は 99% 固定になる）。</summary>
        bool TryReadBattery(ReadOnlySpan<byte> report, out int percent, out bool charging);

        /// <summary>
        /// 出力（振動・ライトバーなど）の状態を、出力レポートへ組み立てる。出力を持たない機種は false を返す（書き込みをしない）。
        /// <paramref name="report"/> は、機器の出力レポートの長さの配列で、毎回同じものを渡す（割り当てを避ける）。
        /// </summary>
        bool TryBuildOutputReport(DS4HapticState state, int batteryPercent, byte[] report);
    }
}
