using DS4Windows.Processing.Gyro;
using System;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IMouseConversionStage"/> の実装。4 つの計算の部品への委譲だけを持つ（計算と状態は各部品のまま。値は各部品の特性化テストが守る）。
    /// 毎フレームの経路のため、割り当てを増やさない。
    /// </summary>
    public sealed class MouseConversionStage : IMouseConversionStage
    {
        private readonly IButtonMouseMovementCalculator _buttonMouseMovement;
        private readonly IFlickStickProcessor _flickStick;
        private readonly IMouseMotionQuantizer _mouseMotionQuantizer;
        private readonly IGyroMouseStickBlender _gyroMouseStickBlender;

        public MouseConversionStage(IButtonMouseMovementCalculator buttonMouseMovement, IFlickStickProcessor flickStick,
            IMouseMotionQuantizer mouseMotionQuantizer, IGyroMouseStickBlender gyroMouseStickBlender)
        {
            _buttonMouseMovement = buttonMouseMovement ?? throw new ArgumentNullException(nameof(buttonMouseMovement));
            _flickStick = flickStick ?? throw new ArgumentNullException(nameof(flickStick));
            _mouseMotionQuantizer = mouseMotionQuantizer ?? throw new ArgumentNullException(nameof(mouseMotionQuantizer));
            _gyroMouseStickBlender = gyroMouseStickBlender ?? throw new ArgumentNullException(nameof(gyroMouseStickBlender));
        }

        public double CalculateButtonMouseMovement(int device, DS4Controls control, int mnum, DS4State cState,
            DS4StateFieldMapping fieldMapping, ButtonMouseInfo buttonMouseInfo, int lsDeadZone, int rsDeadZone,
            DeltaSettingsProcessorGroup deltaAccelProcessorGroup, double timeElapsed)
        {
            return _buttonMouseMovement.Calculate(device, control, mnum, cState, fieldMapping, buttonMouseInfo,
                lsDeadZone, rsDeadZone, deltaAccelProcessorGroup, timeElapsed);
        }

        public void ProcessFlickStick(FlickStickMappingData data, FlickStickSettings settings, double elapsedTime,
            byte stickX, byte stickY, byte prevStickX, byte prevStickY, ref double mouseDeltaX)
        {
            _flickStick.Process(data, settings, elapsedTime, stickX, stickY, prevStickX, prevStickY, ref mouseDeltaX);
        }

        public void QuantizeMouseMotion(int device, ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY)
        {
            _mouseMotionQuantizer.Quantize(device, ref rawMouseX, ref rawMouseY, out mouseX, out mouseY);
        }

        public void BlendGyroMouseStick(GyroMouseStickInfo msinfo, byte gyroX, byte gyroY, DS4State MappedState, PostMapStickData mapStickData)
        {
            _gyroMouseStickBlender.Blend(msinfo, gyroX, gyroY, MappedState, mapStickData);
        }
    }
}
