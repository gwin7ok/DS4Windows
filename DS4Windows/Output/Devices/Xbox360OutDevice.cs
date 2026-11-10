/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4Windows.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Output.Backend;
using DS4Windows.Input;

namespace DS4Windows.Output.Devices
{
    public class Xbox360OutDevice : OutputDevice
    {
        [Flags]
        public enum X360Features : ushort
        {
            XInputSlotNum = 0x01
        }

        //private const int inputResolution = 127 - (-128);
        //private const float reciprocalInputResolution = 1 / (float)inputResolution;
        private const float recipInputPosResolution = 1 / 127f;
        private const float recipInputNegResolution = 1 / 128f;
        private const int outputResolution = 32767 - (-32768);
        public const string devType = "X360";
        private const int USER_INDEX_WAIT = 250;
        public IXbox360PadTarget cont;
        //public Xbox360FeedbackReceivedEventHandler forceFeedbackCall;
        // Input index, Xbox360FeedbackReceivedEventHandler instance
        public Dictionary<int, IDisposable> forceFeedbacksDict =
            new Dictionary<int, IDisposable>();

        private const int XINPUT_SLOT_NUM_DEFAULT = -1;
        private const int MAX_XINPUT_SLOT_NUM = 8;
        private int _xInputSlotNum = XINPUT_SLOT_NUM_DEFAULT;
        public int XinputSlotNum
        {
            get => _xInputSlotNum;
            set
            {
                if (value >= 0 && value < MAX_XINPUT_SLOT_NUM) _xInputSlotNum = value;
            }
        }

        private X360Features _features;
        public X360Features Features => _features;

        public Xbox360OutDevice(IVirtualPadBackend backend)
        {
            cont = backend.CreateXbox360Target();
        }

        public Xbox360OutDevice(IVirtualPadBackend backend, X360Features features) :
            this(backend)
        {
            this._features = features;
        }

        public override void ConvertandSendReport(DS4State state, int device)
        {
            if (!connected) return;

            //cont.ResetReport();
            ushort tempButtons = 0;

            unchecked
            {
                if (state.Share) tempButtons |= Xbox360Buttons.Back;
                if (state.L3) tempButtons |= Xbox360Buttons.LeftThumb;
                if (state.R3) tempButtons |= Xbox360Buttons.RightThumb;
                if (state.Options) tempButtons |= Xbox360Buttons.Start;

                if (state.DpadUp) tempButtons |= Xbox360Buttons.Up;
                if (state.DpadRight) tempButtons |= Xbox360Buttons.Right;
                if (state.DpadDown) tempButtons |= Xbox360Buttons.Down;
                if (state.DpadLeft) tempButtons |= Xbox360Buttons.Left;

                if (state.L1) tempButtons |= Xbox360Buttons.LeftShoulder;
                if (state.R1) tempButtons |= Xbox360Buttons.RightShoulder;

                if (state.Triangle) tempButtons |= Xbox360Buttons.Y;
                if (state.Circle) tempButtons |= Xbox360Buttons.B;
                if (state.Cross) tempButtons |= Xbox360Buttons.A;
                if (state.Square) tempButtons |= Xbox360Buttons.X;
                if (state.PS) tempButtons |= Xbox360Buttons.Guide;
                cont.SetButtonsFull(tempButtons);
            }

            cont.LeftTrigger = state.L2;
            cont.RightTrigger = state.R2;

            // Phase9 Steering 削除 W3: 旧「ステアリング（ジャイロでハンドル操作）の軸の出力先」の switch は削除した（機能ごと削除。Phase9-SteeringWheel-Removal-Plan.md）。通常の軸の出力だけを行う
            cont.LeftThumbX = AxisScale(state.LX, false);
            cont.LeftThumbY = AxisScale(state.LY, true);
            cont.RightThumbX = AxisScale(state.RX, false);
            cont.RightThumbY = AxisScale(state.RY, true);

            cont.SubmitReport();
        }

        private short AxisScale(Int32 Value, Boolean Flip)
        {
            unchecked
            {
                Value -= 0x80;
                float recipRun = Value >= 0 ? recipInputPosResolution : recipInputNegResolution;

                float temp = Value * recipRun;
                //if (Flip) temp = (temp - 0.5f) * -1.0f + 0.5f;
                if (Flip) temp = -temp;
                temp = (temp + 1.0f) * 0.5f;

                return (short)(temp * outputResolution + (-32768));
            }
        }

        public override void Connect()
        {
            cont.Connect();
            connected = true;

            if (_features.HasFlag(X360Features.XInputSlotNum))
            {
                // Need a delay here
                Thread.Sleep(USER_INDEX_WAIT);
                try
                {
                    XinputSlotNum = cont.UserIndex;
                }
                catch (Exception)
                {
                    // Failed to grab xinput slot number. Set default
                    // slot number and remove feature flag
                    _xInputSlotNum = XINPUT_SLOT_NUM_DEFAULT;
                    _features -= X360Features.XInputSlotNum;
                }
            }
        }
        public override void Disconnect()
        {
            foreach (KeyValuePair<int, IDisposable> pair in forceFeedbacksDict)
            {
                pair.Value.Dispose();
            }

            forceFeedbacksDict.Clear();

            connected = false;
            cont.Disconnect();
            cont = null;
        }
        public override string GetDeviceType() => devType;

        public override void ResetState(bool submit=true)
        {
            cont.ResetReport();
            if (submit)
            {
                cont.SubmitReport();
            }
        }

        public override void RemoveFeedbacks()
        {
            foreach (KeyValuePair<int, IDisposable> pair in forceFeedbacksDict)
            {
                pair.Value.Dispose();
            }

            forceFeedbacksDict.Clear();
        }

        public override void RemoveFeedback(int inIdx)
        {
            if (forceFeedbacksDict.TryGetValue(inIdx, out IDisposable handler))
            {
                handler.Dispose();
                forceFeedbacksDict.Remove(inIdx);
            }
        }
    }
}
