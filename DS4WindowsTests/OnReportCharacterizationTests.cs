using DS4Windows.Core.Model;
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
    [Collection(SharedHostStateCollection.Name)]
    public class OnReportCharacterizationTests
    {
        private const int Slot = 0;
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly ITestOutputHelper _output;

        public OnReportCharacterizationTests(ITestOutputHelper output)
        {
            _output = output;
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.InitializeRuntime();   // 起動時の取り付け（TempStates などは DeviceSlotState が作る）
        }

        /// <summary>レポート処理が読む、保護フィールドを持つテスト用デバイス（受信した 1 回分の入力［cState］を指定できる）。</summary>
        private static TestDs4Device NewDevice(Action<ControllerState> fill = null)
        {
            TestDs4Device d = TestDs4Device.Create();
            var cState = new ControllerState();
            fill?.Invoke(cState);
            Set(d, "cState", cState);
            Set(d, "pState", new ControllerState());
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
        private static void Invoke(InputDeviceCore device, int ind)
        {
            TestHost.GetService<IInputReportProcessor>().OnReport(device, EventArgs.Empty, ind);
        }

        private static ControllerState Current(int ind) => TestHost.GetService<IDeviceSlotState>().CurrentStates[ind];

        [Fact]
        public void Index_MinusOne_DoesNothing()
        {
            InputDeviceCore d = NewDevice(s => s.LX = 10);
            Invoke(d, -1);   // 例外なし
        }

        [Fact]
        public void ThePrimaryDevice_CopiesTheRawInput_IntoTheSlotsCurrentState()
        {
            InputDeviceCore d = NewDevice(s => { s.LX = 200; s.LY = 40; s.R2 = 77; });
            Invoke(d, Slot);

            ControllerState current = Current(Slot);
            Assert.Equal(200, current.LX);
            Assert.Equal(40, current.LY);
            Assert.Equal(77, current.R2);
        }

        [Fact]
        public void TheProcessingDelay_IsRecorded_OnlyWhileMeasuring()
        {
            var control = TestHost.GetService<IInputReportProcessor>();
            InputDeviceCore d = NewDevice(s => s.LX = 128);
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
            var control = TestHost.GetService<IInputReportProcessor>();
            InputDeviceCore d = NewDevice(s => s.LX = 99);
            control.RecordingMacro = true;
            try
            {
                Invoke(d, Slot);
                Assert.Equal(99, Current(Slot).LX);
            }
            finally
            {
                control.RecordingMacro = false;
            }
        }

        [Fact]
        public void TheReportPath_AllocatesNothingPerReport()
        {
            InputDeviceCore d = NewDevice(s => { s.LX = 130; s.LY = 120; });
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
