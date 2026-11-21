using DS4Windows.Processing.Touchpad;
using DS4Windows.Processing.Gyro;
using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using IVirtualKBM = DS4Windows.Processing.Abstractions.IVirtualKBM;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing
{
    /// <summary>コントローラーの 1 フレームの割り当て（MapCustom とスティック・トリガーの処理）。</summary>
    public partial class Mapping
    {
        public void MapCustom(int device, ControllerState cState, ControllerState MappedState, ControllerStateExposed eState,
            TouchpadMouse tp)
        {
            /* TODO: This method is slow sauce. Find ways to speed up action execution */
            int mouseDeltaX = 0;
            int mouseDeltaY = 0;

            cState.CalculateStickAngles();
            ControllerStateFieldMapping fieldMapping = fieldMappings[device];
            fieldMapping.PopulateFieldMapping(cState, eState, tp);
            ControllerStateFieldMapping outputfieldMapping = outputFieldMappings[device];
            outputfieldMapping.PopulateFieldMapping(cState, eState, tp);
            // トリガー判定の入力と、フレームの出力の書き込み口を、このフレームの表に合わせる（割り当て 0）
            // 前フレームの状態（MultiAction のエッジ判定用）は、デバイスの前回のレポートを渡す。
            // 前フレームの表は、WasActive が最初に呼ばれたときにだけ作る（遅延）ため、MultiAction がなければ費用は増えない。デバイスがなければ null（従来どおり）
            ControllerState? previousState = null;
            try { previousState = _slots.Controllers[device]?.GetPreviousStateRef(); } catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
            _triggerInputs[device].Update(cState, previousState, eState, tp, fieldMapping);
            _frameOutputs[device].Bind(outputfieldMapping, this.deviceState[device]);
            _mouseSampler.SetFrame(device, cState, fieldMapping);

            SyntheticState deviceState = this.deviceState[device];
            MapFrame frame = _mapFrames[device];
            frame.Begin(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState);
            if (_profileActionProvider.GetProfileActionCount(device) > 0 || profileSettings.GetUseTempProfile(device))
                _specialActions.Run(device);

            ControlSettingsGroup controlSetGroup = profileSettings.GetControlSettingsGroup(device);

            // Process LS／RS（左右で似たコードが 2 回あった。LS → RS の順は変えない）
            _controlFrame.ProcessStick(frame, profileSettings.ForSlot(device).Sticks.LSOutput, controlSetGroup.LS, false);
            _controlFrame.ProcessStick(frame, profileSettings.ForSlot(device).Sticks.RSOutput, controlSetGroup.RS, true);

            // Process L2／R2。L2 → R2 の順は変えない
            _controlFrame.ProcessTrigger(frame,
                controlSetGroup.L2, controlSetGroup.L2FullPull, DS4Controls.L2, DS4Controls.L2FullPull,
                cState.L2, cState.L2Raw, profileSettings.ForSlot(device).Triggers.L2Output, _motions[device].L2TwoStageMappingData);
            _controlFrame.ProcessTrigger(frame,
                controlSetGroup.R2, controlSetGroup.R2FullPull, DS4Controls.R2, DS4Controls.R2FullPull,
                cState.R2, cState.R2Raw, profileSettings.ForSlot(device).Triggers.R2Output, _motions[device].R2TwoStageMappingData);

            // Process Standard buttons
            for (var settingEnum = controlSetGroup.ControlButtons.GetEnumerator(); settingEnum.MoveNext();)
                _controlFrame.ProcessControl(frame, settingEnum.Current);

            // Process Extra Device specific buttons
            for (var settingEnum = controlSetGroup.ExtraDeviceButtons.GetEnumerator(); settingEnum.MoveNext();)
                _controlFrame.ProcessControl(frame, settingEnum.Current);

            // 移行済みの種別（既定は空）の毎フレームの出力。Stage3 の直後（通常の割り当ての後）の位置を保つ
            try { _engine.ApplyFrameOutputs(device, _triggerInputs[device], _frameOutputs[device]); }
            catch (Exception ex) { AppLogger.Log(AppLogLevel.Error, $"Synchronous Button SA handling error: {ex}", LogOptions.UserFacing); }

            outputfieldMapping.PopulateState(MappedState);

            // MappedState summary TRACE removed as requested.

            // マクロが押しているゲームパッドのボタン等を出力へ重ねる処理は、MacroRuntimeState.ApplyHeldControls へ移した（デバイスごと。ユーザー判断 Q2）
            _macroRuntime.ApplyHeldControls(device, MappedState);

            PostMapStickData mapStickData = _motions[device].MapStickActionData;
            if (mapStickData.dirty)
            {
                if (mapStickData.LX != 128 &&
                    Math.Abs(mapStickData.LX - 128) > Math.Abs(MappedState.LX - 128))
                {
                    MappedState.LX = mapStickData.LX;
                }

                if (mapStickData.LY != 128 &&
                    Math.Abs(mapStickData.LY - 128) > Math.Abs(MappedState.LY - 128))
                {
                    MappedState.LY = mapStickData.LY;
                }

                if (mapStickData.RX != 128 &&
                    Math.Abs(mapStickData.RX - 128) > Math.Abs(MappedState.RX - 128))
                {
                    MappedState.RX = mapStickData.RX;
                }

                if (mapStickData.RY != 128 &&
                    Math.Abs(mapStickData.RY - 128) > Math.Abs(MappedState.RY - 128))
                {
                    MappedState.RY = mapStickData.RY;
                }

                mapStickData.Reset();
                mapStickData.dirty = false;
            }

            // 新経路が書いたマウスの移動量。旧経路（局所変数）が何も書いていないときだけ使う（先に書いた方が勝つ、の規則のまま）
            DS4Windows.Processing.Actions.FrameOutput frameOutput = _frameOutputs[device];
            if (frame.MouseDeltaX == 0.0) frame.MouseDeltaX = frameOutput.MouseDeltaX;
            if (frame.MouseDeltaY == 0.0) frame.MouseDeltaY = frameOutput.MouseDeltaY;

            _mouseConversion.QuantizeMouseMotion(device, ref frame.MouseDeltaX, ref frame.MouseDeltaY,
                out mouseDeltaX, out mouseDeltaY);
            if (mouseDeltaX != 0 || mouseDeltaY != 0)
            {
                VirtualKBM.MoveRelativeMouse(mouseDeltaX, mouseDeltaY);
            }
        }

        public void TempMouseJoystick(int device, ControllerState MappedState)
        {
            GyroOutMode imuOutMode = profileSettings.ForSlot(device).Gyro.OutputMode;
            if (imuOutMode == GyroOutMode.MouseJoystick)
            {
                GyroMouseStickInfo msinfo = profileSettings.ForSlot(device).Gyro.MouseStick;
                if (msinfo.outputStick != GyroMouseStickInfo.OutputStick.None)
                {
                    // X／Y の合成（約 55 行）は GyroMouseStickBlender へ移した
                    _mouseConversion.BlendGyroMouseStick(msinfo, _motions[device].GyroStickX, _motions[device].GyroStickY,
                        MappedState, _motions[device].MapStickActionData);
                }
            }
        }
    }
}
