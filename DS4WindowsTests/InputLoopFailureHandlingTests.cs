using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS4・DualSense の Bluetooth の読み取りの失敗（CRC の不一致が続く、読み取りの失敗）の扱い。
    /// 偽の通信路に台本を与え、報告されたフレームの数と切断（<c>Removal</c>）の回数を固定する。
    /// 読み取りの共通の手順（CRC の検証・停止）を基底へ寄せる前後で、この記録が変わらないことを守る。
    /// </summary>
    public class InputLoopFailureHandlingTests
    {
        private const int BadCrcLimit = 10;   // この数まで連続して不一致を許し、次の不一致で切断する

        private static byte[] Ds4BtFrame(bool validCrc)
        {
            var f = new byte[547];
            f[0] = 0x11;
            return WithCrc(f, validCrc);
        }

        private static byte[] DualSenseBtFrame(bool validCrc)
        {
            var f = new byte[78];
            f[0] = 0x31;
            return WithCrc(f, validCrc);
        }

        private static byte[] WithCrc(byte[] f, bool validCrc)
        {
            uint seed = 2351727372;
            int offset = 0;
            int length = 74;
            uint crc = ~Crc32Algorithm.CalculateFasterBT78Hash(ref seed, ref f, ref offset, ref length);
            if (!validCrc) crc ^= 0xFFFFFFFF;
            f[74] = (byte)crc;
            f[75] = (byte)(crc >> 8);
            f[76] = (byte)(crc >> 16);
            f[77] = (byte)(crc >> 24);
            return f;
        }

        private static (int Reports, int Removals) RunDs4(IEnumerable<(HidReadStatus Status, byte[] Data)> script)
        {
            var transport = new FakeHidTransport(inputReportByteLength: 547, outputReportByteLength: 78, vendorId: 0x054C, productId: 0x09CC);
            return Run(transport, script, (t, fs) => new DualShock4Device(t, "DS4", fs));
        }

        private static (int Reports, int Removals) RunDualSense(IEnumerable<(HidReadStatus Status, byte[] Data)> script)
        {
            var transport = new FakeHidTransport(inputReportByteLength: 78, outputReportByteLength: 78, vendorId: 0x054C, productId: 0x0CE6);
            return Run(transport, script, (t, fs) => new DualSenseDevice(t, "DualSense", fs));
        }

        private static (int Reports, int Removals) Run(FakeHidTransport transport, IEnumerable<(HidReadStatus Status, byte[] Data)> script,
            Func<FakeHidTransport, VidPidFeatureSet, InputDeviceCore> create)
        {
            foreach (var step in script) transport.EnqueueRead(step.Status, step.Data);

            InputDeviceCore device = create(transport, VidPidFeatureSet.NoGyroCalib);
            device.PostInit();

            int reports = 0;
            int removals = 0;
            using var removed = new ManualResetEventSlim();
            device.Report += (sender, args) => Interlocked.Increment(ref reports);
            device.Removal += (sender, args) => { Interlocked.Increment(ref removals); removed.Set(); };

            device.StartUpdate();
            removed.Wait(5000);
            Thread.Sleep(100);   // 切断の通知が二重に出ないことを見るため、少し待つ
            device.StopUpdate();
            return (reports, removals);
        }

        private static IEnumerable<(HidReadStatus, byte[])> Frames(int count, bool valid, Func<bool, byte[]> make)
        {
            for (int i = 0; i < count; i++) yield return (HidReadStatus.Success, make(valid));
        }

        private static IEnumerable<(HidReadStatus, byte[])> Concat(params IEnumerable<(HidReadStatus, byte[])>[] parts)
        {
            foreach (var p in parts) foreach (var s in p) yield return s;
        }

        [Fact]
        public void Ds4_Bt_TheLimitOfBadCrcFrames_AreSkipped_AndTheNextOneDisconnectsOnce()
        {
            var ok = RunDs4(Concat(Frames(BadCrcLimit, false, Ds4BtFrame), Frames(1, true, Ds4BtFrame)));
            Assert.Equal(1, ok.Reports);

            var cut = RunDs4(Concat(Frames(BadCrcLimit + 1, false, Ds4BtFrame), Frames(3, true, Ds4BtFrame)));
            Assert.Equal(0, cut.Reports);
            Assert.Equal(1, cut.Removals);
        }

        [Fact]
        public void DualSense_Bt_TheLimitOfBadCrcFrames_AreSkipped_AndTheNextOneDisconnectsOnce()
        {
            var ok = RunDualSense(Concat(Frames(BadCrcLimit, false, DualSenseBtFrame), Frames(1, true, DualSenseBtFrame)));
            Assert.Equal(1, ok.Reports);

            var cut = RunDualSense(Concat(Frames(BadCrcLimit + 1, false, DualSenseBtFrame), Frames(3, true, DualSenseBtFrame)));
            Assert.Equal(0, cut.Reports);
            Assert.Equal(1, cut.Removals);
        }

        [Fact]
        public void Ds4_Bt_AGoodFrameResetsTheBadCrcCount()
        {
            var r = RunDs4(Concat(Frames(BadCrcLimit, false, Ds4BtFrame), Frames(1, true, Ds4BtFrame), Frames(BadCrcLimit, false, Ds4BtFrame), Frames(1, true, Ds4BtFrame)));
            Assert.Equal(2, r.Reports);
        }

        [Fact]
        public void DualSense_Bt_AGoodFrameResetsTheBadCrcCount()
        {
            var r = RunDualSense(Concat(Frames(BadCrcLimit, false, DualSenseBtFrame), Frames(1, true, DualSenseBtFrame), Frames(BadCrcLimit, false, DualSenseBtFrame), Frames(1, true, DualSenseBtFrame)));
            Assert.Equal(2, r.Reports);
        }

        [Theory]
        [InlineData(HidReadStatus.ReadError)]
        [InlineData(HidReadStatus.WaitTimedOut)]
        public void Ds4_Bt_AReadFailure_DisconnectsOnce_AndLaterFramesAreNotReported(HidReadStatus failure)
        {
            var r = RunDs4(Concat(Frames(1, true, Ds4BtFrame), new[] { (failure, (byte[])null) }, Frames(2, true, Ds4BtFrame)));
            Assert.Equal(1, r.Reports);
            Assert.Equal(1, r.Removals);
        }

        [Theory]
        [InlineData(HidReadStatus.ReadError)]
        [InlineData(HidReadStatus.WaitTimedOut)]
        public void DualSense_Bt_AReadFailure_DisconnectsOnce_AndLaterFramesAreNotReported(HidReadStatus failure)
        {
            var r = RunDualSense(Concat(Frames(1, true, DualSenseBtFrame), new[] { (failure, (byte[])null) }, Frames(2, true, DualSenseBtFrame)));
            Assert.Equal(1, r.Reports);
            Assert.Equal(1, r.Removals);
        }
    }
}
