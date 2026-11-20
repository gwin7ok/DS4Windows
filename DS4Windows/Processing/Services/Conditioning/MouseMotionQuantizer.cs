
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Conditioning
{
    /// <summary>
    /// <see cref="IMouseMotionQuantizer"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.calculateFinalMouseMovement</c> を、そのまま移したもの。端数は、デバイスごとに持つ。
    /// 値の特性化は 対応するテスト が守る。
    /// </summary>
    public sealed class MouseMotionQuantizer : IMouseMotionQuantizer
    {
        private readonly double[] _horizontalRemainder = new double[ControllerLimits.MaxControllerCount];
        private readonly double[] _verticalRemainder = new double[ControllerLimits.MaxControllerCount];

        public void Quantize(int device, ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY)
        {
            if ((rawMouseX > 0.0 && _horizontalRemainder[device] > 0.0) || (rawMouseX < 0.0 && _horizontalRemainder[device] < 0.0))
            {
                rawMouseX += _horizontalRemainder[device];
            }
            else
            {
                _horizontalRemainder[device] = 0.0;
            }

            //double mouseXTemp = rawMouseX - (Math.IEEERemainder(rawMouseX * 1000.0, 1.0) / 1000.0);
            double mouseXTemp = rawMouseX - (Mapping.RemainderCutoff(rawMouseX * 100.0, 1.0) / 100.0);
            //double mouseXTemp = rawMouseX - (rawMouseX * 1000.0 - (1.0 * (int)(rawMouseX * 1000.0 / 1.0)));
            mouseX = (int)mouseXTemp;
            _horizontalRemainder[device] = mouseXTemp - mouseX;
            //mouseX = (int)rawMouseX;
            //horizontalRemainder = rawMouseX - mouseX;

            if ((rawMouseY > 0.0 && _verticalRemainder[device] > 0.0) || (rawMouseY < 0.0 && _verticalRemainder[device] < 0.0))
            {
                rawMouseY += _verticalRemainder[device];
            }
            else
            {
                _verticalRemainder[device] = 0.0;
            }

            //double mouseYTemp = rawMouseY - (Math.IEEERemainder(rawMouseY * 1000.0, 1.0) / 1000.0);
            double mouseYTemp = rawMouseY - (Mapping.RemainderCutoff(rawMouseY * 100.0, 1.0) / 100.0);
            mouseY = (int)mouseYTemp;
            _verticalRemainder[device] = mouseYTemp - mouseY;
            //mouseY = (int)rawMouseY;
            //verticalRemainder = rawMouseY - mouseY;
        }
    }
}
