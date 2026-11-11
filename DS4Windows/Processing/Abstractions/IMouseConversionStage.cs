using DS4Windows.Processing.Gyro;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// コントローラーの入力をマウスの動き（またはマウスのように動くスティック）へ変える段: ボタン・スティック・トリガー・ジャイロ方向からの移動量の計算、
    /// フリックスティック、移動量の整数化（端数の持ち越し）、ジャイロのマウススティック出力の合成。それぞれの計算は、下の 4 つの部品が持つ。
    /// 割り当ての段（<c>Mapping</c>）は、この窓口 1 つだけを見る。
    /// </summary>
    public interface IMouseConversionStage
    {
        /// <summary>ボタン・スティック・トリガー・ジャイロ方向からの、生のマウスの移動量（<see cref="IButtonMouseMovementCalculator.Calculate"/>）。</summary>
        double CalculateButtonMouseMovement(int device, DS4Controls control, int mnum, ControllerState cState,
            ControllerStateFieldMapping fieldMapping, ButtonMouseInfo buttonMouseInfo, int lsDeadZone, int rsDeadZone,
            DeltaSettingsProcessorGroup deltaAccelProcessorGroup, double timeElapsed);

        /// <summary>フリックスティックの処理（<see cref="IFlickStickProcessor.Process"/>）。</summary>
        void ProcessFlickStick(FlickStickMappingData data, FlickStickSettings settings, double elapsedTime,
            byte stickX, byte stickY, byte prevStickX, byte prevStickY, ref double mouseDeltaX);

        /// <summary>マウスの移動量の整数化と端数の持ち越し（<see cref="IMouseMotionQuantizer.Quantize"/>）。</summary>
        void QuantizeMouseMotion(int device, ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY);

        /// <summary>ジャイロのマウススティック出力の合成（<see cref="IGyroMouseStickBlender.Blend"/>）。</summary>
        void BlendGyroMouseStick(GyroMouseStickInfo msinfo, byte gyroX, byte gyroY, ControllerState MappedState, PostMapStickData mapStickData);
    }
}
