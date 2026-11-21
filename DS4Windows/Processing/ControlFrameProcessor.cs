#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Gyro;

namespace DS4Windows.Processing
{
    /// <summary>
    /// 1 フレームぶんの、スティック・トリガー・各ボタンの割り当ての処理（<c>Mapping.MapCustom</c> の本体）。
    /// フレームの入力と出力の表、マウスの移動量は <see cref="MapFrame"/> で受ける（デバイスごとに使い回し。割り当て 0）。
    /// </summary>
    public sealed class ControlFrameProcessor
    {
        private readonly IDeviceSlotState _slots;
        private readonly ITriggerDispatchEngine _engine;
        private readonly IMouseConversionStage _mouseConversion;
        private readonly ITwoStageTriggerProcessor _twoStageTrigger;
        private readonly DeviceStickMotion[] _motions;
        private readonly TriggerInput[] _triggerInputs;
        private readonly FrameOutput[] _frameOutputs;

        public ControlFrameProcessor(IDeviceSlotState slots, ITriggerDispatchEngine engine, IMouseConversionStage mouseConversion,
            ITwoStageTriggerProcessor twoStageTrigger, DeviceStickMotion[] motions,
            TriggerInput[] triggerInputs, FrameOutput[] frameOutputs)
        {
            ArgumentNullException.ThrowIfNull(slots);
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(mouseConversion);
            ArgumentNullException.ThrowIfNull(twoStageTrigger);
            ArgumentNullException.ThrowIfNull(motions);
            ArgumentNullException.ThrowIfNull(triggerInputs);
            ArgumentNullException.ThrowIfNull(frameOutputs);
            _slots = slots;
            _engine = engine;
            _mouseConversion = mouseConversion;
            _twoStageTrigger = twoStageTrigger;
            _motions = motions;
            _triggerInputs = triggerInputs;
            _frameOutputs = frameOutputs;
        }

