using System;
using System.Collections.Generic;
using System.Reflection;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4WindowsTests
{
    /// <summary>
    /// テスト用の <see cref="IHidTransport"/>。読み取りの結果（状態とバイト列）を台本の順に返し、書き込み・特徴レポートの読み取りを記録する。
    /// 台本が尽きたら <see cref="HidReadStatus.NotConnected"/> を返す（切断として扱われる）。
    /// </summary>
    internal sealed class FakeHidTransport : IHidTransport
    {
        public sealed class Write
        {
            public string Kind;
            public byte[] Data;
        }

        private readonly Queue<(HidReadStatus Status, byte[] Data)> _reads = new Queue<(HidReadStatus, byte[])>();
        private readonly object _lock = new object();

        public FakeHidTransport(short inputReportByteLength = 64, short outputReportByteLength = 32, short numberFeatureDataIndices = 0,
            ushort vendorId = 0x054C, ushort productId = 0x09CC)
        {
            // 容量・属性の型は internal（OS の構造体を受ける）のため、リフレクションで作る。
            var asm = typeof(HidDeviceCapabilities).Assembly;
            var capsType = asm.GetType("DS4Windows.Core.Platform.NativeMethods+HIDP_CAPS", throwOnError: true);
            object caps = Activator.CreateInstance(capsType);
            SetField(capsType, caps, "InputReportByteLength", inputReportByteLength);
            SetField(capsType, caps, "OutputReportByteLength", outputReportByteLength);
            SetField(capsType, caps, "NumberFeatureDataIndices", numberFeatureDataIndices);
            SetField(capsType, caps, "Reserved", new short[17]);
            Capabilities = (HidDeviceCapabilities)Activator.CreateInstance(typeof(HidDeviceCapabilities),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { caps }, null);

            var attrType = asm.GetType("DS4Windows.Core.Platform.NativeMethods+HIDD_ATTRIBUTES", throwOnError: true);
            object attr = Activator.CreateInstance(attrType);
            SetField(attrType, attr, "VendorID", vendorId);
            SetField(attrType, attr, "ProductID", productId);
            SetField(attrType, attr, "VersionNumber", (ushort)0x100);
            Attributes = (HidDeviceAttributes)Activator.CreateInstance(typeof(HidDeviceAttributes),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { attr }, null);
        }

        private static void SetField(Type type, object boxed, string name, object value)
        {
            var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // フィールドの型（short／ushort／int）に値を合わせる
            if (!(value is Array)) value = Convert.ChangeType(value, f.FieldType);
            f.SetValue(boxed, value);
        }

        public List<Write> Writes { get; } = new List<Write>();
        public int NumInputBuffers { get; private set; } = -1;
        public int CancelCount { get; private set; }
        public int ReadCount { get; private set; }
        public string Serial { get; set; } = "00:11:22:33:44:55";
        /// <summary>true にすると、出力レポートの書き込みが失敗（false）を返す。</summary>
        public bool FailWrites { get; set; }
        /// <summary>特徴レポートを読んだときに、バッファへ入れるバイト列（先頭のレポート ID は残す）。</summary>
        public byte[] FeatureData { get; set; }

        public void EnqueueRead(HidReadStatus status, byte[] data = null)
        {
            lock (_lock) _reads.Enqueue((status, data));
        }

        public bool IsOpen { get; private set; } = true;
        public bool IsExclusive { get; set; }
        public bool IsConnected => true;
        public string Description => "Fake HID";
        public HidDeviceCapabilities Capabilities { get; }
        public HidDeviceAttributes Attributes { get; }
        public string DevicePath { get; set; } = @"\\?\hid#fake";
        public string ParentPath => null;

        public void OpenDevice(bool exclusive) { IsOpen = true; IsExclusive = exclusive; }
        public void CloseDevice() { IsOpen = false; }
        public void CancelIO() { CancelCount++; }
        public void Dispose() { CancelIO(); CloseDevice(); }

        public HidReadStatus ReadFile(Span<byte> inputBuffer, uint timeout = uint.MaxValue)
        {
            ReadCount++;
            (HidReadStatus Status, byte[] Data) next;
            lock (_lock)
            {
                if (_reads.Count == 0) return HidReadStatus.NotConnected;
                next = _reads.Dequeue();
            }

            if (next.Data != null)
                next.Data.AsSpan(0, Math.Min(next.Data.Length, inputBuffer.Length)).CopyTo(inputBuffer);
            return next.Status;
        }

        public bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout)
        {
            Writes.Add(new Write { Kind = "interrupt", Data = (byte[])outputBuffer.Clone() });
            return !FailWrites;
        }

        public bool WriteOutputReportViaControl(byte[] outputBuffer)
        {
            Writes.Add(new Write { Kind = "control", Data = (byte[])outputBuffer.Clone() });
            return !FailWrites;
        }

        public bool WriteFeatureReport(byte[] data)
        {
            Writes.Add(new Write { Kind = "feature", Data = (byte[])data.Clone() });
            return !FailWrites;
        }

        public bool readFeatureData(byte[] inputBuffer)
        {
            if (FeatureData != null)
                Array.Copy(FeatureData, 0, inputBuffer, 0, Math.Min(FeatureData.Length, inputBuffer.Length));
            return true;
        }

        public void resetSerial() { }
        /// <summary>最後に <see cref="ReadSerial"/> が受けた特徴レポート番号（読んでいなければ -1）。</summary>
        public int LastSerialFeatureId { get; private set; } = -1;
        /// <summary>擬似のシリアル（<see cref="GenerateFakeHwSerial"/> の戻り値）。</summary>
        public string FakeHwSerial { get; set; } = "FA:KE:00:00:00:01";
        public string ReadSerial(byte featureID = 18) { LastSerialFeatureId = featureID; return Serial; }
        public string GenerateFakeHwSerial() => FakeHwSerial;
        public void SetNumInputBuffers(int count) { NumInputBuffers = count; }
    }
}
