using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 読み取り表示（<see cref="IControllerReadingsSource"/>）とマクロ記録（<see cref="IMacroRecordingState"/>）の窓口の振る舞い。
    /// 機器が実際に生きている状態は HID なしでは作れないため、機器がない・範囲外のときに何もコピーしないこと、
    /// 記録中のフラグが入力レポートの処理へ届くことを守る。
    /// </summary>
    public class ControllerReadingsSourceTests
    {
        private static ControllerReadingsSource CreateSource(DeviceSlotState state)
            => new ControllerReadingsSource(state, TestHost.GetService<IMapping>(),
                TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IInputReportProcessor>());

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000)]
        public void TryRead_ReturnsFalse_AndCopiesNothing_WhenThereIsNoDevice(int slot)
        {
            var source = CreateSource(new DeviceSlotState());
            var input = new ControllerState { LX = 17 };
            var mapped = new ControllerState { LX = 23 };

            bool read = source.TryRead(slot, 0, input, mapped, out ControllerReadingInfo info);

            Assert.False(read);
            Assert.Equal(17, input.LX);
            Assert.Equal(23, mapped.LX);
            Assert.Equal(0.0, info.Latency);
        }

        [Fact]
        public void IsMeasuringProcessingDelay_IsForwardedToTheInputReportProcessor()
        {
            var processor = TestHost.GetService<IInputReportProcessor>();
            var source = CreateSource(new DeviceSlotState());
            bool before = processor.IsMeasuringProcessingDelay;
            try
            {
                source.IsMeasuringProcessingDelay = !before;

                Assert.Equal(!before, processor.IsMeasuringProcessingDelay);
            }
            finally
            {
                processor.IsMeasuringProcessingDelay = before;
            }
        }

        [Fact]
        public void MacroRecordingState_ForwardsTheRecordingFlag_AndReadsNothingWithoutADevice()
        {
            var processor = TestHost.GetService<IInputReportProcessor>();
            var recording = new MacroRecordingState(new DeviceSlotState(), TestHost.GetService<IMapping>(), processor);
            bool before = processor.RecordingMacro;
            try
            {
                recording.IsRecording = !before;

                Assert.Equal(!before, processor.RecordingMacro);
                Assert.Equal(!before, recording.IsRecording);
            }
            finally
            {
                processor.RecordingMacro = before;
            }

            Assert.False(recording.CanReadInput);
            Assert.False(recording.IsPressed(DS4Controls.Cross));
        }
    }
}
