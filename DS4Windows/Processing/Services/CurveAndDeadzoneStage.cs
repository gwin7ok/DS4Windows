using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="ICurveAndDeadzoneStage"/> の実装。旧 <c>Mapping.SetCurveAndDeadzone</c> の本体を、そのまま移したもの（式・順序は不変）。
    /// 状態（アンチスナップバックの履歴、ファズの前回の値）は <see cref="StickMotionState"/> が持つ。値は、デッドゾーン・出力カーブ・ジャイロの特性化テストが守る。
    /// 毎フレームの経路のため、割り当てを増やさない。
    /// </summary>
    public sealed class CurveAndDeadzoneStage : ICurveAndDeadzoneStage
    {
        private readonly IStickDeadZoneProcessor _stickDeadZone;
        private readonly IOutputCurveProcessor _outputCurve;
        private readonly ITriggerDeadZoneProcessor _triggerDeadZone;
        private readonly IGyroControlsProcessor _gyroControls;
        private readonly IStickInputConditioner _stickConditioner;
        private readonly System.Collections.Generic.Queue<DS4TimedStickAxisValue>[][] stickValueHistory;
        private readonly byte[][] lastStickAxisValues;

        public CurveAndDeadzoneStage(IStickDeadZoneProcessor stickDeadZone, IOutputCurveProcessor outputCurve,
            ITriggerDeadZoneProcessor triggerDeadZone, IGyroControlsProcessor gyroControls,
            IStickInputConditioner stickConditioner, StickMotionState stickMotion)
        {
            _stickDeadZone = stickDeadZone ?? throw new ArgumentNullException(nameof(stickDeadZone));
            _outputCurve = outputCurve ?? throw new ArgumentNullException(nameof(outputCurve));
            _triggerDeadZone = triggerDeadZone ?? throw new ArgumentNullException(nameof(triggerDeadZone));
            _gyroControls = gyroControls ?? throw new ArgumentNullException(nameof(gyroControls));
            _stickConditioner = stickConditioner ?? throw new ArgumentNullException(nameof(stickConditioner));
            if (stickMotion == null) throw new ArgumentNullException(nameof(stickMotion));
            stickValueHistory = stickMotion.StickValueHistory;
            lastStickAxisValues = stickMotion.LastStickAxisValues;
        }

        public ControllerState Apply(int device, ControllerState cState, ControllerState dState, IProfileSettingsService settings)
        {
            double rotation = /*tempDoubleArray[device] =*/  settings.LSRotation[device];
            if (rotation > 0.0 || rotation < 0.0)
                cState.rotateLSCoordinates(rotation);

            double rotationRS = /*tempDoubleArray[device] =*/ settings.RSRotation[device];
            if (rotationRS > 0.0 || rotationRS < 0.0)
                cState.rotateRSCoordinates(rotationRS);

            StickAntiSnapbackInfo lsAntiSnapback = settings.LSAntiSnapbackInfo[device];
            StickAntiSnapbackInfo rsAntiSnapback = settings.RSAntiSnapbackInfo[device];

            if (lsAntiSnapback.enabled)
            {
                _stickConditioner.AntiSnapback(stickValueHistory[device][0], lsAntiSnapback.delta, lsAntiSnapback.timeout, DateTimeOffset.Now.ToUnixTimeMilliseconds(), ref cState.LX, ref cState.LY);
            }

            if (rsAntiSnapback.enabled)
            {
                _stickConditioner.AntiSnapback(stickValueHistory[device][1], rsAntiSnapback.delta, rsAntiSnapback.timeout, DateTimeOffset.Now.ToUnixTimeMilliseconds(), ref cState.RX, ref cState.RY);
            }

            StickDeadZoneInfo lsMod = settings.LSModInfo[device];
            StickDeadZoneInfo rsMod = settings.RSModInfo[device];

            if (lsMod.fuzz > 0)
            {
                _stickConditioner.Fuzz(lastStickAxisValues[device], 0, lsMod.fuzz, ref cState.LX, ref cState.LY);
            }

            if (rsMod.fuzz > 0)
            {
                _stickConditioner.Fuzz(lastStickAxisValues[device], 1, rsMod.fuzz, ref cState.RX, ref cState.RY);
            }

            cState.CopyTo(dState);
            //ControllerState dState = new ControllerState(cState);

            // Phase9 9-A(2)-B: ドリフト補正・アンチスナップバック・ファズは、共通の処理（IStickInputConditioner）が行う。状態（履歴、前回の値）は StickMotionState が持つ。
            cState = _stickConditioner.ApplyDrift(device, dState, settings);


            // Phase9 9-A(1) (b): 左右のスティックのデッドゾーン（Radial・Axial、Radial の外側の出力）は、共通の処理（IStickDeadZoneProcessor）が行う。
            // 以前は、ここに、左右でほぼ同じコードが 2 回あった（約 470 行）。値は Phase9A1SetCurveAndDeadzoneGoldenTests が守る
            _stickDeadZone.Process(lsMod, cState.LX, cState.LY, ref dState.LX, ref dState.LY, ref dState.OutputLSOuter);
            _stickDeadZone.Process(rsMod, cState.RX, cState.RY, ref dState.RX, ref dState.RY, ref dState.OutputRSOuter);

            /*byte l2Deadzone = getL2Deadzone(device);
            int l2AntiDeadzone = getL2AntiDeadzone(device);
            int l2Maxzone = getL2Maxzone(device);
            */

            // Phase9 9-A(1) (d): トリガーのデッドゾーン（デッドゾーン、アンチデッドゾーン、最大ゾーン、最大出力）は、L2／R2 とも共通の処理（ITriggerDeadZoneProcessor）が行う。
            // 以前は、ここに、L2 と R2 で同じコードが 2 回あった（約 100 行）。値は Phase9A1SetCurveAndDeadzoneGoldenTests と Phase7bTriggerDeadZoneRoundingTests が守る
            TriggerDeadZoneZInfo l2ModInfo = settings.L2ModInfo[device];
            if (l2ModInfo.deadZone > 0 || l2ModInfo.antiDeadZone > 0 || l2ModInfo.maxZone != 100 || l2ModInfo.maxOutput != 100.0)
                _triggerDeadZone.Process(l2ModInfo, cState.L2, ref dState.L2);

            /*byte r2Deadzone = getR2Deadzone(device);
            int r2AntiDeadzone = getR2AntiDeadzone(device);
            int r2Maxzone = getR2Maxzone(device);
            */
            TriggerDeadZoneZInfo r2ModInfo = settings.R2ModInfo[device];
            if (r2ModInfo.deadZone > 0 || r2ModInfo.antiDeadZone > 0 || r2ModInfo.maxZone != 100 || r2ModInfo.maxOutput != 100.0)
                _triggerDeadZone.Process(r2ModInfo, cState.R2, ref dState.R2);

            // Only apply deprecated Sensitivity modifier for Radial DZ
            if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                double lsSens = settings.LSSens[device];
                if (lsSens != 1.0)
                {
                    dState.LX = (byte)NumericUtil.Clamp(0, lsSens * (dState.LX - 128.0) + 128.0, 255);
                    dState.LY = (byte)NumericUtil.Clamp(0, lsSens * (dState.LY - 128.0) + 128.0, 255);
                }
            }

            // Only apply deprecated Sensitivity modifier for Radial DZ
            if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                double rsSens = settings.RSSens[device];
                if (rsSens != 1.0)
                {
                    dState.RX = (byte)NumericUtil.Clamp(0, rsSens * (dState.RX - 128.0) + 128.0, 255);
                    dState.RY = (byte)NumericUtil.Clamp(0, rsSens * (dState.RY - 128.0) + 128.0, 255);
                }
            }

            double l2Sens = settings.L2Sens[device];
            if (l2Sens != 1.0)
                dState.L2 = (byte)NumericUtil.Clamp(0, l2Sens * dState.L2, 255);

            double r2Sens = settings.R2Sens[device];
            if (r2Sens != 1.0)
                dState.R2 = (byte)NumericUtil.Clamp(0, r2Sens * dState.R2, 255);

            SquareStickInfo squStk = settings.SquStickInfo[device];
            // Phase9 9-A(1) (c): スクエアスティックと出力カーブは IOutputCurveProcessor へ切り出した（左右・L2／R2 は同じ処理）。条件の判定は、ここに残す
            if (squStk.lsMode && (dState.LX != 128 || dState.LY != 128))
                _outputCurve.ProcessSquareStick(squStk.lsRoundness, ref dState.LX, ref dState.LY);

            int lsOutCurveMode = settings.GetLsOutCurveMode(device);
            if (lsOutCurveMode > 0 && (dState.LX != 128 || dState.LY != 128))
                _outputCurve.ProcessStickCurve(lsOutCurveMode, lsMod.deadzoneType, settings.LsOutBezierCurveObj[device], ref dState.LX, ref dState.LY);

            if (squStk.rsMode && (dState.RX != 128 || dState.RY != 128))
                _outputCurve.ProcessSquareStick(squStk.rsRoundness, ref dState.RX, ref dState.RY);

            int rsOutCurveMode = settings.GetRsOutCurveMode(device);
            if (rsOutCurveMode > 0 && (dState.RX != 128 || dState.RY != 128))
                _outputCurve.ProcessStickCurve(rsOutCurveMode, rsMod.deadzoneType, settings.RsOutBezierCurveObj[device], ref dState.RX, ref dState.RY);

            int l2OutCurveMode = settings.GetL2OutCurveMode(device);
            if (l2OutCurveMode > 0 && dState.L2 != 0)
                _outputCurve.ProcessTriggerCurve(l2OutCurveMode, settings.L2OutBezierCurveObj[device], ref dState.L2);

            int r2OutCurveMode = settings.GetR2OutCurveMode(device);
            if (r2OutCurveMode > 0 && dState.R2 != 0)
                _outputCurve.ProcessTriggerCurve(r2OutCurveMode, settings.R2OutBezierCurveObj[device], ref dState.R2);


            bool saControls = settings.GyroOutputMode[device] == GyroOutMode.Controls;
            if (saControls && dState.Motion.outputGyroControls)
            {
                int SXD = (int)(128d * settings.SXDeadzone[device]);
                int SZD = (int)(128d * settings.SZDeadzone[device]);
                double SXMax = settings.SXMaxzone[device];
                double SZMax = settings.SZMaxzone[device];
                double sxAntiDead = settings.SXAntiDeadzone[device];
                double szAntiDead = settings.SZAntiDeadzone[device];
                double sxsens = settings.SXSens[device];
                double szsens = settings.SZSens[device];

                int gyroX = cState.Motion.accelX, gyroZ = cState.Motion.accelZ;

                // Phase9 9-A(2)-A: ジャイロの補正（デッドゾーン、最大ゾーン、アンチデッドゾーン、感度、出力カーブ）は、X／Z とも共通の処理（IGyroControlsProcessor）が行う。
                // 以前は、ここに、X と Z で同じコードが 2 回あった（約 150 行）。値は Phase9A2GyroControlsGoldenTests が守る。出力カーブを適用する条件は、X と Z で違うため、ここに残す
                dState.Motion.outputAccelX = _gyroControls.Correct(gyroX, SXD, SXMax, sxAntiDead, sxsens);
                dState.Motion.outputAccelZ = _gyroControls.Correct(gyroZ, SZD, SZMax, szAntiDead, szsens);

                int sxOutCurveMode = settings.GetSxOutCurveMode(device);
                if (sxOutCurveMode > 0)
                    dState.Motion.outputAccelX = _gyroControls.ApplyCurve(sxOutCurveMode, settings.SxOutBezierCurveObj[device], dState.Motion.outputAccelX);

                int szOutCurveMode = settings.GetSzOutCurveMode(device);
                if (szOutCurveMode > 0 && dState.Motion.outputAccelZ != 0)
                    dState.Motion.outputAccelZ = _gyroControls.ApplyCurve(szOutCurveMode, settings.SzOutBezierCurveObj[device], dState.Motion.outputAccelZ);
            }

            return dState;
        }
    }
}
