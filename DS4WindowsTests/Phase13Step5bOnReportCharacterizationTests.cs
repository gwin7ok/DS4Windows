using System;
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5b 5b-a: 入力レポートの処理（旧 <c>ControlService.On_Report</c>。5b-c で <c>InputReportProcessor</c> へ移した）の特性テスト。
    /// 移す前の挙動を固定し、移した後も同じ結果になることを確かめる。入口は <see cref="Invoke"/> に集約している
    /// （5b-c で、呼び出し先を <c>IInputReportProcessor.OnReport</c> に替えた）。
    /// デバイスは <see cref="TestDs4Device"/>（実機の HID なし）に、レポート処理が読む保護フィールドをリフレクションで与えて使う。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step5bOnReportCharacterizationTests
    {
        private const int Slot = 0;
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly ITestOutputHelper _output;

        public Phase13Step5bOnReportCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.GetService<ControlService>();   // TempStates／PreviousStates は ControlService のコンストラクターが作る（本番では起動時に必ず構築される）
        }

        /// <summary>レポート処理が読む、保護フィールドを持つテスト用デバイス（受信した 1 回分の入力［cState］を指定できる）。</summary>
        private static TestDs4Device NewDevice(Action<DS4State> fill = null, bool primary = true, bool outputMapGyro = true)
        {
            TestDs4Device d = TestDs4Device.Create();
            var cState = new DS4State();
            fill?.Invoke(cState);
            Set(d, "cState", cState);
            Set(d, "pState", new DS4State());
            Set(d, "jointState", new DS4State());
            Set(d, "jointPreviousState", new DS4State());
            Set(d, "primaryDevice", primary);
            Set(d, "outputMapGyro", outputMapGyro);
            Set(d, "jointDeviceSlotNumber", DS4Device.DEFAULT_JOINT_SLOT_NUMBER);
            d.firstReport = false;
            d.firstActive = DateTime.UtcNow;
            return d;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo f = null;
            for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
                f = t.GetField(name, All | BindingFlags.DeclaredOnly);
            Assert.True(f != null, name + " が見つからない");
            f.SetValue(target, value);
        }

        /// <summary>入口。5b-c で、ControlService.On_Report（リフレクション）から IInputReportProcessor.OnReport の直接呼び出しに替えた。</summary>
        private static void Invoke(DS4Device device, int ind)
        {
            TestHost.GetService<IInputReportProcessor>().OnReport(device, EventArgs.Empty, ind);
        }

        private static DS4State Current(int ind) => TestHost.GetService<IDeviceSlotState>().CurrentStates[ind];

        [Fact]
        public void Index_MinusOne_DoesNothing()
        {
            DS4Device d = NewDevice(s => s.LX = 10);
            Invoke(d, -1);   // 例外なし
        }

        [Fact]
        public void ThePrimaryDevice_CopiesTheRawInput_IntoTheSlotsCurrentState()
        {
            DS4Device d = NewDevice(s => { s.LX = 200; s.LY = 40; s.R2 = 77; });
            Invoke(d, Slot);

            DS4State current = Current(Slot);
            Assert.Equal(200, current.LX);
            Assert.Equal(40, current.LY);
            Assert.Equal(77, current.R2);
        }

        [Fact]
        public void ANonPrimaryDevice_WithoutGyroOutput_CopiesTheMotion_AndSkipsTheMapping()
        {
            DS4Device d = NewDevice(s => s.LX = 150, primary: false, outputMapGyro: false);
            d.GetRawCurrentStateRef().Motion.accelXG = 0.25;

            Invoke(d, Slot);

            Assert.Equal(150, Current(Slot).LX);
            Assert.Equal(0.25, Current(Slot).Motion.accelXG);
        }

        [Fact]
        public void TheProcessingDelay_IsRecorded_OnlyWhileMeasuring()
        {
            var control = TestHost.GetService<ControlService>();
            DS4Device d = NewDevice(s => s.LX = 128);
            d.lastInputReportTimestamp = Stopwatch.GetTimestamp() - Stopwatch.Frequency / 100;   // 約 10 ms 前に受信した

            // 遅延の記録は、仮想コントローラーへ出力する経路（DInput 専用でない）でだけ行われる
            var settings = TestHost.GetService<IProfileSettingsService>();
            bool oldDInputOnly = settings.UseDInputOnlyArray[Slot];
            settings.UseDInputOnlyArray[Slot] = false;

            control.IsMeasuringProcessingDelay = false;
            control.ProcessingDelayMs[Slot] = 0;
            try
            {
                Invoke(d, Slot);
                Assert.Equal(0.0, control.GetProcessingDelay(Slot));

                control.IsMeasuringProcessingDelay = true;
                Invoke(d, Slot);
                Assert.True(control.GetProcessingDelay(Slot) >= 9.0, "計測中は、受信からの経過時間が記録される");
            }
            finally
            {
                control.IsMeasuringProcessingDelay = false;
                settings.UseDInputOnlyArray[Slot] = oldDInputOnly;
            }
        }

        [Fact]
        public void RecordingMacro_DoesNotStopTheReportProcessing()
        {
            var control = TestHost.GetService<ControlService>();
            DS4Device d = NewDevice(s => s.LX = 99);
            control.recordingMacro = true;
            try
            {
                Invoke(d, Slot);
                Assert.Equal(99, Current(Slot).LX);
            }
            finally
            {
                control.recordingMacro = false;
            }
        }

        [Fact]
        public void TheReportPath_AllocatesNothingPerReport()
        {
            DS4Device d = NewDevice(s => { s.LX = 130; s.LY = 120; });
            for (int i = 0; i < 2000; i++) Invoke(d, Slot);   // ウォームアップ

            const int n = 2000;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < n; i++) Invoke(d, Slot);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            _output.WriteLine($"OnReport の割り当て: {allocated} bytes / {n} 回");
            Assert.Equal(0, allocated);   // 合格基準（Phase13-Step5b-Plan §6）: 1 レポートあたりの割り当ては 0
        }
    }
}
