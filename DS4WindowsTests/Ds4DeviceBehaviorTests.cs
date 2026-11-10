using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS4（USB）の読み取りループの特性化テスト。偽の通信路（<see cref="FakeHidTransport"/>）に台本の入力を与え、
    /// 報告される状態の列、電池、出力レポートの書き込み、台本が尽きたあとの切断（<c>Removal</c>）を記録して、期待値（<see cref="Expected"/>）と比べる。
    /// 共通のデバイスへ移す前後で、この記録が変わらないことを移行の合格条件にする。
    /// </summary>
    public class Ds4DeviceBehaviorTests
    {
        private readonly ITestOutputHelper output;

        public Ds4DeviceBehaviorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // USB の入力レポート: [0]=ID、[1]=LX、[2]=LY、[5]=下位ボタン、[8]=L2、[10..11]=タイムスタンプ、[30]=電池・ケーブル
        private static byte[] Frame(byte lx, byte ly, byte l2, byte buttons, ushort stamp, byte battery)
        {
            var f = new byte[64];
            f[0] = 0x01;
            f[1] = lx;
            f[2] = ly;
            f[5] = buttons;
            f[8] = l2;
            f[10] = (byte)stamp;
            f[11] = (byte)(stamp >> 8);
            f[30] = battery;
            return f;
        }

        private static string Snapshot(DS4State s)
        {
            return $"X={s.Cross} Sq={s.Square} LX={s.LX} LY={s.LY} L2={s.L2} Bat={s.Battery} Pkt={s.PacketCounter} " +
                   $"Us={s.totalMicroSec} Ts={s.ds4Timestamp} El={s.elapsedTime:F6}";
        }

        // BT の入力レポート（547 バイト）: [0]=0x11、[2..]=USB と同じ並び、[74..77]=CRC32（先頭 74 バイトの）
        private static byte[] BtFrame(byte[] usbFrame, bool validCrc = true)
        {
            var f = new byte[547];
            f[0] = 0x11;
            Array.Copy(usbFrame, 0, f, 2, usbFrame.Length);
            uint seed = 2351727372;
            byte[] buffer = f;
            int offset = 0;
            int length = 74;
            uint crc = ~DS4Windows.Input.Crc32Algorithm.CalculateFasterBT78Hash(ref seed, ref buffer, ref offset, ref length);
            if (!validCrc) crc ^= 0xFFFFFFFF;
            f[74] = (byte)crc;
            f[75] = (byte)(crc >> 8);
            f[76] = (byte)(crc >> 16);
            f[77] = (byte)(crc >> 24);
            return f;
        }

        private (string Reports, string Writes, int Battery, bool Removed) Run(VidPidFeatureSet featureSet, params byte[][] frames)
        {
            return RunWith(new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 32,
                vendorId: 0x054C, productId: 0x09CC), featureSet, frames);
        }

        private (string Reports, string Writes, int Battery, bool Removed) RunWith(FakeHidTransport transport, VidPidFeatureSet featureSet, params byte[][] frames)
        {
            foreach (byte[] frame in frames)
            {
                transport.EnqueueRead(HidReadStatus.Success, frame);
            }

            var device = new DS4Device(transport, "DS4", featureSet);
            device.PostInit();

            var reports = new StringBuilder();
            device.Report += (sender, args) => reports.AppendLine(Snapshot(sender.getCurrentStateRef()));
            using var removed = new ManualResetEventSlim();
            device.Removal += (sender, args) => removed.Set();

            device.StartUpdate();
            bool wasRemoved = removed.Wait(5000);
            device.StopUpdate();

            var writes = new StringBuilder();
            foreach (FakeHidTransport.Write w in transport.Writes)
            {
                writes.AppendLine($"{w.Kind} len={w.Data.Length} id={w.Data[0]:X2} b1={w.Data[1]:X2} b2={w.Data[2]:X2} b3={w.Data[3]:X2}");
            }

            return (reports.ToString(), writes.ToString(), device.Battery, wasRemoved);
        }

        [Fact]
        public void UsbScript_ReportsEachFrame_ReadsTheBattery_WritesTheOutput_AndRemovesOnTheEndOfTheScript()
        {
            var result = Run(VidPidFeatureSet.NoGyroCalib,
                Frame(10, 20, 0, 0x08, 100, 0x05),
                Frame(200, 128, 255, 0x28, 200, 0x18),
                Frame(128, 128, 0, 0x08, 300, 0x1A));

            output.WriteLine(result.Reports);
            output.WriteLine(result.Writes);
            output.WriteLine("Battery=" + result.Battery + " Removed=" + result.Removed);
            Assert.True(result.Removed);
            Assert.Equal(Expected, (result.Reports + "--\n" + result.Writes + $"Battery={result.Battery}\n").Replace("\r\n", "\n"));
        }

        [Fact]
        public void BtScript_ReportsTheFramesWithAValidCrc_AndSkipsTheOnesWithABadCrc()
        {
            var transport = new FakeHidTransport(inputReportByteLength: 547, outputReportByteLength: 78,
                vendorId: 0x054C, productId: 0x09CC);
            var result = RunWith(transport, VidPidFeatureSet.NoGyroCalib,
                BtFrame(Frame(10, 20, 0, 0x08, 100, 0x05)),
                BtFrame(Frame(99, 99, 0, 0x08, 150, 0x05), validCrc: false),
                BtFrame(Frame(200, 128, 255, 0x28, 200, 0x18)));

            output.WriteLine(result.Reports);
            output.WriteLine(result.Writes);
            output.WriteLine("Battery=" + result.Battery + " Removed=" + result.Removed);
            Assert.True(result.Removed);
            Assert.Equal(BtExpected, (result.Reports + "--\n" + result.Writes + $"Battery={result.Battery}\n").Replace("\r\n", "\n"));
        }

        [Fact]
        public void DongleScript_ReportsTheFrames_AndRemovesOnTheEndOfTheScript()
        {
            var transport = new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 32, numberFeatureDataIndices: 22,
                vendorId: 0x054C, productId: 0x0BA0);
            var result = RunWith(transport, VidPidFeatureSet.NoGyroCalib,
                Frame(10, 20, 0, 0x08, 100, 0x05),
                Frame(200, 128, 255, 0x28, 200, 0x18));

            output.WriteLine(result.Reports);
            output.WriteLine(result.Writes);
            output.WriteLine("Battery=" + result.Battery + " Removed=" + result.Removed);
            Assert.True(result.Removed);
            Assert.Equal(DongleExpected, (result.Reports + "--\n" + result.Writes + $"Battery={result.Battery}\n").Replace("\r\n", "\n"));
        }

        private const string BtExpected =
            "X=False Sq=False LX=10 LY=20 L2=0 Bat=62 Pkt=1 Us=533 Ts=100 El=0.000533\n" +
            "X=True Sq=False LX=200 LY=128 L2=255 Bat=72 Pkt=2 Us=1066 Ts=200 El=0.000533\n" +
            "--\n" +
            "interrupt len=334 id=11 b1=C0 b2=00 b3=07\n" +
            "interrupt len=334 id=11 b1=C0 b2=00 b3=07\n" +
            "Battery=72\n";
        private const string DongleExpected =
            "X=False Sq=False LX=10 LY=20 L2=0 Bat=62 Pkt=1 Us=533 Ts=100 El=0.000533\n" +
            "X=True Sq=False LX=200 LY=128 L2=255 Bat=72 Pkt=2 Us=1066 Ts=200 El=0.000533\n" +
            "--\n" +
            "interrupt len=32 id=05 b1=07 b2=04 b3=00\n" +
            "Battery=72\n";

        // 旧実装（DS4Device）が出した記録そのもの。移行後の実装は、これと同じ記録を出すこと
        private const string Expected =
            "X=False Sq=False LX=10 LY=20 L2=0 Bat=62 Pkt=1 Us=533 Ts=100 El=0.000533\n" +
            "X=True Sq=False LX=200 LY=128 L2=255 Bat=72 Pkt=2 Us=1066 Ts=200 El=0.000533\n" +
            "X=False Sq=False LX=128 LY=128 L2=0 Bat=90 Pkt=3 Us=1599 Ts=300 El=0.000533\n" +
            "--\n" +
            "interrupt len=32 id=05 b1=07 b2=04 b3=00\n" +
            "Battery=90\n";
    }
}
