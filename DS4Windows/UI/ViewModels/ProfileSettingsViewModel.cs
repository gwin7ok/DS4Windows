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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    public class ProfileSettingsViewModel : ObservableObject
    {
        private int device;
        public int Device { get => device; }

        /// <summary>「ジャイロ」タブの ViewModel。</summary>
        public GyroSettingsViewModel Gyro { get; }

        /// <summary>「タッチパッド」タブの ViewModel。</summary>
        public TouchpadSettingsViewModel Touchpad { get; }

        /// <summary>「L2 &amp; R2」（トリガー）の ViewModel。</summary>
        public TriggerSettingsViewModel Triggers { get; }

        /// <summary>「LS」「RS」（スティック）の ViewModel。</summary>
        public StickSettingsViewModel Sticks { get; }
        private readonly IProfileSettingsService profileSettings;
        private readonly IProfileLoadTarget loadTarget;   // Phase13-Step6 6-e: 旧 ControlService controlService（ジャイロ系 setter と空プロファイルの読込に渡すだけだった）
        private readonly IEnvironmentService environmentService;
        private readonly IPathService pathService;
        private readonly IOutputSlotService outputSlotService;
        private readonly IProfileRepository profileRepo;
        private readonly IProfilePresetLoader presetLoader;

        private int funcDevNum;
        public int FuncDevNum { get => funcDevNum; }

        // Phase6-Step7b: 設定の読み書き先（Device＝編集スロット）とは別に、ランブルテスト・ライトバーの
        // プレビュー・校正などで実際に使うコントローラーのスロット番号を持つ。-1 は「実機なし」
        // （プロファイル一覧から開いた場合）。詳細は docs-forDIMG/MadeByAgent/Phase6-Step7b-Plan.md §2.1
        private readonly int targetDevice;
        public int TargetDevice { get => targetDevice; }

        private ImageBrush lightbarImgBrush = new ImageBrush();
        private SolidColorBrush lightbarColBrush = new SolidColorBrush();

        public int LightbarModeIndex
        {
            get
            {
                int index = 0;
                switch (profileSettings.LightbarSettingsInfo[device].Mode)
                {
                    case LightbarMode.DS4Win:
                        index = 0; break;
                    case LightbarMode.Passthru:
                        index = 1; break;
                    default: break;
                }

                return index;
            }
            set
            {
                LightbarMode temp = LightbarMode.DS4Win;
                switch (value)
                {
                    case 0:
                        temp = LightbarMode.DS4Win; break;
                    case 1:
                        temp = LightbarMode.Passthru; break;
                    default: break;
                }

                profileSettings.LightbarSettingsInfo[device].Mode = temp;
                OnPropertyChanged(nameof(LightbarModeIndex));
            }
        }
        public System.Windows.Media.Brush LightbarBrush
        {
            get
            {
                System.Windows.Media.Brush tempBrush;
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led;
                if (!RainbowExists)
                {
                    lightbarColBrush.Color = new System.Windows.Media.Color()
                    {
                        A = 255,
                        R = color.red,
                        G = color.green,
                        B = color.blue
                    };
                    tempBrush = lightbarColBrush as System.Windows.Media.Brush;
                }
                else
                {
                    tempBrush = lightbarImgBrush as System.Windows.Media.Brush;
                }

                return tempBrush;
            }
        }
        public System.Windows.Media.Color MainColor
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led; //ref Global.MainColor[device];
                return new System.Windows.Media.Color()
                {
                    A = 255,
                    R = color.red,
                    G = color.green,
                    B = color.blue
                };
            }
        }
        public string MainColorString
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led; //ref Global.MainColor[device];
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
                /*return new System.Windows.Media.Color()
                {
                    A = 255,
                    R = color.red,
                    G = color.green,
                    B = color.blue
                }.ToString();
                */
            }
        }
        public int MainColorR
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.red;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.red = (byte)value;
                OnPropertyChanged(nameof(MainColorR));
            }
        }
        public string MainColorRString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.red.ToString("X2")}FF0000";
        }
        public int MainColorG
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.green;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.green = (byte)value;
                OnPropertyChanged(nameof(MainColorG));
            }
        }
        public string MainColorGString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.green.ToString("X2")}00FF00";
        }
        public int MainColorB
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.blue;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.blue = (byte)value;
                OnPropertyChanged(nameof(MainColorB));
            }
        }
        public string MainColorBString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led.blue.ToString("X2")}0000FF";
        }
        public string LowColor
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed; //ref Global.LowColor[device];
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public int LowColorR
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.red;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.red = (byte)value;
                OnPropertyChanged(nameof(LowColorR));
                OnPropertyChanged(nameof(LowColorRString));
            }
        }
        public string LowColorRString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.red.ToString("X2")}FF0000";
        }
        public int LowColorG
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.green;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.green = (byte)value;
                OnPropertyChanged(nameof(LowColorG));
                OnPropertyChanged(nameof(LowColorGString));
            }
        }
        public string LowColorGString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.green.ToString("X2")}00FF00";
        }
        public int LowColorB
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.blue;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.blue = (byte)value;
                OnPropertyChanged(nameof(LowColorB));
                OnPropertyChanged(nameof(LowColorBString));
            }
        }
        public string LowColorBString
        {
            get => $"#{profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed.blue.ToString("X2")}0000FF";
        }
        public System.Windows.Media.Color LowColorMedia
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed;
                return new System.Windows.Media.Color()
                {
                    A = 255,
                    R = color.red,
                    B = color.blue,
                    G = color.green
                };
            }
        }

        public int FlashTypeIndex
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.flashType; //Global.FlashType[device];
            set => profileSettings.LightbarSettingsInfo[device].ds4winSettings.flashType = (byte)value;
        }

        public int FlashAt
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.flashAt; //Global.FlashAt[device];
            set => profileSettings.LightbarSettingsInfo[device].ds4winSettings.flashAt = value;
        }

        public string FlashColor
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_FlashLed;
                if (color.red == 0 && color.green == 0 && color.blue == 0)
                {
                    color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led;
                }

                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public System.Windows.Media.Color FlashColorMedia
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_FlashLed;
                if (color.red == 0 && color.green == 0 && color.blue == 0)
                {
                    color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led;
                }

                return new System.Windows.Media.Color()
                {
                    A = 255,
                    R = color.red,
                    B = color.blue,
                    G = color.green
                };
            }
        }

        public int ChargingType
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.chargingType;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.chargingType = value;
                OnPropertyChanged(nameof(ChargingColorVisible));
            }
        }

        public bool ColorBatteryPercent
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.ledAsBattery;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.ledAsBattery = value;
            }
        }

        public string ChargingColor
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_ChargingLed;
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public System.Windows.Media.Color ChargingColorMedia
        {
            get
            {
                ref DS4Color color = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_ChargingLed;
                return new System.Windows.Media.Color()
                {
                    A = 255,
                    R = color.red,
                    B = color.blue,
                    G = color.green
                };
            }
        }

        public Visibility ChargingColorVisible
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.chargingType == 3 ? Visibility.Visible : Visibility.Hidden;
        }
        public double Rainbow
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.rainbow;
            set
            {
                profileSettings.LightbarSettingsInfo[device].ds4winSettings.rainbow = value;
                OnPropertyChanged(nameof(Rainbow));
                OnPropertyChanged(nameof(RainbowExists));
            }
        }
        public bool RainbowExists
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.rainbow != 0.0;
        }

        public double MaxSatRainbow
        {
            get => profileSettings.LightbarSettingsInfo[device].ds4winSettings.maxRainbowSat * 100.0;
            set => profileSettings.LightbarSettingsInfo[device].ds4winSettings.maxRainbowSat = value / 100.0;
        }

        // Phase11 K-W6-10: DebouncingMs（デバウンス時間）のプロパティとイベントは機能廃止で削除した

        // DS3 の傾きの擬似はジャイロのタブにある。保存時の補正（DS4 への補正）と、画面の再起動の確認が使うため、窓口をここにも残す
        public bool HasUseDs3PitchRollSimChanged => Gyro.HasUseDs3PitchRollSimChanged;

        public bool UseDs3PitchRollSim
        {
            get => Gyro.UseDs3PitchRollSim;
            set => Gyro.UseDs3PitchRollSim = value;
        }

        /// <summary>サービスの再起動を求めたあとに、<see cref="HasUseDs3PitchRollSimChanged"/> の印を戻す。同じ変更のために、再起動をくり返し求めないようにするため。</summary>
        public void AcknowledgeUseDs3PitchRollSimChange() => Gyro.AcknowledgeUseDs3PitchRollSimChange();

        public int RumbleBoost
        {
            get => profileSettings.RumbleBoost[device];
            set => profileSettings.RumbleBoost[device] = (byte)value;
        }

        public int RumbleAutostopTime
        {
            // RumbleAutostopTime value is in milliseconds in XML config file, but GUI uses just seconds
            get => profileSettings.GetRumbleAutostopTime(device) / 1000;
            set => profileSettings.SetRumbleAutostopTime(device, value * 1000);
        }

        private bool heavyRumbleActive;
        public bool HeavyRumbleActive
        {
            get => heavyRumbleActive;
            set
            {
                heavyRumbleActive = value;
                OnPropertyChanged(nameof(HeavyRumbleActive));
            }
        }
        private bool lightRumbleActive;
        public bool LightRumbleActive
        {
            get => lightRumbleActive;
            set
            {
                lightRumbleActive = value;
                OnPropertyChanged(nameof(LightRumbleActive));
            }
        }
        public bool UseControllerReadout
        {
            get => profileSettings.DS4Mapping;
            set => profileSettings.DS4Mapping = value;
        }

        public int ButtonMouseSensitivity
        {
            get => profileSettings.ButtonMouseInfos[device].buttonSensitivity;
            set
            {
                int temp = profileSettings.ButtonMouseInfos[device].buttonSensitivity;
                if (temp == value) return;
                profileSettings.ButtonMouseInfos[device].ButtonSensitivity = value;
                OnPropertyChanged(nameof(ButtonMouseSensitivity));
            }
        }
        public int ButtonMouseVerticalScale
        {
            get => Convert.ToInt32(profileSettings.ButtonMouseInfos[device].buttonVerticalScale * 100.0);
            set
            {
                double temp = profileSettings.ButtonMouseInfos[device].buttonVerticalScale;
                double attemptValue = value * 0.01;
                if (temp == attemptValue) return;
                profileSettings.ButtonMouseInfos[device].buttonVerticalScale = attemptValue;
                OnPropertyChanged(nameof(ButtonMouseVerticalScale));
            }
        }
        private double RawButtonMouseOffset
        {
            get => profileSettings.ButtonMouseInfos[device].mouseVelocityOffset;
        }

        public double ButtonMouseOffset
        {
            get => profileSettings.ButtonMouseInfos[device].mouseVelocityOffset * 100.0;
            set
            {
                double temp = profileSettings.ButtonMouseInfos[device].mouseVelocityOffset * 100.0;
                if (temp == value) return;
                profileSettings.ButtonMouseInfos[device].mouseVelocityOffset = value * 0.01;
                OnPropertyChanged(nameof(ButtonMouseOffset));
            }
        }
        private int outputMouseSpeed;
        public int OutputMouseSpeed
        {
            get => outputMouseSpeed;
            set
            {
                if (value == outputMouseSpeed) return;
                outputMouseSpeed = value;
                OnPropertyChanged(nameof(OutputMouseSpeed));
            }
        }
        private double mouseOffsetSpeed;
        public double MouseOffsetSpeed
        {
            get => mouseOffsetSpeed;
            set
            {
                if (mouseOffsetSpeed == value) return;
                mouseOffsetSpeed = value;
                OnPropertyChanged(nameof(MouseOffsetSpeed));
            }
        }
        public bool MouseAcceleration
        {
            get => profileSettings.ButtonMouseInfos[device].mouseAccel;
            set => profileSettings.ButtonMouseInfos[device].mouseAccel = value;
        }

        public bool EnableTouchpadToggle
        {
            get => profileSettings.EnableTouchToggle[device];
            set => profileSettings.EnableTouchToggle[device] = value;
        }

        public bool EnableOutputDataToDS4
        {
            get => profileSettings.EnableOutputDataToDS4[device];
            set => profileSettings.EnableOutputDataToDS4[device] = value;
        }

        public bool LaunchProgramExists
        {
            get => !string.IsNullOrEmpty(profileSettings.LaunchProgram[device]);
            set
            {
                if (!value) ResetLauchProgram();
            }
        }
        public string LaunchProgram
        {
            get => profileSettings.LaunchProgram[device];
        }
        public string LaunchProgramName
        {
            get
            {
                string temp = profileSettings.LaunchProgram[device];
                if (!string.IsNullOrEmpty(temp))
                {
                    temp = Path.GetFileNameWithoutExtension(temp);
                }
                else
                {
                    temp = "Browse";
                }

                return temp;
            }
        }
        public ImageSource LaunchProgramIcon
        {
            get
            {
                ImageSource exeicon = null;
                string path = profileSettings.LaunchProgram[device];
                if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".exe")
                {
                    using (Icon ico = Icon.ExtractAssociatedIcon(path))
                    {
                        exeicon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        exeicon.Freeze();
                    }
                }

                return exeicon;
            }
        }
        public bool DInputOnly
        {
            get => profileSettings.DinputOnly[device];
            set
            {
                bool temp = profileSettings.DinputOnly[device];
                if (temp == value) return;

                profileSettings.DinputOnly[device] = value;
                OnPropertyChanged(nameof(DInputOnly));
            }
        }

        public bool VirtualTriggerButtons
        {
            get => profileSettings.OutputVirtualTriggerButton[device];
            set
            {
                bool temp = profileSettings.OutputVirtualTriggerButton[device];
                if (temp == value) return;

                profileSettings.OutputVirtualTriggerButton[device] = value;
                OnPropertyChanged(nameof(VirtualTriggerButtons));
            }
        }
        private List<EnumChoiceSelection<DS4TriggerOutputMode>> outputDS4TriggerModeOptions =
            new List<EnumChoiceSelection<DS4TriggerOutputMode>>
            {
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.DS4OutputTriggersDefault, DS4TriggerOutputMode.Default),
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.Analog, DS4TriggerOutputMode.Analog),
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.Buttons, DS4TriggerOutputMode.Buttons),
            };

        public List<EnumChoiceSelection<DS4TriggerOutputMode>> OutputDS4TriggerModeOptions => outputDS4TriggerModeOptions;

        public DS4TriggerOutputMode OutputDS4TriggerMode
        {
            get => profileSettings.OutputDS4TriggerMode[device];
            set
            {
                DS4TriggerOutputMode temp = profileSettings.OutputDS4TriggerMode[device];
                if (temp == value) return;

                profileSettings.OutputDS4TriggerMode[device] = value;
                OnPropertyChanged(nameof(OutputDS4TriggerMode));
            }
        }
        public bool IdleDisconnectExists
        {
            get => profileSettings.IdleDisconnectTimeout[device] != 0;
            set
            {
                // If enabling Idle Disconnect, set default time.
                // Otherwise, set time to 0 to mean disabled
                profileSettings.IdleDisconnectTimeout[device] = value ?
                    BackingStore.DEFAULT_ENABLE_IDLE_DISCONN_MINS * 60 : 0;

                OnPropertyChanged(nameof(IdleDisconnect));
                OnPropertyChanged(nameof(IdleDisconnectExists));
            }
        }
        public int IdleDisconnect
        {
            get => profileSettings.IdleDisconnectTimeout[device] / 60;
            set
            {
                int temp = profileSettings.IdleDisconnectTimeout[device] / 60;
                if (temp == value) return;
                profileSettings.IdleDisconnectTimeout[device] = value * 60;
                OnPropertyChanged(nameof(IdleDisconnect));
                OnPropertyChanged(nameof(IdleDisconnectExists));
            }
        }
        private int tempBtPollRate;
        public int TempBTPollRateIndex
        {
            get => profileSettings.BTPollRate[device];
            set
            {
                if (profileSettings.BTPollRate[device] == value) return;
                profileSettings.BTPollRate[device] = value;
            }
        }
        public int ControllerTypeIndex
        {
            get => profileSettings.OutContType[device] == OutContType.DS4 ? 1 : 0;
            set
            {
                OutContType target = value == 1 ? OutContType.DS4 : OutContType.X360;
                if (profileSettings.OutContType[device] == target) return;
                profileSettings.OutContType[device] = target;
            }
        }

        private int tempControllerIndex;
        public int TempControllerIndex
        {
            get => tempControllerIndex;
            set
            {
                if (tempControllerIndex == value) return;
                tempControllerIndex = value;
                OutContType type = value == 1 ? OutContType.DS4 : OutContType.X360;
                profileSettings.OutContType[device] = type;
                // Phase11 W6 K-W6-4: 出力ピッカー（BindingWindow）は OutDevTypeTemp から、Pad タブの表示（DS4／Xbox 360）を決める。
                // 以前はここで更新せず、編集画面を開き直すまで前の種類のままだった
                outputSlotService.OutDevTypeTemp[device] = type;
            }
        }

        public OutContType TempConType
        {
            get => tempControllerIndex == 1 ? OutContType.DS4 : OutContType.X360;
            set
            {
                int newIndex = value == OutContType.DS4 ? 1 : 0;
                if (tempControllerIndex == newIndex) return;
                TempControllerIndex = newIndex;
            }
        }
        /// <summary>
        /// Phase11 W4（Issue #4 の 1）: DS4 の出力でしか成り立たない設定が ON なのに、出力の種類（Emulated Controller）が DS4 でないとき、
        /// DS4 へ補正する。保存・適用の両方が通る <c>ProfileEditor.ExecuteSaveOrApply</c> から、保存する前に呼ぶ。
        /// 2026-09-14 の保存・適用の統合（<c>9da60b85</c>）で失われた、旧 <c>ProfileEditor</c> の補正の復元（旧コードの動きに合わせる）:
        /// <list type="bullet">
        /// <item>(a) <see cref="UseDs3PitchRollSim"/> が ON なら DS4（旧 <c>SaveBtn_Click</c>。GUI ログなし）</item>
        /// <item>(c) <see cref="EnableOutputDataToDS4"/> が ON で、(a) のあとも DS4 でなければ DS4（旧 <c>SetLateProperties</c> の
        /// Phase5-Step14 (c)-2。呼び出し側が GUI ログを出す）</item>
        /// </list>
        /// OFF のときは何もしない（DS4 から X360 へ戻すことはしない）。
        /// 「DS4 でない」の判定は、保存される値（<see cref="ControllerTypeIndex"/>）と画面の選択（<see cref="TempControllerIndex"/>）の
        /// どちらかが DS4 でなければ、とする（ずれていても、保存される値を DS4 にそろえるため）。
        /// </summary>
        /// <returns>DS4 へ変えた理由。変えなかった場合は <see cref="Ds4OutputCorrectionReason.None"/></returns>
        public Ds4OutputCorrectionReason CorrectOutputTypeToDs4()
        {
            bool notDs4 = ControllerTypeIndex != 1 || TempControllerIndex != 1;
            if (!notDs4)
            {
                return Ds4OutputCorrectionReason.None;
            }

            Ds4OutputCorrectionReason reason;
            if (UseDs3PitchRollSim)
            {
                reason = Ds4OutputCorrectionReason.Ds3PitchRollSim;
            }
            // Phase11 W6c（K-W6-12。ユーザー指示 2026-11-03）: (c)［EnableOutputDataToDS4 が ON なら DS4 へ強制］は廃止した。
            // この設定は「物理コントローラーへ出力データを送るか」であり、仮想パッドの種類（Emulated Controller）とは無関係
            else
            {
                return Ds4OutputCorrectionReason.None;
            }

            ControllerTypeIndex = 1;   // 保存される値（profileSettings.OutContType）
            TempControllerIndex = 1;   // 画面の選択
            return reason;
        }

        /// <summary>
        /// Phase6-Step8-3: キャンセル時に、編集中に変えた一時的な出力種別（<c>OutDevTypeTemp</c>）を X360 へ戻す。
        /// ProfileEditor.CancelBtn_Click の 旧 <c>Global.outDevTypeTemp[deviceNum] = X360</c>（現在は OutputSlotService の配列）の置換。
        /// </summary>
        public void ResetOutDevTypeTemp()
        {
            outputSlotService.OutDevTypeTemp[device] = OutContType.X360;
        }

        // ---- Phase6-Step8-4: ProfileEditor のバックエンド操作（保存・ブランク読み込み・キャッシュ更新）----
        // 編集スロット（device）に対する操作だけを行い、コントローラーのスロットへの再適用はしない
        // （再適用は ProfileSaved を受けた MainWindow.SyncProfileListAndControllers の責務。Step7b 決定8）。

        /// <summary>編集スロットの内容を、プロファイル <paramref name="profileName"/> として保存する。保存の成否を返す。</summary>
        public bool SaveProfile(string profileName)
        {
            return profileRepo.SaveProfile(device, profileName);
        }

        /// <summary>
        /// 編集スロットへ空のプロファイルを読み込む（新規作成でプリセット選択をキャンセルしたとき）。
        /// Phase13-Step6 6-e: 読込先の窓口（<see cref="IProfileLoadTarget"/>）は、呼び出し元から渡さず、この ViewModel が持つものを使う。
        /// </summary>
        public void LoadBlankProfile()
        {
            presetLoader.LoadBlank(device, false, loadTarget, false);
        }

        /// <summary>編集スロットのカスタムフラグのキャッシュを更新する（ボタン設定画面を閉じた後など）。</summary>
        public void RefreshCustomsFlags()
        {
            profileRepo.CacheProfileCustomsFlags(device);
        }

        /// <summary>編集スロットの拡張プロファイル情報（スペシャルアクション参照など）のキャッシュを更新する。</summary>
        public void RefreshExtraProfileInfo()
        {
            profileRepo.CacheExtraProfileInfo(device);
        }

        public OutContType ContType
        {
            // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク2）: 参照先を
            // outputSlotService.GetOutputDeviceType(device) から
            // profileSettings.OutContType（Global.OutContTypeと同一実体）へ修正。
            // （GetOutputDeviceType は Phase6-Step5-2 で削除済み）
            get => profileSettings.OutContType[device];
        }

        public double SXDeadZone
        {
            get => profileSettings.SXDeadzone[device];
            set
            {
                double temp = profileSettings.SXDeadzone[device];
                if (temp == value) return;
                profileSettings.SXDeadzone[device] = value;
                OnPropertyChanged(nameof(SXDeadZone));
            }
        }
        public double SZDeadZone
        {
            get => profileSettings.SZDeadzone[device];
            set
            {
                double temp = profileSettings.SZDeadzone[device];
                if (temp == value) return;
                profileSettings.SZDeadzone[device] = value;
                OnPropertyChanged(nameof(SZDeadZone));
            }
        }
        public double SXMaxZone
        {
            get => profileSettings.SXMaxzone[device];
            set => profileSettings.SXMaxzone[device] = value;
        }

        public double SZMaxZone
        {
            get => profileSettings.SZMaxzone[device];
            set => profileSettings.SZMaxzone[device] = value;
        }

        public double SXAntiDeadZone
        {
            get => profileSettings.SXAntiDeadzone[device];
            set => profileSettings.SXAntiDeadzone[device] = value;
        }

        public double SZAntiDeadZone
        {
            get => profileSettings.SZAntiDeadzone[device];
            set => profileSettings.SZAntiDeadzone[device] = value;
        }

        public double SXSens
        {
            get => profileSettings.SXSens[device];
            set => profileSettings.SXSens[device] = value;
        }

        public double SZSens
        {
            get => profileSettings.SZSens[device];
            set => profileSettings.SZSens[device] = value;
        }

        public int SXOutputCurveIndex
        {
            get => profileSettings.GetSxOutCurveMode(device);
            set
            {
                profileSettings.SetSxOutCurveMode(device, value);
                OnPropertyChanged(nameof(SXCustomCurveSelected));
            }
        }

        public int SZOutputCurveIndex
        {
            get => profileSettings.GetSzOutCurveMode(device);
            set
            {
                profileSettings.SetSzOutCurveMode(device, value);
                OnPropertyChanged(nameof(SZCustomCurveSelected));
            }
        }

        public bool SXCustomCurveSelected
        {
            get => profileSettings.GetSxOutCurveMode(device) == 6;
        }
        public bool SZCustomCurveSelected
        {
            get => profileSettings.GetSzOutCurveMode(device) == 6;
        }
        public string SXCustomCurve
        {
            get => profileSettings.SxOutBezierCurveObj[device].CustomDefinition;
            set => profileSettings.SxOutBezierCurveObj[device].InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }

        public string SZCustomCurve
        {
            get => profileSettings.SzOutBezierCurveObj[device].CustomDefinition;
            set => profileSettings.SzOutBezierCurveObj[device].InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }

        private PresetMenuHelper presetMenuUtil;
        public PresetMenuHelper PresetMenuUtil
        {
            get => presetMenuUtil;
        }

        public int DualSenseRumbleEmulationPerIndex
        {
            get
            {
                switch (profileSettings.DualSenseRumbleEmulationMode[device])
                {
                    case DualSenseDevice.RumbleEmulationMode.Disabled: return 2;
                    case DualSenseDevice.RumbleEmulationMode.Legacy: return 1;
                    case DualSenseDevice.RumbleEmulationMode.Accurate: return 0;
                    default: return 0;
                }
            }
            set
            {
                DualSenseDevice.RumbleEmulationMode temp;
                switch (value)
                {
                    case 2: temp = DualSenseDevice.RumbleEmulationMode.Disabled; break;
                    case 1: temp = DualSenseDevice.RumbleEmulationMode.Legacy; break;
                    case 0: temp = DualSenseDevice.RumbleEmulationMode.Accurate; break;
                    default: temp = DualSenseDevice.RumbleEmulationMode.Accurate; break;
                }
                profileSettings.DualSenseRumbleEmulationMode[device] = temp;
            }
        }

        public int DualSenseHapticPowerLevelPerIndex
        {
            get => profileSettings.DualSenseHapticPowerLevel[device];
            set => profileSettings.DualSenseHapticPowerLevel[device] = (byte)value;
        }

        public bool EnableGenericRumbleStrRescaleForDualSenseDevices
        {
            get => profileSettings.UseGenericRumbleStrRescaleForDualSenses[device];
            set => profileSettings.UseGenericRumbleStrRescaleForDualSenses[device] = value;
        }

        public bool UsingMinViGEm173333
        {
            get => environmentService.IsUsingMinViGEm117333();
        }


        public bool InverseRumbleMotors
        {
            get => profileSettings.InverseRumbleMotors[device];
            set => profileSettings.InverseRumbleMotors[device] = value;
        }

        // Phase6-Step9-4: 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // 以前は ?? AppHost.GetService／Global.*Instance／Program.rootHub のフォールバックを持っていた。
        // loadTarget は、ジャイロ系 setter が IProfileSettingsService へ渡す引数と、空プロファイルの読込にだけ使われ、従来も
        // Program.rootHub が null の環境（テスト等）では null だったため、null を許容する（Phase13-Step6 6-e: 旧 ControlService controlService。
        // 生成側の ViewModelFactory が IProfileLoadTarget を直接渡す）。
        // targetDevice は、編集スロット（device）と実機を分けて渡す引数（Phase6-Step7b）。変更しない。
        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public ProfileSettingsViewModel(DS4Windows.Core.Model.LightbarOverrides lightbar, int device, IProfileSettingsService profileSettings,
            IProfileLoadTarget loadTarget,
            IOutputSlotService outputSlotService,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            IEnvironmentService environmentService,
            IPathService pathService,
            int targetDevice = -1)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            this.device = device;
            this.targetDevice = targetDevice;
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            this.loadTarget = loadTarget;
            this.outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            this.presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            // Phase6-Step7b: 実機を使う機能（ランブルテスト、校正、入力ハイライト等）の対象。
            // 実機なし（-1）の場合は、プロファイル一覧から開いたときの従来動作（コントローラー0）を維持する
            funcDevNum = HasTargetDevice ? targetDevice : 0;

            tempControllerIndex = ControllerTypeIndex;

            this.outputSlotService.OutDevTypeTemp[device] = OutContType.X360;
            tempBtPollRate = profileSettings.BTPollRate[device];

            outputMouseSpeed = CalculateOutputMouseSpeed(ButtonMouseSensitivity);
            mouseOffsetSpeed = RawButtonMouseOffset * outputMouseSpeed;

            /*ImageSourceConverter sourceConverter = new ImageSourceConverter();
            ImageSource temp = sourceConverter.
                ConvertFromString($"{ResourcePaths.AssemblyResourcePrefix}component/Resources/rainbowCCrop.png") as ImageSource;
            lightbarImgBrush.ImageSource = temp.Clone();
            */
            Uri tempResourceUri = new Uri($"{ResourcePaths.AssemblyResourcePrefix}component/Resources/rainbowCCrop.png");
            BitmapImage tempBitmap = new BitmapImage();
            tempBitmap.BeginInit();
            // Needed for some systems not using the System default color profile
            tempBitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            tempBitmap.UriSource = tempResourceUri;
            tempBitmap.EndInit();
            lightbarImgBrush.ImageSource = tempBitmap.Clone();

            presetMenuUtil = new PresetMenuHelper(device, this.profileSettings);
            Sticks = new StickSettingsViewModel(this.profileSettings.ForSlot(device).Sticks);
            // スティックの出力モードを変えたら、保存用のプロファイルの印を更新する
            Sticks.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StickSettingsViewModel.LSOutputIndex) || e.PropertyName == nameof(StickSettingsViewModel.RSOutputIndex)) CalcProfileFlags(s, EventArgs.Empty);
            };
            Triggers = new TriggerSettingsViewModel(this.profileSettings.ForSlot(device).Triggers);
            Touchpad = new TouchpadSettingsViewModel(this.profileSettings.ForSlot(device).Touchpad, environmentService);
            Gyro = new GyroSettingsViewModel(this.profileSettings.ForSlot(device).Gyro, loadTarget, environmentService);
            // ジャイロの出力モードを変えたら、保存用のプロファイルの印を更新する
            Gyro.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GyroSettingsViewModel.GyroOutModeIndex)) CalcProfileFlags(s, EventArgs.Empty);
            };

            SetupEvents();
        }

        private void CalcProfileFlags(object sender, EventArgs e)
        {
            profileRepo.CacheProfileCustomsFlags(device);
        }

        private void SetupEvents()
        {
            PropertyChanged += OnOwnPropertyChanged;

            // Phase6-Step7b（決定7＝H1）: 編集操作のたびに実機へ即時に効かせていた 6 件のフックを削除した
            // （GyroOutModeIndexChanging／TouchpadOutputIndexChanging の touchPad.Reset()、TouchpadOutputIndexChanged の
            // PostSetup()、TouchMouseStickTrackballFrictionChanged の ResetTouchStickAccel、LS／RSDeltaAccelEnabledChanged の
            // deltaAccelProcessors のリセット）。プロファイル編集画面の編集スロットが常に作業スロット（8）になり、
            // device < CURRENT_DS4_CONTROLLER_LIMIT の判定を通らなくなったため。同じ処理は、適用・保存時の
            // ApplyProfile → ControlService.PreLoadReset／CheckProfileOptions で行われる（Phase6-Step7b-Plan.md §1A.3）
        }

        /// <summary>
        /// 自分の値が変わったときの連動（派生する表示用プロパティの再通知と、保存用の印の更新）。
        /// 値を設定した直後に同期して実行する。
        /// </summary>
        private void OnOwnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainColor):
                    ProfileSettingsViewModel_MainColorChanged(sender, EventArgs.Empty);
                    break;
                case nameof(MainColorR):
                    OnPropertyChanged(nameof(MainColorRString));
                    OnPropertyChanged(nameof(MainColorString));
                    OnPropertyChanged(nameof(LightbarBrush));
                    break;
                case nameof(MainColorG):
                    OnPropertyChanged(nameof(MainColorGString));
                    OnPropertyChanged(nameof(MainColorString));
                    OnPropertyChanged(nameof(LightbarBrush));
                    break;
                case nameof(MainColorB):
                    OnPropertyChanged(nameof(MainColorBString));
                    OnPropertyChanged(nameof(MainColorString));
                    OnPropertyChanged(nameof(LightbarBrush));
                    break;
                case nameof(Rainbow):
                    OnPropertyChanged(nameof(LightbarBrush));
                    break;
                case nameof(ButtonMouseSensitivity):
                    OutputMouseSpeed = CalculateOutputMouseSpeed(ButtonMouseSensitivity);
                    MouseOffsetSpeed = RawButtonMouseOffset * OutputMouseSpeed;
                    break;
                case nameof(ButtonMouseOffset):
                    ProfileSettingsViewModel_ButtonMouseOffsetChanged(sender, EventArgs.Empty);
                    break;
            }
        }

        private void ProfileSettingsViewModel_ButtonMouseOffsetChanged(object sender,
            EventArgs e)
        {
            MouseOffsetSpeed = RawButtonMouseOffset * OutputMouseSpeed;
        }

        private void ProfileSettingsViewModel_MainColorChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(MainColorString));
            OnPropertyChanged(nameof(MainColorR));
            OnPropertyChanged(nameof(MainColorG));
            OnPropertyChanged(nameof(MainColorB));
            OnPropertyChanged(nameof(LightbarBrush));
        }

        public void UpdateFlashColor(System.Windows.Media.Color color)
        {
            profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_FlashLed = new DS4Color() { red = color.R, green = color.G, blue = color.B };
            OnPropertyChanged(nameof(FlashColor));
        }

        public void UpdateMainColor(System.Windows.Media.Color color)
        {
            profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_Led = new DS4Color() { red = color.R, green = color.G, blue = color.B };
            OnPropertyChanged(nameof(MainColor));
        }

        public void UpdateLowColor(System.Windows.Media.Color color)
        {
            ref DS4Color lowColor = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_LowLed;
            lowColor.red = color.R;
            lowColor.green = color.G;
            lowColor.blue = color.B;

            OnPropertyChanged(nameof(LowColor));
            OnPropertyChanged(nameof(LowColorR));
            OnPropertyChanged(nameof(LowColorG));
            OnPropertyChanged(nameof(LowColorB));
            OnPropertyChanged(nameof(LowColorRString));
            OnPropertyChanged(nameof(LowColorGString));
            OnPropertyChanged(nameof(LowColorBString));
        }

        /// <summary>
        /// 実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。
        /// Phase6-Step7b: ライトバーのプレビュー等、実機に触れる処理はこの判定を使う（編集スロット Device ではない）。
        /// </summary>
        private bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;

        // Phase6-Step7b: ライトバー強制色のプレビューは、編集スロット（Device）ではなく実機（targetDevice）に対して行う
        public void UpdateForcedColor(System.Windows.Media.Color color)
        {
            if (HasTargetDevice)
            {
                DS4Color dcolor = new DS4Color() { red = color.R, green = color.G, blue = color.B };
                _lightbar.forcedColor[targetDevice] = dcolor;
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = true;
            }
        }

        public void StartForcedColor(System.Windows.Media.Color color)
        {
            if (HasTargetDevice)
            {
                DS4Color dcolor = new DS4Color() { red = color.R, green = color.G, blue = color.B };
                _lightbar.forcedColor[targetDevice] = dcolor;
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = true;
            }
        }

        public void EndForcedColor()
        {
            if (HasTargetDevice)
            {
                _lightbar.forcedColor[targetDevice] = new DS4Color(0, 0, 0);
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = false;
            }
        }

        public void UpdateChargingColor(System.Windows.Media.Color color)
        {
            ref DS4Color chargeColor = ref profileSettings.LightbarSettingsInfo[device].ds4winSettings.m_ChargingLed;
            chargeColor.red = color.R;
            chargeColor.green = color.G;
            chargeColor.blue = color.B;
            OnPropertyChanged(nameof(ChargingColor));
        }

        public void UpdateLaunchProgram(string path)
        {
            profileSettings.LaunchProgram[device] = path;
            OnPropertyChanged(nameof(LaunchProgramExists));
            OnPropertyChanged(nameof(LaunchProgram));
            OnPropertyChanged(nameof(LaunchProgramName));
            OnPropertyChanged(nameof(LaunchProgramIcon));
        }

        public void ResetLauchProgram()
        {
            profileSettings.LaunchProgram[device] = string.Empty;
            OnPropertyChanged(nameof(LaunchProgramExists));
            OnPropertyChanged(nameof(LaunchProgram));
            OnPropertyChanged(nameof(LaunchProgramName));
            OnPropertyChanged(nameof(LaunchProgramIcon));
        }

        private int CalculateOutputMouseSpeed(int mouseSpeed)
        {
            int result = mouseSpeed * Mapping.MOUSESPEEDFACTOR;
            return result;
        }

        public void LaunchCurveEditor(string customDefinition)
        {
            // Custom curve editor web link clicked. Open the bezier curve editor web app usign the default browser app and pass on current custom definition as a query string parameter.
            // The Process.Start command using HTML page doesn't support query parameters, so if there is a custom curve definition then lookup the default browser executable name from a sysreg.
            string defaultBrowserCmd = String.Empty;
            try
            {
                if (!String.IsNullOrEmpty(customDefinition))
                {
                    string progId = String.Empty;
                    using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\http\\UserChoice"))
                    {
                        progId = userChoiceKey?.GetValue("Progid")?.ToString();
                    }

                    if (!String.IsNullOrEmpty(progId))
                    {
                        using (RegistryKey browserPathCmdKey = Registry.ClassesRoot.OpenSubKey($"{progId}\\shell\\open\\command"))
                        {
                            defaultBrowserCmd = browserPathCmdKey?.GetValue(null).ToString().ToLower();
                        }

                        if (!String.IsNullOrEmpty(defaultBrowserCmd))
                        {
                            int iStartPos = (defaultBrowserCmd[0] == '"' ? 1 : 0);
                            defaultBrowserCmd = defaultBrowserCmd.Substring(iStartPos, defaultBrowserCmd.LastIndexOf(".exe") + 4 - iStartPos);
                            if (Path.GetFileName(defaultBrowserCmd) == "launchwinapp.exe")
                                defaultBrowserCmd = String.Empty;
                        }

                        // Fallback to IE executable if the default browser HTML shell association is for some reason missing or is not set
                        if (String.IsNullOrEmpty(defaultBrowserCmd))
                            defaultBrowserCmd = "C:\\Program Files\\Internet Explorer\\iexplore.exe";

                        if (!File.Exists(defaultBrowserCmd))
                            defaultBrowserCmd = String.Empty;
                    }
                }

                // Launch custom bezier editor webapp using a default browser executable command or via a default shell command. The default shell exeution doesn't support query parameters.
                if (!String.IsNullOrEmpty(defaultBrowserCmd))
                    System.Diagnostics.Process.Start(defaultBrowserCmd, $"\"file:///{pathService.ProgramDirectory}\\BezierCurveEditor\\index.html?curve={customDefinition.Replace(" ", "")}\"");
                else
                {
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo($"{pathService.ProgramDirectory}\\BezierCurveEditor\\index.html");
                    startInfo.UseShellExecute = true;
                    using (System.Diagnostics.Process temp = System.Diagnostics.Process.Start(startInfo))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Ui_BezierEditorOpenFailed", pathService.ProgramDirectory, ex.Message);
            }
        }

        // TODO(技術的負債・Phase5-Step14 タスク(c)-1関連): 本メソッドはフィールドを直接書き換え、
        // PropertyChanged を出さない。本メソッドが更新する各フィールド（tempControllerIndex等）がUIへ正しく反映される
        // のは、呼び出し元（ProfileEditor.xaml.cs）が必ずDataContextをnullにしてから再代入する
        // （Reload()、およびStopEditorBindings()+RefreshEditorBindings()のペア）という規約を
        // 守っているためである。将来、本メソッドを新たな呼び出し元から呼ぶ場合、このDataContext
        // 再設定の規約を守らないと、値は正しく更新されてもUI上には反映されない不具合が再発する。
        // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Status.md タスク(c)-1
        public void UpdateLateProperties()
        {
            tempControllerIndex = ControllerTypeIndex;
            // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク2）: 読み取り側の参照先を
            // profileSettings.OutContType へ修正。書き込み先の OutDevTypeTemp（Temp出力デバイス
            // 種別の一時保持）は outputSlotService 側のまま変更しない。
            outputSlotService.OutDevTypeTemp[device] = profileSettings.OutContType[device];
            tempBtPollRate = profileSettings.BTPollRate[device];
            outputMouseSpeed = CalculateOutputMouseSpeed(ButtonMouseSensitivity);
            mouseOffsetSpeed = RawButtonMouseOffset * outputMouseSpeed;
            Gyro.RefreshSmoothMethodIndexes();
        }
    }
}