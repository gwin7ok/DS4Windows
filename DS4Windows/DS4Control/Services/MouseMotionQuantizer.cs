using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="IMouseMotionQuantizer"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.calculateFinalMouseMovement</c> を、そのまま移したもの。端数は、デバイスごとに持つ（旧: <c>Mapping</c> の 1 組を全コントローラーで共有。ユーザー判断 Q2）。
    /// 値の特性化は <c>Phase9BMouseMotionQuantizerTests</c> が守る。
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
            double mouseXTemp = rawMouseX - (Mapping.remainderCutoff(rawMouseX * 100.0, 1.0) / 100.0);
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
            double mouseYTemp = rawMouseY - (Mapping.remainderCutoff(rawMouseY * 100.0, 1.0) / 100.0);
            mouseY = (int)mouseYTemp;
            _verticalRemainder[device] = mouseYTemp - mouseY;
            //mouseY = (int)rawMouseY;
            //verticalRemainder = rawMouseY - mouseY;
        }
    }
}
