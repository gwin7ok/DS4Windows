using DS4Windows.Processing.Synthetic;
using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Phase8-Step4 W2: <see cref="IFrameOutput"/> の実装（デバイスごとに 1 つ。<c>Mapping</c> が持ち、毎フレーム <see cref="Bind"/> で出力の表を差し替える。割り当て 0）。
    /// 中身は、現行の <c>Mapping.ResetToDefaultValue</c> と、Stage3（Synchronous Button SpecialAction handling）の出力の分岐をそのまま移したもの
    /// （<c>FrameOutputTests</c> が現行のメソッドと比較する）。まだ <c>Mapping</c> には配線していない（W4）。
    /// </summary>
    public sealed class FrameOutput : IPadFrameOutput, IToggleFrameOutput, IMouseMoveFrameOutput, IFrameOutputTrace
    {
        private ControllerStateFieldMapping _outputFields;
        private SyntheticState _deviceState;

        /// <summary>このフレームの出力の表と、デバイスの合成状態（マウスのクリックのカウンター）を設定する。</summary>
        public void Bind(ControllerStateFieldMapping outputFields, SyntheticState deviceState)
        {
            _outputFields = outputFields;
            _deviceState = deviceState;
            MouseDeltaX = 0.0;   // Phase8-Step7 7.8f-3: フレームごとに、マウスの移動量の加算器を空にする
            MouseDeltaY = 0.0;
        }

        /// <summary>Phase8-Step7 7.8f-3（Q6＝A）: このフレームで、新経路が書いたマウスの移動量（旧 <c>tempMouseDeltaX／Y</c> の置き換え）。</summary>
        public double MouseDeltaX { get; private set; }

        public double MouseDeltaY { get; private set; }

        public void SetMouseDeltaX(double value)
        {
            if (MouseDeltaX == 0.0) MouseDeltaX = value;
        }

        public void SetMouseDeltaY(double value)
        {
            if (MouseDeltaY == 0.0) MouseDeltaY = value;
        }

        // 現行の Mapping.ResetToDefaultValue と同じ
        public void ResetTriggerControl(DS4Controls control)
        {
            ControllerStateFieldMapping fieldMap = _outputFields;
            if (fieldMap == null) return;

            int controlNum = (int)control;
            ControllerStateFieldMapping.ControlType controlType = ControllerStateFieldMapping.mappedType[controlNum];
            if (controlType == ControllerStateFieldMapping.ControlType.Button)
            {
                fieldMap.buttons[controlNum] = false;
            }
            else if (controlType == ControllerStateFieldMapping.ControlType.AxisDir)
            {
                fieldMap.axisdirs[controlNum] = 128;
                int controlRelation = (controlNum % 2 == 0 ? controlNum - 1 : controlNum + 1);
                fieldMap.axisdirs[controlRelation] = 128;
            }
            else if (controlType == ControllerStateFieldMapping.ControlType.Trigger)
            {
                fieldMap.triggers[controlNum] = 0;
            }
            else if (controlType == ControllerStateFieldMapping.ControlType.Touch)
            {
                fieldMap.buttons[controlNum] = false;
            }
        }

        // Phase8-Step7 7.8f-1: 旧 customMapQueue の処理（Mapping の全 dcs のあと）が出力の表へ書いていたものと同じ
        public void SetAxisPair(DS4Controls outControl, byte value)
        {
            ControllerStateFieldMapping fields = _outputFields;
            if (fields == null) return;

            int tempOutControl = (int)outControl;
            int controlRelation = tempOutControl % 2 == 0 ? tempOutControl - 1 : tempOutControl + 1;
            fields.axisdirs[tempOutControl] = value;
            fields.axisdirs[controlRelation] = value;
        }

        public void SetTrigger(DS4Controls outControl, byte value)
        {
            ControllerStateFieldMapping fields = _outputFields;
            if (fields == null) return;

            fields.triggers[(int)outControl] = value;
        }

        public void SetButton(DS4Controls outControl)
        {
            ControllerStateFieldMapping fields = _outputFields;
            if (fields == null) return;

            fields.buttons[(int)outControl] = true;
        }

        // Phase8-Step7 7.8f-2: 旧経路の Controls の Toggle が、デバイスの合成状態へ書いていたものと同じ
        public void FlipToggle()
        {
            SyntheticState deviceState = _deviceState;
            if (deviceState == null) return;

            deviceState.currentClicks.toggle = !deviceState.currentClicks.toggle;
        }

        public void CountToggle()
        {
            SyntheticState deviceState = _deviceState;
            if (deviceState == null) return;

            deviceState.currentClicks.toggleCount++;
        }

        // Phase8-Step7 7.1-W2: 現行の Stage3 の「出力の組み合わせ」のログ（Mapping の Stage3／LogSpecialActionTrace）と同じ順序・書式
        public void AppendOutputCombo(List<string> parts)
        {
            ControllerStateFieldMapping fields = _outputFields;
            SyntheticState ds = _deviceState;
            if (fields == null) return;

            if (fields.outputTouchButton) parts.Add("TouchpadClick");
            if (ds != null)
            {
                if (ds.currentClicks.leftCount > 0) parts.Add($"LeftMouse({ds.currentClicks.leftCount})");
                if (ds.currentClicks.rightCount > 0) parts.Add($"RightMouse({ds.currentClicks.rightCount})");
                if (ds.currentClicks.middleCount > 0) parts.Add($"MiddleMouse({ds.currentClicks.middleCount})");
                if (ds.currentClicks.fourthCount > 0) parts.Add($"FourthMouse({ds.currentClicks.fourthCount})");
                if (ds.currentClicks.fifthCount > 0) parts.Add($"FifthMouse({ds.currentClicks.fifthCount})");
                if (ds.currentClicks.wUpCount > 0) parts.Add($"WUP({ds.currentClicks.wUpCount})");
                if (ds.currentClicks.wDownCount > 0) parts.Add($"WDOWN({ds.currentClicks.wDownCount})");
            }

            for (int bi = 0; bi < fields.buttons.Length; bi++)
            {
                if (fields.buttons[bi]) parts.Add($"{(X360Controls)bi}({bi})");
            }
        }

        // 現行の Stage3 の出力の分岐と同じ
        public void PressX360(X360Controls xboxControl)
        {
            ControllerStateFieldMapping fields = _outputFields;
            SyntheticState deviceState = _deviceState;
            if (fields == null || deviceState == null) return;

            if (xboxControl == X360Controls.TouchpadClick)
            {
                fields.outputTouchButton = true;
            }
            else if (xboxControl >= X360Controls.LeftMouse && xboxControl <= X360Controls.WDOWN)
            {
                // マウス系の出力: デバイスの合成状態のクリックのカウンターを増やす
                switch (xboxControl)
                {
                    case X360Controls.LeftMouse: deviceState.currentClicks.leftCount++; break;
                    case X360Controls.RightMouse: deviceState.currentClicks.rightCount++; break;
                    case X360Controls.MiddleMouse: deviceState.currentClicks.middleCount++; break;
                    case X360Controls.FourthMouse: deviceState.currentClicks.fourthCount++; break;
                    case X360Controls.FifthMouse: deviceState.currentClicks.fifthCount++; break;
                    case X360Controls.WUP: deviceState.currentClicks.wUpCount++; break;
                    case X360Controls.WDOWN: deviceState.currentClicks.wDownCount++; break;
                }
            }
            else if ((int)xboxControl < fields.buttons.Length && xboxControl != X360Controls.None)
            {
                // Phase10 完了後の点検（2026-10-22）: パッドのボタンは、Controls と同じ変換表（reverseX360ButtonMapping）で DS4 のボタンへ写す。以前は X360 の番号をそのまま使い、
                // Back・Start だけ、別のボタン（TouchLeft・TouchUpper）へ書いていた（ほかのボタンは、番号が一致するため、変換しても同じ）
                int outputIndex = xboxControl >= X360Controls.LXNeg && xboxControl <= X360Controls.Start
                    ? (int)X360ControlTables.reverseX360ButtonMapping[(int)xboxControl]
                    : (int)xboxControl;
                if (outputIndex < fields.buttons.Length) fields.buttons[outputIndex] = true;
            }
        }
    }
}
