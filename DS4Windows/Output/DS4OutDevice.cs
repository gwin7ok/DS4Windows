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
using System;
using DS4Windows.Output.Backend;

namespace DS4Windows.Output
{
    abstract class DS4OutDevice : OutputDevice
    {
        internal const byte RUMBLE_FEATURE_FLAG = 0x01;
        internal const byte LIGHTBAR_FEATURE_FLAG = 0x02;
        internal const byte FLASH_FEATURE_FLAG = 0x04;

        public const string devtype = "DS4";

        public IDs4PadTarget? cont;
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
            cont!.Connect(); // 接続中（cont を作成した後）にだけ呼ぶ
            connected = true;
        }
        public override void Disconnect()
        {
            // Remove feedback handlers before Disconnect
            RemoveFeedbacks();

            connected = false;
            cont!.Disconnect(); // 接続中（cont を作成した後）にだけ呼ぶ
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
            if (forceFeedbacksDict.TryGetValue(inIdx, out IDisposable? handler))
            {
                handler.Dispose();
                forceFeedbacksDict.Remove(inIdx);
            }
        }

        /// <summary>
        /// スロットごとの、仮想 DS4 のトリガーの出力のモードを引く関数。作った側（カートリッジ）が設定する。null のときは、トリガーの後処理をしない。
        /// </summary>
        public System.Func<int, DS4Windows.Core.Settings.DS4TriggerOutputMode>? TriggerModeProvider { get; set; }

        /// <summary>
        /// 送出の直前に、トリガーの出力のモードを状態へ反映する。仮想 DS4 は L2／R2 のボタンも持つため、アナログ値からボタンを作る
        /// （Buttons ではアナログ値を 0 にして、ボタンだけにする）。状態は呼び出し元と共有のため、書き換えは呼び出し元へも見える（旧・入力レポートの処理と同じ）。
        /// 毎フレーム呼ぶ。割り当てはしない。
        /// </summary>
        protected void ApplyTriggerOutputMode(ControllerState state, int device)
        {
            if (TriggerModeProvider == null) return;

            DS4Windows.Core.Settings.DS4TriggerOutputMode trigMode = TriggerModeProvider(device);
            if (trigMode == DS4Windows.Core.Settings.DS4TriggerOutputMode.Default)
            {
                state.L2Btn = state.L2 > 0;
                state.R2Btn = state.R2 > 0;
            }
            else if (trigMode == DS4Windows.Core.Settings.DS4TriggerOutputMode.Buttons)
            {
                state.L2Btn = state.L2 > 0;
                state.R2Btn = state.R2 > 0;
                // Disable analog output
                state.L2 = 0;
                state.R2 = 0;
            }
        }

        /// <summary>入力の状態から、仮想 DS4 のボタン・十字キー・特殊ボタンの値を求める（Basic と Ext で共通）。</summary>
        protected static void MapButtons(ControllerState state, out ushort tempButtons,
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
