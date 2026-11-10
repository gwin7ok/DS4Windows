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
using System.Threading.Tasks;
using DS4Windows.Output.Backend;

namespace DS4Windows.Output
{
    abstract class DS4OutDevice : OutputDevice
    {
        internal const byte RUMBLE_FEATURE_FLAG = 0x01;
        internal const byte LIGHTBAR_FEATURE_FLAG = 0x02;
        internal const byte FLASH_FEATURE_FLAG = 0x04;

        public const string devtype = "DS4";

        public IDs4PadTarget cont;
        // 振動の戻りの購読（キーは入力側の番号）。拡張 API（出力バッファ）を使えない旧ドライバー向けの経路で、破棄すると購読をやめる
        public System.Collections.Generic.Dictionary<int, IDisposable> forceFeedbacksDict =
            new System.Collections.Generic.Dictionary<int, IDisposable>();

        protected bool canUseAwaitOutputBuffer = false;
        public bool CanUseAwaitOutputBuffer => canUseAwaitOutputBuffer;

        public DS4OutDevice(IVirtualPadBackend backend)
        {
            cont = backend.CreateDs4Target();
        }

        public override void Connect()
        {
            cont.Connect();
            connected = true;
        }
        public override void Disconnect()
        {
            // Remove feedback handlers before Disconnect
            RemoveFeedbacks();

            connected = false;
            cont.Disconnect();
            //cont.Dispose();
            cont = null;
        }
        public override string GetDeviceType() => devtype;

        public override void RemoveFeedbacks()
        {
            foreach (System.Collections.Generic.KeyValuePair<int, IDisposable> pair in forceFeedbacksDict)
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

        /// <summary>入力の状態から、仮想 DS4 のボタン・十字キー・特殊ボタンの値を求める（Basic と Ext で共通）。</summary>
        protected static void MapButtons(DS4State state, out ushort tempButtons,
            out Ds4DPadDirection tempDPad, out ushort tempSpecial)
        {
            tempButtons = 0;
            tempDPad = Ds4DPadDirection.None;
            tempSpecial = 0;
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
            }
        }

        public virtual void StartOutputBufferThread()
        {
        }
    }
}
