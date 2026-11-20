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
            double tempMouseDeltaX = 0.0;
            double tempMouseDeltaY = 0.0;
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
            if (_profileActionProvider.GetProfileActionCount(device) > 0 || profileSettings.GetUseTempProfile(device))
                _specialActions.Run(device);

            ControlSettingsGroup controlSetGroup = profileSettings.GetControlSettingsGroup(device);

            // Process LS／RS（Phase9 MapCustom の分割 M3c: 左右で似たコードが 2 回あった。LS → RS の順は変えない）
            ProcessStick(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY,
                profileSettings.ForSlot(device).Sticks.LSOutput, controlSetGroup.LS, false);
            ProcessStick(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY,
                profileSettings.ForSlot(device).Sticks.RSOutput, controlSetGroup.RS, true);

            // Process L2／R2。L2 → R2 の順は変えない）
            ProcessTriggerWithTwoStage(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY,
                controlSetGroup.L2, controlSetGroup.L2FullPull, DS4Controls.L2, DS4Controls.L2FullPull,
                cState.L2, cState.L2Raw, profileSettings.ForSlot(device).Triggers.L2Output, l2TwoStageMappingData[device]);
            ProcessTriggerWithTwoStage(device, cState, MappedState, eState, tp, fieldMapping, outputfieldMapping, deviceState,
                ref tempMouseDeltaX, ref tempMouseDeltaY,
                controlSetGroup.R2, controlSetGroup.R2FullPull, DS4Controls.R2, DS4Controls.R2FullPull,
                cState.R2, cState.R2Raw, profileSettings.ForSlot(device).Triggers.R2Output, r2TwoStageMappingData[device]);

            // Process Standard buttons
            for (var settingEnum = controlSetGroup.ControlButtons.GetEnumerator(); settingEnum.MoveNext();)
            {
                DS4ControlSettings dcs = settingEnum.Current;
                ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY);
            }

            // Process Extra Device specific buttons
            for (var settingEnum = controlSetGroup.ExtraDeviceButtons.GetEnumerator(); settingEnum.MoveNext();)
            {
                DS4ControlSettings dcs = settingEnum.Current;
                ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY);
            }

            // 移行済みの種別（既定は空）の毎フレームの出力。Stage3 の直後（通常の割り当ての後）の位置を保つ
            try { _engine.ApplyFrameOutputs(device, _triggerInputs[device], _frameOutputs[device]); }
            catch (Exception ex) { AppLogger.Log(AppLogLevel.Error, $"Synchronous Button SA handling error: {ex}", LogOptions.UserFacing); }

            outputfieldMapping.PopulateState(MappedState);

            // MappedState summary TRACE removed as requested.

            // マクロが押しているゲームパッドのボタン等を出力へ重ねる処理は、MacroRuntimeState.ApplyHeldControls へ移した（デバイスごと。ユーザー判断 Q2）
            _macroRuntime.ApplyHeldControls(device, MappedState);

            PostMapStickData mapStickData = mapStickActionData[device];
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
            if (tempMouseDeltaX == 0.0) tempMouseDeltaX = frameOutput.MouseDeltaX;
            if (tempMouseDeltaY == 0.0) tempMouseDeltaY = frameOutput.MouseDeltaY;

            _mouseConversion.QuantizeMouseMotion(device, ref tempMouseDeltaX, ref tempMouseDeltaY,
                out mouseDeltaX, out mouseDeltaY);
            if (mouseDeltaX != 0 || mouseDeltaY != 0)
            {
                VirtualKBM.MoveRelativeMouse(mouseDeltaX, mouseDeltaY);
            }
        }

        // LS／RS の 1 スティック分の処理。
        // 左右の違い: 軸の添字、フリックスティックの読む軸、RS だけの「デルタ加速」（Controls モードのとき、割り当ての処理の前に実行）。ホットパスのため割り当てを増やさない
        private void ProcessStick(int device, ControllerState cState, ControllerState MappedState, ControllerStateExposed eState,
            TouchpadMouse tp, ControllerStateFieldMapping fieldMapping, ControllerStateFieldMapping outputfieldMapping, SyntheticState deviceState,
            ref double tempMouseDeltaX, ref double tempMouseDeltaY,
            StickOutputSetting stickSettings, List<DS4ControlSettings> stickControls, bool isRightStick)
        {
            if (stickSettings.mode == StickMode.Controls)
            {
                if (isRightStick && stickSettings.outputSettings.controlSettings.deltaAccelSettings.enabled)
                {
                    InputDeviceCore d = _slots.Controllers[device];
                    ControllerState cRawState = d.GetCurrentStateRef();
                    cRawState.CalculateStickAngles();

                    double maxXValue = cState.RX >= 128.0 ? 255.0 : 0.0;
                    double maxYValue = cState.RY >= 128.0 ? 255.0 : 0.0;
                    double maxXDir = maxXValue - 128.0;
                    double maxYDir = maxYValue - 128.0;
                    double axisDirX = cState.RX - 128.0;
                    double axisDirY = cState.RY - 128.0;
                    double axisRawDirX = cRawState.RX - 128.0;
                    double axisRawDirY = cRawState.RY - 128.0;

                    deltaAccelProcessors[device].RSProcessor.Process(device, axisDirX, axisDirY,
                        axisRawDirX, axisRawDirY,
                        maxXDir, maxYDir,
                        stickSettings.outputSettings.controlSettings.deltaAccelSettings);
                }

                for (var settingEnum = stickControls.GetEnumerator(); settingEnum.MoveNext();)
                {
                    DS4ControlSettings dcs = settingEnum.Current;
                    ProcessControlSettingAction(dcs, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY);
                }
            }
            else
            {
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RXNeg : DS4Controls.LXNeg)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RXPos : DS4Controls.LXPos)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RYNeg : DS4Controls.LYNeg)] = 128;
                outputfieldMapping.axisdirs[(int)(isRightStick ? DS4Controls.RYPos : DS4Controls.LYPos)] = 128;

                switch (stickSettings.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.FlickStick:
                        InputDeviceCore d = _slots.Controllers[device];
                        ControllerState cRawState = d.GetCurrentStateRef();
                        ControllerState pState = d.GetPreviousStateRef();

                        if (isRightStick)
                        {
                            _mouseConversion.ProcessFlickStick(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.RX, cRawState.RY, pState.RX, pState.RY, ref tempMouseDeltaX);
                        }
                        else
                        {
                            _mouseConversion.ProcessFlickStick(flickMappingData[device], stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.LX, cRawState.LY, pState.LX, pState.LY, ref tempMouseDeltaX);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        // L2／R2 の 1 トリガー分の処理。
        // ホットパスのため、ラムダや割り当てを使わない
        private void ProcessTriggerWithTwoStage(int device, ControllerState cState, ControllerState MappedState, ControllerStateExposed eState,
            TouchpadMouse tp, ControllerStateFieldMapping fieldMapping, ControllerStateFieldMapping outputfieldMapping, SyntheticState deviceState,
            ref double tempMouseDeltaX, ref double tempMouseDeltaY,
            DS4ControlSettings dcsTemp, DS4ControlSettings fullPullSettings, DS4Controls softControl, DS4Controls fullControl,
            byte triggerValue, byte triggerRawValue, TriggerOutputSettings triggerSettings, TwoStageTriggerMappingData twoStageData)
        {
            if (triggerSettings.twoStageMode == TwoStageTriggerMode.Disabled)
            {
                ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                    tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                    ref tempMouseDeltaY);
            }
            else
            {
                DS4ControlSettings fullPull = fullPullSettings;
                // 前のフレームで出力していたボタン。Process は、トリガーを離したフレームで状態を Reset()（previousActiveButtons も消える）するため、
                // 呼ぶ前に退避する。退避しないと、離したフレームで割り当てを処理できず、Key が押されたままになる
                TwoStageTriggerMappingData.ActiveZoneButtons previousButtons = twoStageData.previousActiveButtons;
                _twoStageTrigger.Process(device, triggerValue, triggerRawValue, ref dcsTemp, ref fullPull,
                    triggerSettings, twoStageData, out DS4ControlSettings? outputSoftPull, out DS4ControlSettings? outputFullPull);

                TwoStageTriggerMappingData.ActiveZoneButtons tempButtons = TwoStageTriggerMappingData.ActiveZoneButtons.None;
                // Check for Soft Pull activation
                if (outputSoftPull != null ||
                    (previousButtons & TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull) != 0)
                {
                    if (outputSoftPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.SoftPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.triggers[(int)softControl] = 0;
                        outputfieldMapping.triggers[(int)softControl] = 0;
                    }

                    ProcessControlSettingAction(dcsTemp, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY);
                }
                else
                {
                    // Soft Pull binding not engaged
                    outputfieldMapping.triggers[(int)softControl] = 0;
                }

                // Check for Full Pull activation
                if (outputFullPull != null ||
                    (previousButtons & TwoStageTriggerMappingData.ActiveZoneButtons.FullPull) != 0)
                {
                    if (outputFullPull != null)
                    {
                        tempButtons |= TwoStageTriggerMappingData.ActiveZoneButtons.FullPull;
                    }
                    else
                    {
                        // Need to reset input state so output binding is not activated.
                        // Used to de-activate Extras
                        fieldMapping.buttons[(int)fullControl] = false;
                    }

                    ProcessControlSettingAction(fullPull, device, cState, MappedState, eState,
                        tp, fieldMapping, outputfieldMapping, deviceState, ref tempMouseDeltaX,
                        ref tempMouseDeltaY);
                }

                // Store active buttons state
                twoStageData.previousActiveButtons = tempButtons;
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
                    _mouseConversion.BlendGyroMouseStick(msinfo, gyroStickX[device], gyroStickY[device],
                        MappedState, mapStickActionData[device]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessControlSettingAction(DS4ControlSettings dcs, int device, ControllerState cState, ControllerState MappedState, ControllerStateExposed eState,
            TouchpadMouse tp, ControllerStateFieldMapping fieldMapping, ControllerStateFieldMapping outputfieldMapping, SyntheticState deviceState, ref double tempMouseDeltaX, ref double tempMouseDeltaY)
        {
            // Check if this control press would trigger a Special Action
            // If so, suppress the normal mapping to prevent conflict
            if (CheckForSpecialActionSuppression(device, dcs.control, cState, eState, tp, fieldMapping))
            {
                // Special Action would be triggered - reset the control to default and return without processing
                ResetToDefaultValue(dcs.control, MappedState, outputfieldMapping);
                return;
            }

            // 新経路（エンジン）が所有する割り当て（7.8b では Key の条件を満たすもの）は、エンジンが処理し、旧経路の本体は素通りする。
            // ロールバックは git のみ
            if (_engine.OwnsControl(device, dcs.control))
            {
                _engine.ProcessControl(device, dcs.control, _triggerInputs[device], _frameOutputs[device]);
                return;
            }

            // 新経路が所有しない割り当て（読めない Extras ほか）は、束縛を作らない＝何もしない。
        }
    }
}
