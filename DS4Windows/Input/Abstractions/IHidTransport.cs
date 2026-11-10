using System;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// HID 機器との通信（開閉、入力レポートの読み取り、出力レポート・特徴レポートの書き込み、シリアルの読み取り）の窓口。
    /// <c>DS4Device</c> 系は、OS の具象（<see cref="HidDevice"/>）ではなく、この契約を通して通信する。
    /// 実装は <see cref="HidDevice"/>（Windows の HID API）。テストでは、読み取りの結果と書き込みの記録を台本で返す偽物に替えられる。
    /// </summary>
    public interface IHidTransport : IDisposable
    {
        bool IsOpen { get; }
        bool IsExclusive { get; }
        bool IsConnected { get; }
        string Description { get; }
        HidDeviceCapabilities Capabilities { get; }
        HidDeviceAttributes Attributes { get; }
        string DevicePath { get; }
        string ParentPath { get; }

        void OpenDevice(bool exclusive);
        void CloseDevice();

        /// <summary>保留中の読み取りを取り消す（切断・停止のとき）。</summary>
        void CancelIO();

        /// <summary>入力レポートを読む。<paramref name="timeout"/> はミリ秒（既定は無期限）。</summary>
        HidReadStatus ReadFile(Span<byte> inputBuffer, uint timeout = uint.MaxValue);

        /// <summary>出力レポートを、割り込み転送で書く。</summary>
        bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout);

        /// <summary>出力レポートを、コントロール転送で書く。</summary>
        bool WriteOutputReportViaControl(byte[] outputBuffer);

        bool WriteFeatureReport(byte[] data);

        /// <summary>特徴レポートを読む（先頭にレポート ID を入れて渡す）。</summary>
        bool readFeatureData(byte[] inputBuffer);

        void resetSerial();
        string ReadSerial(byte featureID = 18);
        string GenerateFakeHwSerial();

        /// <summary>OS 側の入力バッファの数を設定する（遅延を減らすため、読み取りの開始前に呼ぶ）。</summary>
        void SetNumInputBuffers(int count);
    }
}
