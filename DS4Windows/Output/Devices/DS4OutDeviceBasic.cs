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
using DS4Windows.Output.Backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows.Input;

namespace DS4Windows.Output.Devices
{
    class DS4OutDeviceBasic : DS4OutDevice
    {
        public DS4OutDeviceBasic(IVirtualPadBackend backend) : base(backend)
        {
        }

        public override void ConvertandSendReport(DS4State state, int device)
        {
            if (!connected) return;

            //cont.ResetReport();
            ushort tempButtons = 0;
            Ds4DPadDirection tempDPad = Ds4DPadDirection.None;
            ushort tempSpecial = 0;
            unchecked
            {
                if (state.Share) tempButtons |= Ds4Buttons.Share;
                if (state.L3) tempButtons |= Ds4Buttons.ThumbLeft;
                if (state.R3) tempButtons |= Ds4Buttons.ThumbRight;
                if (state.Options) tempButtons |= Ds4Buttons.Options;

                if (state.DpadUp && state.DpadRight) tempDPad = Ds4DPadDirection.Northeast;
                else if (state.DpadUp && state.DpadLeft) tempDPad = Ds4DPadDirection.Northwest;
                else if (state.DpadUp) tempDPad = Ds4DPadDirection.North;
                else if (state.DpadRight && state.DpadDown) tempDPad = Ds4DPadDirection.Southeast;
                else if (state.DpadRight) tempDPad = Ds4DPadDirection.East;
                else if (state.DpadDown && state.DpadLeft) tempDPad = Ds4DPadDirection.Southwest;
                else if (state.DpadDown) tempDPad = Ds4DPadDirection.South;
                else if (state.DpadLeft) tempDPad = Ds4DPadDirection.West;

                /*if (state.DpadUp) tempDPad = (state.DpadRight) ? DualShock4DPadValues.Northeast : DualShock4DPadValues.North;
                if (state.DpadRight) tempDPad = (state.DpadDown) ? DualShock4DPadValues.Southeast : DualShock4DPadValues.East;
                if (state.DpadDown) tempDPad = (state.DpadLeft) ? DualShock4DPadValues.Southwest : DualShock4DPadValues.South;
                if (state.DpadLeft) tempDPad = (state.DpadUp) ? DualShock4DPadValues.Northwest : DualShock4DPadValues.West;
                */

                if (state.L1) tempButtons |= Ds4Buttons.ShoulderLeft;
                if (state.R1) tempButtons |= Ds4Buttons.ShoulderRight;
                if (state.L2Btn) tempButtons |= Ds4Buttons.TriggerLeft;
                if (state.R2Btn) tempButtons |= Ds4Buttons.TriggerRight;
                //if (state.L2 > 0) tempButtons |= Ds4Buttons.TriggerLeft;
                //if (state.R2 > 0) tempButtons |= Ds4Buttons.TriggerRight;

                if (state.Triangle) tempButtons |= Ds4Buttons.Triangle;
                if (state.Circle) tempButtons |= Ds4Buttons.Circle;
                if (state.Cross) tempButtons |= Ds4Buttons.Cross;
                if (state.Square) tempButtons |= Ds4Buttons.Square;
                if (state.PS) tempSpecial |= Ds4SpecialButtons.Ps;
                if (state.OutputTouchButton) tempSpecial |= Ds4SpecialButtons.Touchpad;
                cont.SetButtonsFull(tempButtons);
                cont.SetSpecialButtonsFull((byte)tempSpecial);
                cont.SetDPadDirection(tempDPad);
                //report.Buttons = (ushort)tempButtons;
                //report.SpecialButtons = (byte)tempSpecial;
            }

            cont.LeftTrigger = state.L2;
            cont.RightTrigger = state.R2;

            // Phase9 Steering 削除 W3: 旧「ステアリング（ジャイロでハンドル操作）の軸の出力先」の switch は削除した（機能ごと削除。Phase9-SteeringWheel-Removal-Plan.md）。通常の軸の出力だけを行う
            cont.LeftThumbX = state.LX;
            cont.LeftThumbY = state.LY;
            cont.RightThumbX = state.RX;
            cont.RightThumbY = state.RY;

            cont.SubmitReport();
        }

        public override void ResetState(bool submit = true)
        {
            cont.ResetReport();
            if (submit)
            {
                cont.SubmitReport();
            }
        }
    }
}
