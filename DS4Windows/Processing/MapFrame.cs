#nullable enable

using DS4Windows.Core.Model;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;

namespace DS4Windows.Processing
{
    /// <summary>
    /// 1 フレームぶんの割り当ての処理が共有する入力・出力の表と、旧経路が積むマウスの移動量。
    /// デバイスごとに 1 つを使い回す（フレームごとの割り当てなし）。<see cref="Begin"/> で、そのフレームの値へ合わせる。
    /// </summary>
    public sealed class MapFrame
    {
        public int Device { get; private set; }
        public ControllerState CState { get; private set; } = null!;   // Begin が、使う前に必ず設定する
        public ControllerState MappedState { get; private set; } = null!;   // 同上
        public ControllerStateExposed EState { get; private set; } = null!;   // 同上
        public TouchpadMouse Tp { get; private set; } = null!;   // 同上
        public ControllerStateFieldMapping Fields { get; private set; } = null!;   // 同上
        public ControllerStateFieldMapping OutFields { get; private set; } = null!;   // 同上
        public SyntheticState DeviceState { get; private set; } = null!;   // 同上
        // ref で渡すため、フィールドにしている（旧経路の ProcessFlickStick・QuantizeMouseMotion が ref を受ける）
        public double MouseDeltaX;
        public double MouseDeltaY;

        public void Begin(int device, ControllerState cState, ControllerState mappedState, ControllerStateExposed eState, TouchpadMouse tp,
            ControllerStateFieldMapping fields, ControllerStateFieldMapping outFields, SyntheticState deviceState)
        {
            Device = device;
            CState = cState;
            MappedState = mappedState;
            EState = eState;
            Tp = tp;
            Fields = fields;
            OutFields = outFields;
            DeviceState = deviceState;
            MouseDeltaX = 0.0;
            MouseDeltaY = 0.0;
        }
    }
}
