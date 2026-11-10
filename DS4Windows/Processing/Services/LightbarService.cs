using System;
using static System.Math;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="ILightbarService"/> の実装。ライトバーの色（虹色、電池連動、点滅、充電中、無操作のフェード、強制表示、終了時の消灯、距離プロファイル）を計算する。
    /// スロットごとの経過（虹色の角度、フェードの向きと開始時刻）をこのインスタンスが持つ。時間は <see cref="TimeProvider"/> から読む（テストでは仮想時間）。
    /// </summary>
    public class LightbarService : ILightbarService
    {
        private readonly static byte[/* Light On duration */, /* Light Off duration */] BatteryIndicatorDurations =
        {
            { 28, 252 }, // on 10% of the time at 0
            { 28, 252 },
            { 56, 224 },
            { 84, 196 },
            { 112, 168 },
            { 140, 140 },
            { 168, 112 },
            { 196, 84 },
            { 224, 56 }, // on 80% of the time at 80, etc.
            { 252, 28 }, // on 90% of the time at 90
            { 0, 0 }     // use on 100%. 0 is for "charging" OR anything sufficiently-"charged"
        };

        private readonly IProfileSettingsService profileSettings;
        private readonly LightbarOverrides overrides;
        private readonly TimeProvider time;

        private readonly double[] counters = new double[ControllerLimits.MaxControllerCount];
        private readonly bool[] fadedirection = new bool[ControllerLimits.MaxControllerCount];
        // フェードの計測（旧 Stopwatch の IsRunning／Restart／ElapsedMilliseconds／Stop と同じ意味）
        private readonly bool[] fadeRunning = new bool[ControllerLimits.MaxControllerCount];
        private readonly long[] fadeStart = new long[ControllerLimits.MaxControllerCount];
        private readonly DateTime[] oldnow = new DateTime[ControllerLimits.MaxControllerCount];

        internal const int PULSE_FLASH_DURATION = 2000;
        internal const double PULSE_FLASH_SEGMENTS = PULSE_FLASH_DURATION / 40;
        internal const int PULSE_CHARGING_DURATION = 4000;
        internal const double PULSE_CHARGING_SEGMENTS = (PULSE_CHARGING_DURATION / 40) - 2;

        public LightbarService(IProfileSettingsService profileSettings, LightbarOverrides overrides, TimeProvider time)
        {
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            this.overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            DateTime start = UtcNow();
            for (int i = 0; i < oldnow.Length; i++) oldnow[i] = start;
        }

        private DateTime UtcNow() => time.GetUtcNow().UtcDateTime;

        private void RestartFade(int deviceNum)
        {
            fadeRunning[deviceNum] = true;
            fadeStart[deviceNum] = time.GetTimestamp();
        }

        private long FadeElapsedMilliseconds(int deviceNum)
        {
            return (long)time.GetElapsedTime(fadeStart[deviceNum]).TotalMilliseconds;
        }

        public void Update(ILightbarDevice device, int deviceNum)
        {
            DS4Color color = new DS4Color();
            bool useForceLight = this.overrides.forcelight[deviceNum];
            LightbarSettingInfo lightbarSettingInfo = this.profileSettings.GetLightbarSettingsInfo(deviceNum);
            LightbarDS4WinInfo lightModeInfo = lightbarSettingInfo.ds4winSettings;
            bool useLightRoutine = lightbarSettingInfo.mode == LightbarMode.DS4Win;
            //bool useLightRoutine = false;
            if (!this.overrides.defaultLight && !useForceLight && useLightRoutine)
            {
                if (lightModeInfo.useCustomLed)
                {
                    color = lightModeInfo.m_CustomLed; //getCustomColor(deviceNum);
                }
                else
                {
                    double rainbow = lightModeInfo.rainbow;// getRainbow(deviceNum);
                    if (rainbow > 0)
                    {
                        // Display rainbow
                        DateTime now = UtcNow();
                        if (now >= oldnow[deviceNum] + TimeSpan.FromMilliseconds(10)) //update by the millisecond that way it's a smooth transtion
                        {
                            int diffMs = now.Subtract(oldnow[deviceNum]).Milliseconds;
                            oldnow[deviceNum] = now;
                            if (device.IsCharging)
                                //counters[deviceNum] -= 1.5 * 3 / rainbow;
                                counters[deviceNum] -= 360.0 * (diffMs / 1000.0 / rainbow);
                            else
                                //counters[deviceNum] += 1.5 * 3 / rainbow;
                                counters[deviceNum] += 360.0 * (diffMs / 1000.0 / rainbow);
                        }

                        if (counters[deviceNum] < 0)
                            counters[deviceNum] = 180000;
                        else if (counters[deviceNum] > 180000)
                            counters[deviceNum] = 0;

                        double maxSat = lightModeInfo.maxRainbowSat; // GetMaxSatRainbow(deviceNum);
                        if (lightModeInfo.ledAsBattery)
                        {
                            byte useSat = (byte)(maxSat == 1.0 ?
                                device.Battery * 2.55 :
                                device.Battery * 2.55 * maxSat);
                            color = ColorUtil.HueToRgb((float)counters[deviceNum] % 360, useSat);
                        }
                        else
                            color = ColorUtil.HueToRgb((float)counters[deviceNum] % 360,
                                (byte)(maxSat == 1.0 ? 255 : 255 * maxSat));

                    }
                    else if (lightModeInfo.ledAsBattery)
                    {
                        ref DS4Color fullColor = ref lightModeInfo.m_Led; //ref getMainColor(deviceNum);
                        ref DS4Color lowColor = ref lightModeInfo.m_LowLed; //ref getLowColor(deviceNum);
                        color = ColorUtil.getTransitionedColor(ref lowColor, ref fullColor, device.Battery);
                    }
                    else
                    {
                        color = this.profileSettings.GetMainColor(deviceNum);
                    }
                }

                if (device.Battery <= lightModeInfo.flashAt && !this.overrides.defaultLight && !device.IsCharging)
                {
                    ref DS4Color flashColor = ref lightModeInfo.m_FlashLed; //ref getFlashColor(deviceNum);
                    if (!(flashColor.red == 0 &&
                        flashColor.green == 0 &&
                        flashColor.blue == 0))
                        color = flashColor;

                    if (lightModeInfo.flashType == 1)
                    {
                        double ratio = 0.0;

                        if (!fadeRunning[deviceNum])
                        {
                            bool temp = fadedirection[deviceNum];
                            fadedirection[deviceNum] = !temp;
                            RestartFade(deviceNum);
                            ratio = temp ? 100.0 : 0.0;
                        }
                        else
                        {
                            long elapsed = FadeElapsedMilliseconds(deviceNum);

                            if (fadedirection[deviceNum])
                            {
                                if (elapsed < PULSE_FLASH_DURATION)
                                {
                                    elapsed = elapsed / 40;
                                    ratio = 100.0 * (elapsed / PULSE_FLASH_SEGMENTS);
                                }
                                else
                                {
                                    ratio = 100.0;
                                    fadeRunning[deviceNum] = false;
                                }
                            }
                            else
                            {
                                if (elapsed < PULSE_FLASH_DURATION)
                                {
                                    elapsed = elapsed / 40;
                                    ratio = (0 - 100.0) * (elapsed / PULSE_FLASH_SEGMENTS) + 100.0;
                                }
                                else
                                {
                                    ratio = 0.0;
                                    fadeRunning[deviceNum] = false;
                                }
                            }
                        }

                        DS4Color tempCol = new DS4Color(0, 0, 0);
                        color = ColorUtil.getTransitionedColor(ref color, ref tempCol, ratio);
                    }
                }

                int idleDisconnectTimeout = this.profileSettings.GetIdleDisconnectTimeout(deviceNum);
                if (idleDisconnectTimeout > 0 && lightModeInfo.ledAsBattery &&
                    (!device.IsCharging || device.Battery >= 100))
                {
                    // Fade lightbar by idle time
                    TimeSpan timeratio = new TimeSpan(UtcNow().Ticks - device.LastActive.Ticks);
                    double botratio = timeratio.TotalMilliseconds;
                    double topratio = TimeSpan.FromSeconds(idleDisconnectTimeout).TotalMilliseconds;
                    double ratio = 100.0 * (botratio / topratio), elapsed = ratio;
                    if (ratio >= 50.0 && ratio < 100.0)
                    {
                        DS4Color emptyCol = new DS4Color(0, 0, 0);
                        color = ColorUtil.getTransitionedColor(ref color, ref emptyCol,
                            (uint)(-100.0 * (elapsed = 0.02 * (ratio - 50.0)) * (elapsed - 2.0)));
                    }
                    else if (ratio >= 100.0)
                    {
                        DS4Color emptyCol = new DS4Color(0, 0, 0);
                        color = ColorUtil.getTransitionedColor(ref color, ref emptyCol, 100.0);
                    }
                        
                }

                if (device.IsCharging && device.Battery < 100)
                {
                    switch (lightModeInfo.chargingType)
                    {
                        case 1:
                        {
                            double ratio = 0.0;

                            if (!fadeRunning[deviceNum])
                            {
                                bool temp = fadedirection[deviceNum];
                                fadedirection[deviceNum] = !temp;
                                RestartFade(deviceNum);
                                ratio = temp ? 100.0 : 0.0;
                            }
                            else
                            {
                                long elapsed = FadeElapsedMilliseconds(deviceNum);

                                if (fadedirection[deviceNum])
                                {
                                    if (elapsed < PULSE_CHARGING_DURATION)
                                    {
                                        elapsed = elapsed / 40;
                                        if (elapsed > PULSE_CHARGING_SEGMENTS)
                                            elapsed = (long)PULSE_CHARGING_SEGMENTS;
                                        ratio = 100.0 * (elapsed / PULSE_CHARGING_SEGMENTS);
                                    }
                                    else
                                    {
                                        ratio = 100.0;
                                        fadeRunning[deviceNum] = false;
                                    }
                                }
                                else
                                {
                                    if (elapsed < PULSE_CHARGING_DURATION)
                                    {
                                        elapsed = elapsed / 40;
                                        if (elapsed > PULSE_CHARGING_SEGMENTS)
                                            elapsed = (long)PULSE_CHARGING_SEGMENTS;
                                        ratio = (0 - 100.0) * (elapsed / PULSE_CHARGING_SEGMENTS) + 100.0;
                                    }
                                    else
                                    {
                                        ratio = 0.0;
                                        fadeRunning[deviceNum] = false;
                                    }
                                }
                            }

                            DS4Color emptyCol = new DS4Color(0, 0, 0);
                            color = ColorUtil.getTransitionedColor(ref color, ref emptyCol, ratio);
                            break;
                        }
                        case 2:
                        {
                            counters[deviceNum] += 0.167;
                            color = ColorUtil.HueToRgb((float)counters[deviceNum] % 360, 255);
                            break;
                        }
                        case 3:
                        {
                            color = lightModeInfo.m_ChargingLed; //getChargingColor(deviceNum);
                            break;
                        }
                        default: break;
                    }
                }
            }
            else if (useForceLight)
            {
                color = this.overrides.forcedColor[deviceNum];
                useLightRoutine = true;
            }
            else if (this.overrides.shuttingdown)
            {
                color = new DS4Color(0, 0, 0);
                useLightRoutine = true;
            }
            else if (useLightRoutine)
            {
                if (device.ConnectionType == ConnectionType.BT)
                    color = new DS4Color(32, 64, 64);
                else
                    color = new DS4Color(0, 0, 0);
            }

            if (useLightRoutine)
            {
                bool distanceprofile = this.profileSettings.DistanceProfiles[deviceNum] || this.profileSettings.TempProfileDistanceArray[deviceNum];
                //distanceprofile = (ProfilePath[deviceNum].ToLower().Contains("distance") || tempprofilename[deviceNum].ToLower().Contains("distance"));
                if (distanceprofile && !this.overrides.defaultLight)
                {
                    // Thing I did for Distance
                    float rumble = device.LeftHeavySlowRumble / 2.55f;
                    byte max = Max(color.red, Max(color.green, color.blue));
                    if (device.LeftHeavySlowRumble > 100)
                    {
                        DS4Color maxCol = new DS4Color(max, max, 0);
                        DS4Color redCol = new DS4Color(255, 0, 0);
                        color = ColorUtil.getTransitionedColor(ref maxCol, ref redCol, rumble);
                    }
                    else
                    {
                        DS4Color maxCol = new DS4Color(max, max, 0);
                        DS4Color redCol = new DS4Color(255, 0, 0);
                        DS4Color tempCol = ColorUtil.getTransitionedColor(ref maxCol,
                            ref redCol, 39.6078f);
                        color = ColorUtil.getTransitionedColor(ref color, ref tempCol,
                            device.LeftHeavySlowRumble);
                    }
                }

                /*DS4HapticState haptics = new DS4HapticState
                {
                    LightBarColor = color
                };
                */
                DS4LightbarState lightState = new DS4LightbarState
                {
                    LightBarColor = color,
                };

                if (lightState.IsLightBarSet())
                {
                    if (useForceLight && this.overrides.forcedFlash[deviceNum] > 0)
                    {
                        lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = (byte)(25 - this.overrides.forcedFlash[deviceNum]);
                        lightState.LightBarExplicitlyOff = true;
                    }
                    else if (device.Battery <= lightModeInfo.flashAt && lightModeInfo.flashType == 0 && !this.overrides.defaultLight && !device.IsCharging)
                    {
                        int level = device.Battery / 10;
                        if (level >= 10)
                            level = 10; // all values of >~100% are rendered the same

                        lightState.LightBarFlashDurationOn = BatteryIndicatorDurations[level, 0];
                        lightState.LightBarFlashDurationOff = BatteryIndicatorDurations[level, 1];
                    }
                    else if (distanceprofile && device.LeftHeavySlowRumble > 155) //also part of Distance
                    {
                        lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = (byte)((-device.LeftHeavySlowRumble + 265));
                        lightState.LightBarExplicitlyOff = true;
                    }
                    else
                    {
                        //haptics.LightBarFlashDurationOff = haptics.LightBarFlashDurationOn = 1;
                        lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = 0;
                        lightState.LightBarExplicitlyOff = true;
                    }
                }
                else
                {
                    lightState.LightBarExplicitlyOff = true;
                }

                byte tempLightBarOnDuration = device.LightBarOnDuration;
                if (tempLightBarOnDuration != lightState.LightBarFlashDurationOn && tempLightBarOnDuration != 1 && lightState.LightBarFlashDurationOn == 0)
                    lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = 1;

                device.SetLightbarState(ref lightState);
                //device.SetHapticState(ref haptics);
                //device.pushHapticState(ref haptics);
            }
        }

    }
}
