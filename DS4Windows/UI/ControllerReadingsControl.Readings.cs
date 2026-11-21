using DS4Windows.Core.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DS4Windows.UI
{
    /// <summary>読み取り値の定期更新と、画面の値への反映。</summary>
    public partial class ControllerReadingsControl
    {
        private void ControllerReadingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            readingTimer.Stop();

            // 前回のUI更新処理がまだ完了していない場合はスキップし、キューの滞留を防止
            if (isUiUpdating)
            {
                if (useTimer) readingTimer.Start();
                return;
            }

            // 状態のコピーは機器の読み取りの待機を取って行う（ControlService ではなく窓口が担う）。
            // 割り当てを増やさないよう、コピー先はこの画面が持つ baseState／interState を渡す
            if (readingsSource != null && readingsSource.TryRead(deviceNum, profileDeviceNum, baseState, interState, out ControllerReadingInfo info))
            {
                isUiUpdating = true;

                // UIスレッドへのディスパッチを非同期・Background優先度にすることで、
                // マウスやWindowsの描画メッセージを最優先させ、OS全体の重さを解消
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        UpdateUiControls(info);
                    }
                    finally
                    {
                        isUiUpdating = false;
                    }
                }));
            }

            if (useTimer)
            {
                readingTimer.Start();
            }
        }

        /// <summary>
        /// 値が変化したコントロールのみをピンポイントで更新する差分描画処理
        /// </summary>
        private void UpdateUiControls(ControllerReadingInfo info)
        {
            int inLx = baseState.LX;
            int inLy = baseState.LY;
            int outLx = interState.LX;
            int outLy = interState.LY;

            // 左スティック Canvas & ラベル差分更新
            if (inLx != lastInLX || inLy != lastInLY || outLx != lastOutLX || outLy != lastOutLY)
            {
                if (inLx != lastInLX || inLy != lastInLY)
                {
                    Canvas.SetLeft(lsValRec, inLx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(lsValRec, inLy / 255.0 * CANVAS_WIDTH - 3);
                    lxInValLb.Content = inLx;
                    lyInValLb.Content = inLy;
                    lastInLX = inLx;
                    lastInLY = inLy;
                }
                if (outLx != lastOutLX || outLy != lastOutLY)
                {
                    Canvas.SetLeft(lsMapValRec, outLx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(lsMapValRec, outLy / 255.0 * CANVAS_WIDTH - 3);
                    lxOutValLb.Content = outLx;
                    lyOutValLb.Content = outLy;
                    lastOutLX = outLx;
                    lastOutLY = outLy;
                }
            }

            int inRx = baseState.RX;
            int inRy = baseState.RY;
            int outRx = interState.RX;
            int outRy = interState.RY;

            // 右スティック Canvas & ラベル差分更新
            if (inRx != lastInRX || inRy != lastInRY || outRx != lastOutRX || outRy != lastOutRY)
            {
                if (inRx != lastInRX || inRy != lastInRY)
                {
                    Canvas.SetLeft(rsValRec, inRx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(rsValRec, inRy / 255.0 * CANVAS_WIDTH - 3);
                    rxInValLb.Content = inRx;
                    ryInValLb.Content = inRy;
                    lastInRX = inRx;
                    lastInRY = inRy;
                }
                if (outRx != lastOutRX || outRy != lastOutRY)
                {
                    Canvas.SetLeft(rsMapValRec, outRx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(rsMapValRec, outRy / 255.0 * CANVAS_WIDTH - 3);
                    rxOutValLb.Content = outRx;
                    ryOutValLb.Content = outRy;
                    lastOutRX = outRx;
                    lastOutLY = outRy;
                }
            }

            // SixAxis / ジャイロ差分更新
            int accelX = exposeState.GetAccelX();
            int accelY = exposeState.GetAccelY();
            int accelZ = exposeState.GetAccelZ();
            double outAccelX = interState.Motion.outputAccelX;
            double outAccelZ = interState.Motion.outputAccelZ;

            if (accelX != lastAccelX || accelZ != lastAccelZ || Math.Abs(outAccelX - lastOutAccelX) > 0.01 || Math.Abs(outAccelZ - lastOutAccelZ) > 0.01)
            {
                int posX = accelX + 127;
                int posZ = accelZ + 127;
                Canvas.SetLeft(sixAxisValRec, posX / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetTop(sixAxisValRec, posZ / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetLeft(sixAxisMapValRec, Math.Min(Math.Max(outAccelX + 127.0, 0), 255.0) / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetTop(sixAxisMapValRec, Math.Min(Math.Max(outAccelZ + 127.0, 0), 255.0) / 255.0 * CANVAS_WIDTH - 3);

                sixAxisXInValLb.Content = exposeState.AccelX;
                sixAxisXOutValLb.Content = (int)outAccelX;
                sixAxisZInValLb.Content = exposeState.AccelZ;
                sixAxisZOutValLb.Content = (int)outAccelZ;

                accelXSlider.Value = accelX;
                accelYSlider.Value = accelY;
                accelZSlider.Value = accelZ;

                lastAccelX = accelX;
                lastAccelY = accelY;
                lastAccelZ = accelZ;
                lastOutAccelX = outAccelX;
                lastOutAccelZ = outAccelZ;
            }

            // L2 トリガー差分更新
            int inL2 = baseState.L2;
            int outL2 = interState.L2;
            if (inL2 != lastInL2 || outL2 != lastOutL2)
            {
                l2Slider.Value = inL2;
                l2ValLbTrans.Y = Math.Min(outL2, Math.Max(0, 255)) / 255.0 * -70.0 + TRIG_LB_TRANSFORM_OFFSETY;
                l2ValLbBrush.Color = outL2 >= 255 ? Colors.Green : (outL2 == 0 ? Colors.Red : ThemeForegroundColor());
                l2InValLb.Content = inL2;
                l2OutValLb.Content = outL2;
                lastInL2 = inL2;
                lastOutL2 = outL2;
            }

            // R2 トリガー差分更新
            int inR2 = baseState.R2;
            int outR2 = interState.R2;
            if (inR2 != lastInR2 || outR2 != lastOutR2)
            {
                r2Slider.Value = inR2;
                r2ValLbTrans.Y = Math.Min(outR2, Math.Max(0, 255)) / 255.0 * -70.0 + TRIG_LB_TRANSFORM_OFFSETY;
                r2ValLbBrush.Color = outR2 >= 255 ? Colors.Green : (outR2 == 0 ? Colors.Red : ThemeForegroundColor());
                r2InValLb.Content = inR2;
                r2OutValLb.Content = outR2;
                lastInR2 = inR2;
                lastOutR2 = outR2;
            }

            // ジャイロスライダー差分更新
            int yaw = baseState.Motion.gyroYawFull;
            int pitch = baseState.Motion.gyroPitchFull;
            int roll = baseState.Motion.gyroRollFull;
            if (yaw != lastGyroYaw || pitch != lastGyroPitch || roll != lastGyroRoll)
            {
                gyroYawSlider.Value = yaw;
                gyroPitchSlider.Value = pitch;
                gyroRollSlider.Value = roll;
                lastGyroYaw = yaw;
                lastGyroPitch = pitch;
                lastGyroRoll = roll;
            }

            // タッチパッド差分更新
            int touchX = baseState.TrackPadTouch0.X;
            int touchY = baseState.TrackPadTouch0.Y;
            if (touchX != lastTouchX || touchY != lastTouchY)
            {
                touchXValLb.Content = touchX;
                touchYValLb.Content = touchY;
                lastTouchX = touchX;
                lastTouchY = touchY;
            }

            // 入力遅延 (最大値の更新と表示)
            double latency = info.Latency;
            if (latency > maxLatency) maxLatency = latency;
            int latencyInt = (int)(latency * 10);

            if (latencyInt != lastLatencyInt || latency >= maxLatency)
            {
                int warnInterval = info.WarnInterval;

                // 元のリソース（言語ファイル）による「入力遅延: XX ms」の文字列を取得し、それにMaxを付与する
                string baseText = string.Format(Properties.Resources.InputDelay, latency.ToString("0.0"));
                inputDelayLb.Content = $"{baseText} (Max: {maxLatency.ToString("0.0")} ms)";

                if (latency > warnInterval)
                {
                    inpuDelayBackBrush.Color = Colors.Red;
                    inpuDelayForeBrush.Color = Colors.White;
                }
                else if (latency > (warnInterval * 0.5))
                {
                    inpuDelayBackBrush.Color = Colors.Yellow;
                    inpuDelayForeBrush.Color = Colors.Black;
                }
                else
                {
                    inpuDelayBackBrush.Color = Colors.Transparent;
                    inpuDelayForeBrush.Color = ThemeForegroundColor();
                }
                lastLatencyInt = latencyInt;
            }

            // バッテリー残量差分更新
            int battery = baseState.Battery;
            if (battery != lastBattery)
            {
                batteryLvlLb.Content = $"{Translations.Strings.Battery}: {battery}%";
                lastBattery = battery;
            }

            // キャリブレーションインジケータ
            long cntCalibrating = info.CalibrationCount;
            if (cntCalibrating != lastCalibrating)
            {
                gyroCalEllipse.Visibility = cntCalibrating > 0 && ((cntCalibrating / 250) % 2 == 1) ? Visibility.Visible : Visibility.Hidden;
                lastCalibrating = cntCalibrating;
            }

            // 出力遅延 (最大値の更新と表示)
            double procDelay = info.ProcessingDelayMs;
            if (procDelay > maxProcDelay) maxProcDelay = procDelay;
            int procDelayInt = (int)(procDelay * 100);

            if (procDelayInt != lastProcDelayInt || procDelay >= maxProcDelay)
            {
                outputDelayLabel ??= FindName("outputDelayLb") as Label;
                if (outputDelayLabel != null)
                {
                    outputDelayLabel.Content = procDelay > 0.0 ? $"{string.Format(Properties.Resources.OutputDelay, procDelay)} (Max: {maxProcDelay:0.00} ms)" : Properties.Resources.OutputDelayNA;
                }
                lastProcDelayInt = procDelayInt;
            }
        }
    }
}
