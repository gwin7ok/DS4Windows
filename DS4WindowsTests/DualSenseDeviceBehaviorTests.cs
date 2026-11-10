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
    /// DualSense（USB）の読み取りループの特性化テスト。偽の通信路（<see cref="FakeHidTransport"/>）に台本の入力を与え、
    /// 報告される状態の列、電池、出力レポートの書き込み、台本が尽きたあとの切断（<c>Removal</c>）を記録して、期待値（<see cref="Expected"/>）と比べる。
    /// 共通のデバイスへ移す前後で、この記録が変わらないことを移行の合格条件にする。
    /// </summary>
    public class DualSenseDeviceBehaviorTests
    {
        private readonly ITestOutputHelper output;

        public DualSenseDeviceBehaviorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // USB の入力レポート: [0]=ID、[1]=LX、[2]=LY、[5]=L2、[8]=下位ボタン、[28..31]=タイムスタンプ、[53]=電池、[54]=充電
        private static byte[] Frame(byte lx, byte ly, byte l2, byte buttons, uint stamp, byte battery, byte charging)
        {
            var f = new byte[64];
            f[0] = 0x01;
            f[1] = lx;
            f[2] = ly;
            f[5] = l2;
            f[8] = buttons;
            f[28] = (byte)stamp;
            f[29] = (byte)(stamp >> 8);
            f[30] = (byte)(stamp >> 16);
            f[31] = (byte)(stamp >> 24);
            f[53] = battery;
            f[54] = charging;
            return f;
        }

        private static string Snapshot(DS4State s)
        {
            return $"X={s.Cross} Sq={s.Square} LX={s.LX} LY={s.LY} L2={s.L2} Bat={s.Battery} Pkt={s.PacketCounter} " +
                   $"Us={s.totalMicroSec} Ts={s.ds4Timestamp} El={s.elapsedTime:F6}";
        }

        private (string Reports, string Writes, int Battery, bool Removed) Run(VidPidFeatureSet featureSet, params byte[][] frames)
        {
            var transport = new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 48,
                vendorId: 0x054C, productId: 0x0CE6);
            foreach (byte[] frame in frames)
            {
                transport.EnqueueRead(HidReadStatus.Success, frame);
            }

            var device = new DualSenseDevice(transport, "DualSense", featureSet);
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
                Frame(10, 20, 0, 0x08, 3000, 0x05, 0x00),
                Frame(200, 128, 255, 0x28, 6000, 0x08, 0x08),
                Frame(128, 128, 0, 0x08, 9000, 0x0A, 0x08));

            output.WriteLine(result.Reports);
            output.WriteLine(result.Writes);
            output.WriteLine("Battery=" + result.Battery + " Removed=" + result.Removed);
            Assert.True(result.Removed);
            Assert.Equal(Expected, (result.Reports + "--\n" + result.Writes + $"Battery={result.Battery}\n").Replace("\r\n", "\n"));
        }

        // 旧実装（DualSenseDevice）が出した記録そのもの。移行後の実装は、これと同じ記録を出すこと
        private const string Expected =
            "X=False Sq=False LX=10 LY=20 L2=0 Bat=62 Pkt=1 Us=1000 Ts=187 El=0.001000\n" +
            "X=True Sq=False LX=200 LY=128 L2=255 Bat=100 Pkt=2 Us=2000 Ts=375 El=0.001000\n" +
            "X=False Sq=False LX=128 LY=128 L2=0 Bat=100 Pkt=3 Us=3000 Ts=562 El=0.001000\n" +
            "--\n" +
            "interrupt len=48 id=02 b1=0F b2=15 b3=00\n" +
            "interrupt len=48 id=02 b1=0F b2=15 b3=00\n" +
            "Battery=100\n";
    }
}
