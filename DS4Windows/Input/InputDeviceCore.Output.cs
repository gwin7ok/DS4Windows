using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input
{
    /// <summary>出力（振動・ライトバー・出力レポートの書き込み）。</summary>
    public partial class InputDeviceCore
    {
        /// <summary>
        /// 「物理コントローラーへ出力データを送る」を ON → OFF にする直前に、1 度だけ、ライトバー消灯・振動ゼロを書く差し込み口
        /// （フラグを立てたあとは何も書かれず、パッドが最後の色を保持するため）。既定は何もしない（出力を書く機種だけが override する）。
        /// 呼び出し側［<c>DeviceConnectionSetup.ApplyProfileOptions</c>］は、<see cref="ModifyFeatureSetFlag"/> で NoOutputData を立てる前に呼ぶ。
        /// </summary>
        public virtual void WriteOutputOffOnce()
        {
        }

        protected virtual void StopOutputUpdate()
        {
            lock (exitLocker)
            {
                if (ds4Output != null &&
                    ds4Output.IsAlive && !ds4Output.ThreadState.HasFlag(System.Threading.ThreadState.Stopped) &&
                    !ds4Output.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested))
                {
                    try
                    {
                        exitOutputThread = true;
                        ds4Output.Interrupt();
                        ds4Output.Join();
                    }
                    catch (Exception e)
                    {
                        AppLogger.LogDebug($"[InputDeviceCore] {Mac}: exception: {e.Message}");
                    }
                }
            }
        }

        protected bool WriteOutput(byte[] outputBuffer)
        {
            if (conType == ConnectionType.BT)
            {
                //if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                //    return hDevice.WriteOutputReportViaControl(outputReport);

                // Use Interrupt endpoint for BT DS4 connected devices now
                return hDevice.WriteOutputReportViaInterrupt(outputBuffer, READ_STREAM_TIMEOUT);
            }
            else
            {
                return hDevice.WriteOutputReportViaInterrupt(outputBuffer, READ_STREAM_TIMEOUT);
            }
        }

        protected bool WriteOutput()
        {
            if (conType == ConnectionType.BT)
            {
                //if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                //    return hDevice.WriteOutputReportViaControl(outputReport);

                // Use Interrupt endpoint for almost BT DS4 connected devices now
                return hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
            }
            else
            {
                return hDevice.WriteOutputReportViaInterrupt(outReportBuffer, READ_STREAM_TIMEOUT);
            }
        }

        public void SetRumble(byte rightLightFastMotor, byte leftHeavySlowMotor) => Haptics.SetRumble(rightLightFastMotor, leftHeavySlowMotor);

        protected void MergeStates() => Haptics.Merge();

        public void SetLightbarState(ref DS4LightbarState lightState) => Haptics.SetLightbar(ref lightState);
    }
}
