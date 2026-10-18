using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W2: <see cref="IFrameOutput"/> の実装（デバイスごとに 1 つ。<c>Mapping</c> が持ち、毎フレーム <see cref="Bind"/> で出力の表を差し替える。割り当て 0）。
    /// 中身は、現行の <c>Mapping.ResetToDefaultValue</c> と、Stage3（Synchronous Button SpecialAction handling）の出力の分岐をそのまま移したもの
    /// （<c>FrameOutputTests</c> が現行のメソッドと比較する）。まだ <c>Mapping</c> には配線していない（W4）。
    /// </summary>
    public sealed class FrameOutput : IFrameOutput
    {
        private DS4StateFieldMapping _outputFields;
        private SyntheticState _deviceState;

        /// <summary>このフレームの出力の表と、デバイスの合成状態（マウスのクリックのカウンター）を設定する。</summary>
        public void Bind(DS4StateFieldMapping outputFields, SyntheticState deviceState)
        {
            _outputFields = outputFields;
            _deviceState = deviceState;
        }

        // 現行の Mapping.ResetToDefaultValue と同じ
        public void ResetTriggerControl(DS4Controls control)
        {
            DS4StateFieldMapping fieldMap = _outputFields;
            if (fieldMap == null) return;

            int controlNum = (int)control;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                fieldMap.buttons[controlNum] = false;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                fieldMap.axisdirs[controlNum] = 128;
                int controlRelation = (controlNum % 2 == 0 ? controlNum - 1 : controlNum + 1);
                fieldMap.axisdirs[controlRelation] = 128;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                fieldMap.triggers[controlNum] = 0;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Touch)
            {
                fieldMap.buttons[controlNum] = false;
            }
        }

        // 現行の Stage3 の出力の分岐と同じ
        public void PressX360(X360Controls xboxControl)
        {
            DS4StateFieldMapping fields = _outputFields;
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
                fields.buttons[(int)xboxControl] = true;
            }
        }
    }
}
