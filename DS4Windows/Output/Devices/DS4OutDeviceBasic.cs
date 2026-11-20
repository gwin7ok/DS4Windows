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

#nullable enable

using DS4Windows.Core.Model;
using DS4Windows.Output.Backend;

namespace DS4Windows.Output.Devices
{
    class DS4OutDeviceBasic : DS4OutDevice
    {
        public DS4OutDeviceBasic(IVirtualPadBackend backend) : base(backend)
        {
        }

        public override void ConvertandSendReport(ControllerState state, int device)
        {
            if (!connected) return;

            ApplyTriggerOutputMode(state, device);

            //cont.ResetReport();
            ushort tempButtons = 0;
            Ds4DPadDirection tempDPad = Ds4DPadDirection.None;
            ushort tempSpecial = 0;
            unchecked
            {
                MapButtons(state, out tempButtons, out tempDPad, out tempSpecial);
                cont!.SetButtonsFull(tempButtons); // 接続中（cont を作成した後）にだけ呼ぶ
                cont.SetSpecialButtonsFull((byte)tempSpecial);
                cont.SetDPadDirection(tempDPad);
                //report.Buttons = (ushort)tempButtons;
                //report.SpecialButtons = (byte)tempSpecial;
            }

            cont.LeftTrigger = state.L2;
            cont.RightTrigger = state.R2;

            // 旧「ステアリング（ジャイロでハンドル操作）の軸の出力先」の switch は削除した（機能ごと削除）。通常の軸の出力だけを行う
            cont.LeftThumbX = state.LX;
            cont.LeftThumbY = state.LY;
            cont.RightThumbX = state.RX;
            cont.RightThumbY = state.RY;

            cont.SubmitReport();
        }

        public override void ResetState(bool submit = true)
        {
            cont!.ResetReport(); // 接続中（cont を作成した後）にだけ呼ぶ
            if (submit)
            {
                cont.SubmitReport();
            }
        }
    }
}
