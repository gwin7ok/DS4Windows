using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Output.Abstractions;
using DS4Windows.Output.Backend;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Devices
{
    /// <summary>
    /// Xbox 360 の仮想パッドの出力のカートリッジ。XInput のスロット番号を取れる ViGEmBus（1.17.333.0 以降）なら、その機能を有効にして作る。
    /// 戻りは振動のみ（大・小のモーター）。
    /// </summary>
    public sealed class Xbox360OutputCartridge : IOutputDeviceCartridge
    {
        // XInput のスロット番号を取れる最初の ViGEmBus のバージョン
        private static readonly Version XinputSlotMinVersion = new Version("1.17.333.0");

        private readonly IDeviceRumbleService _rumble;

        public Xbox360OutputCartridge(IDeviceRumbleService rumble)
        {
            _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
        }

        public OutContType Type => OutContType.X360;

        public OutputDevice Create(IVirtualPadBackend backend, Version busVersion)
        {
            if (XinputSlotMinVersion.CompareTo(busVersion) <= 0)
            {
                return new Xbox360OutDevice(backend, Xbox360OutDevice.X360Features.XInputSlotNum);
            }

            return new Xbox360OutDevice(backend);
        }

        public OutputDevice Narrow(OutputDevice device) => device as Xbox360OutDevice;

        public void AttachFeedback(int index, OutputDevice outDevice, InputDeviceCore device)
        {
            int devIndex = index;
            Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
            IDisposable p = tempXbox.cont.SubscribeFeedback((largeMotor, smallMotor) =>
            {
                //Trace.WriteLine(string.Format("Rumble ({0}, {1}) {2}",
                //    args.LargeMotor, args.SmallMotor, DateTime.Now.ToString("hh:mm:ss.FFFF")));
                _rumble.SetDevRumble(device, largeMotor, smallMotor, devIndex);
            });
            tempXbox.forceFeedbacksDict.Add(index, p);
        }

        public void RemoveFeedback(OutputDevice outDevice, int inputIndex)
        {
            int inIdx = inputIndex;
            Xbox360OutDevice tempXbox = outDevice as Xbox360OutDevice;
            tempXbox.RemoveFeedback(inIdx);
            //tempXbox.cont.FeedbackReceived -= tempXbox.forceFeedbackCall;
            //tempXbox.forceFeedbackCall = null;
        }
    }
}