        // LS／RS の 1 スティック分の処理。
        // 左右の違い: 軸の添字、フリックスティックの読む軸、RS だけの「デルタ加速」（Controls モードのとき、割り当ての処理の前に実行）。ホットパスのため割り当てを増やさない
        public void ProcessStick(MapFrame f, StickOutputSetting stickSettings, List<DS4ControlSettings> stickControls, bool isRightStick)
        {
            if (stickSettings.mode == StickMode.Controls)
            {
                if (isRightStick && stickSettings.outputSettings.controlSettings.deltaAccelSettings.enabled)
                {
                    InputDeviceCore d = _slots.Controllers[f.Device];
                    ControllerState cRawState = d.GetCurrentStateRef();
                    cRawState.CalculateStickAngles();

                    double maxXValue = f.CState.RX >= 128.0 ? 255.0 : 0.0;
                    double maxYValue = f.CState.RY >= 128.0 ? 255.0 : 0.0;
                    double maxXDir = maxXValue - 128.0;
                    double maxYDir = maxYValue - 128.0;
                    double axisDirX = f.CState.RX - 128.0;
                    double axisDirY = f.CState.RY - 128.0;
                    double axisRawDirX = cRawState.RX - 128.0;
                    double axisRawDirY = cRawState.RY - 128.0;

                    _motions[f.Device].DeltaAccelProcessors.RSProcessor.Process(f.Device, axisDirX, axisDirY,
                        axisRawDirX, axisRawDirY,
                        maxXDir, maxYDir,
                        stickSettings.outputSettings.controlSettings.deltaAccelSettings);
                }

                for (var settingEnum = stickControls.GetEnumerator(); settingEnum.MoveNext();)
                {
                    DS4ControlSettings dcs = settingEnum.Current;
                    ProcessControl(f, dcs);
                }
            }
            else
            {
                f.OutFields.axisdirs[(int)(isRightStick ? DS4Controls.RXNeg : DS4Controls.LXNeg)] = 128;
                f.OutFields.axisdirs[(int)(isRightStick ? DS4Controls.RXPos : DS4Controls.LXPos)] = 128;
                f.OutFields.axisdirs[(int)(isRightStick ? DS4Controls.RYNeg : DS4Controls.LYNeg)] = 128;
                f.OutFields.axisdirs[(int)(isRightStick ? DS4Controls.RYPos : DS4Controls.LYPos)] = 128;

                switch (stickSettings.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.FlickStick:
                        InputDeviceCore d = _slots.Controllers[f.Device];
                        ControllerState cRawState = d.GetCurrentStateRef();
                        ControllerState pState = d.GetPreviousStateRef();

                        if (isRightStick)
                        {
                            _mouseConversion.ProcessFlickStick(_motions[f.Device].FlickMappingData, stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.RX, cRawState.RY, pState.RX, pState.RY, ref f.MouseDeltaX);
                        }
                        else
                        {
                            _mouseConversion.ProcessFlickStick(_motions[f.Device].FlickMappingData, stickSettings.outputSettings.flickSettings, cRawState.elapsedTime,
                                cRawState.LX, cRawState.LY, pState.LX, pState.LY, ref f.MouseDeltaX);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        // L2／R2 の 1 トリガー分の処理。
        // ホットパスのため、ラムダや割り当てを使わない
        public void ProcessTrigger(MapFrame f,
            DS4ControlSettings dcsTemp, DS4ControlSettings fullPullSettings, DS4Controls softControl, DS4Controls fullControl,
            byte triggerValue, byte triggerRawValue, TriggerOutputSettings triggerSettings, TwoStageTriggerMappingData twoStageData)
        {
            if (triggerSettings.twoStageMode == TwoStageTriggerMode.Disabled)
            {
                ProcessControl(f, dcsTemp);
            }
            else
            {
                DS4ControlSettings fullPull = fullPullSettings;
                // 前のフレームで出力していたボタン。Process は、トリガーを離したフレームで状態を Reset()（previousActiveButtons も消える）するため、
                // 呼ぶ前に退避する。退避しないと、離したフレームで割り当てを処理できず、Key が押されたままになる
                TwoStageTriggerMappingData.ActiveZoneButtons previousButtons = twoStageData.previousActiveButtons;
                _twoStageTrigger.Process(f.Device, triggerValue, triggerRawValue, ref dcsTemp, ref fullPull,
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
                        f.Fields.triggers[(int)softControl] = 0;
                        f.OutFields.triggers[(int)softControl] = 0;
                    }

                    ProcessControl(f, dcsTemp);
                }
                else
                {
                    // Soft Pull binding not engaged
                    f.OutFields.triggers[(int)softControl] = 0;
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
                        f.Fields.buttons[(int)fullControl] = false;
                    }

                    ProcessControl(f, fullPull);
                }

                // Store active buttons state
                twoStageData.previousActiveButtons = tempButtons;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessControl(MapFrame f, DS4ControlSettings dcs)
        {
            // Check if this control press would trigger a Special Action
            // If so, suppress the normal mapping to prevent conflict
            if (_engine.IsSuppressed(f.Device, dcs.control))
            {
                // Special Action would be triggered - reset the control to default and return without processing
                ResetToDefaultValue(dcs.control, f.OutFields);
                return;
            }

            // 新経路（エンジン）が所有する割り当て（7.8b では Key の条件を満たすもの）は、エンジンが処理し、旧経路の本体は素通りする。
            // ロールバックは git のみ
            if (_engine.OwnsControl(f.Device, dcs.control))
            {
                _engine.ProcessControl(f.Device, dcs.control, _triggerInputs[f.Device], _frameOutputs[f.Device]);
                return;
            }

            // 新経路が所有しない割り当て（読めない Extras ほか）は、束縛を作らない＝何もしない。
        }

        // 成立中のボタンの通常の出力を、既定値へ戻す（FrameOutput.ResetTriggerControl と同じ結果）
        internal static void ResetToDefaultValue(DS4Controls control, ControllerStateFieldMapping fieldMap)
        {
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
    }
}
