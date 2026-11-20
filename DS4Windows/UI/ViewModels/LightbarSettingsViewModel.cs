using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「ライトバー」タブの ViewModel。1 つのスロットのライトバーの設定（<see cref="LightbarSlotSettings"/>）だけを扱う。
    /// プロパティ名は、画面の Binding と同じ名前のまま（画面の部品は、この ViewModel を DataContext にして、そのまま使える）。
    /// 色の選択中に実機のライトバーへ一時的に色を出す（強制表示）のは、設定の編集スロットではなく、実機（targetDevice）に対して行う。
    /// </summary>
    public class LightbarSettingsViewModel : ObservableObject
    {
        private readonly LightbarSlotSettings lightbar;
        private readonly LightbarOverrides overrides;
        private readonly IEnvironmentService environmentService;
        private readonly int targetDevice;

        public LightbarSettingsViewModel(LightbarSlotSettings lightbar, LightbarOverrides overrides,
            IEnvironmentService environmentService, int targetDevice)
        {
            ArgumentNullException.ThrowIfNull(lightbar);
            this.lightbar = lightbar;
            ArgumentNullException.ThrowIfNull(overrides);
            this.overrides = overrides;
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
            this.targetDevice = targetDevice;

            // 自分の値が変わったときの連動（派生する表示用プロパティの再通知）。値を設定した直後に同期して実行する
            PropertyChanged += OnOwnPropertyChanged;
        }

        /// <summary>古い ViGEm では、一部の機能が使えない旨の注意を出すための判定。</summary>
        public bool UsingMinViGEm173333 => environmentService.IsUsingMinViGEm117333();

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
                    break;
                case nameof(MainColorG):
                    OnPropertyChanged(nameof(MainColorGString));
                    OnPropertyChanged(nameof(MainColorString));
                    break;
                case nameof(MainColorB):
                    OnPropertyChanged(nameof(MainColorBString));
                    OnPropertyChanged(nameof(MainColorString));
                    break;
                case nameof(Rainbow):
                    break;
            }
        }

        private void ProfileSettingsViewModel_MainColorChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(MainColorString));
            OnPropertyChanged(nameof(MainColorR));
            OnPropertyChanged(nameof(MainColorG));
            OnPropertyChanged(nameof(MainColorB));
        }

        /// <summary>
        /// 実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。
        /// ライトバーのプレビューは、この判定で、実機があるときだけ行う（編集スロットではない）。
        /// </summary>
        private bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;

        public int LightbarModeIndex
        {
            get
            {
                int index = 0;
                switch (lightbar.Info.Mode)
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

                lightbar.Info.Mode = temp;
                OnPropertyChanged(nameof(LightbarModeIndex));
            }
        }
        public DS4Color MainColor => lightbar.Info.ds4winSettings.m_Led;
        public string MainColorString
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_Led; //ref Global.MainColor[device];
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
            get => lightbar.Info.ds4winSettings.m_Led.red;
            set
            {
                lightbar.Info.ds4winSettings.m_Led.red = (byte)value;
                OnPropertyChanged(nameof(MainColorR));
            }
        }
        public string MainColorRString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_Led.red.ToString("X2")}FF0000";
        }
        public int MainColorG
        {
            get => lightbar.Info.ds4winSettings.m_Led.green;
            set
            {
                lightbar.Info.ds4winSettings.m_Led.green = (byte)value;
                OnPropertyChanged(nameof(MainColorG));
            }
        }
        public string MainColorGString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_Led.green.ToString("X2")}00FF00";
        }
        public int MainColorB
        {
            get => lightbar.Info.ds4winSettings.m_Led.blue;
            set
            {
                lightbar.Info.ds4winSettings.m_Led.blue = (byte)value;
                OnPropertyChanged(nameof(MainColorB));
            }
        }
        public string MainColorBString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_Led.blue.ToString("X2")}0000FF";
        }
        public string LowColor
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_LowLed; //ref Global.LowColor[device];
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public int LowColorR
        {
            get => lightbar.Info.ds4winSettings.m_LowLed.red;
            set
            {
                lightbar.Info.ds4winSettings.m_LowLed.red = (byte)value;
                OnPropertyChanged(nameof(LowColorR));
                OnPropertyChanged(nameof(LowColorRString));
            }
        }
        public string LowColorRString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_LowLed.red.ToString("X2")}FF0000";
        }
        public int LowColorG
        {
            get => lightbar.Info.ds4winSettings.m_LowLed.green;
            set
            {
                lightbar.Info.ds4winSettings.m_LowLed.green = (byte)value;
                OnPropertyChanged(nameof(LowColorG));
                OnPropertyChanged(nameof(LowColorGString));
            }
        }
        public string LowColorGString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_LowLed.green.ToString("X2")}00FF00";
        }
        public int LowColorB
        {
            get => lightbar.Info.ds4winSettings.m_LowLed.blue;
            set
            {
                lightbar.Info.ds4winSettings.m_LowLed.blue = (byte)value;
                OnPropertyChanged(nameof(LowColorB));
                OnPropertyChanged(nameof(LowColorBString));
            }
        }
        public string LowColorBString
        {
            get => $"#{lightbar.Info.ds4winSettings.m_LowLed.blue.ToString("X2")}0000FF";
        }
        public DS4Color LowColorValue
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_LowLed;
                return color;
            }
        }

        public int FlashTypeIndex
        {
            get => lightbar.Info.ds4winSettings.flashType; //Global.FlashType[device];
            set => lightbar.Info.ds4winSettings.flashType = (byte)value;
        }

        public int FlashAt
        {
            get => lightbar.Info.ds4winSettings.flashAt; //Global.FlashAt[device];
            set => lightbar.Info.ds4winSettings.flashAt = value;
        }

        public string FlashColor
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_FlashLed;
                if (color.red == 0 && color.green == 0 && color.blue == 0)
                {
                    color = ref lightbar.Info.ds4winSettings.m_Led;
                }

                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public DS4Color FlashColorValue
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_FlashLed;
                if (color.red == 0 && color.green == 0 && color.blue == 0)
                {
                    color = ref lightbar.Info.ds4winSettings.m_Led;
                }

                return color;
            }
        }

        public int ChargingType
        {
            get => lightbar.Info.ds4winSettings.chargingType;
            set
            {
                lightbar.Info.ds4winSettings.chargingType = value;
                OnPropertyChanged(nameof(ChargingColorShown));
            }
        }

        public bool ColorBatteryPercent
        {
            get => lightbar.Info.ds4winSettings.ledAsBattery;
            set
            {
                lightbar.Info.ds4winSettings.ledAsBattery = value;
            }
        }

        public string ChargingColor
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_ChargingLed;
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }
        public DS4Color ChargingColorValue
        {
            get
            {
                ref DS4Color color = ref lightbar.Info.ds4winSettings.m_ChargingLed;
                return color;
            }
        }

        public bool ChargingColorShown
        {
            get => lightbar.Info.ds4winSettings.chargingType == 3;
        }
        public double Rainbow
        {
            get => lightbar.Info.ds4winSettings.rainbow;
            set
            {
                lightbar.Info.ds4winSettings.rainbow = value;
                OnPropertyChanged(nameof(Rainbow));
                OnPropertyChanged(nameof(RainbowExists));
            }
        }
        public bool RainbowExists
        {
            get => lightbar.Info.ds4winSettings.rainbow != 0.0;
        }

        public double MaxSatRainbow
        {
            get => lightbar.Info.ds4winSettings.maxRainbowSat * 100.0;
            set => lightbar.Info.ds4winSettings.maxRainbowSat = value / 100.0;
        }

        public void UpdateFlashColor(DS4Color color)
        {
            lightbar.Info.ds4winSettings.m_FlashLed = color;
            OnPropertyChanged(nameof(FlashColor));
        }

        public void UpdateMainColor(DS4Color color)
        {
            lightbar.Info.ds4winSettings.m_Led = color;
            OnPropertyChanged(nameof(MainColor));
        }

        public void UpdateLowColor(DS4Color color)
        {
            lightbar.Info.ds4winSettings.m_LowLed = color;

            OnPropertyChanged(nameof(LowColor));
            OnPropertyChanged(nameof(LowColorR));
            OnPropertyChanged(nameof(LowColorG));
            OnPropertyChanged(nameof(LowColorB));
            OnPropertyChanged(nameof(LowColorRString));
            OnPropertyChanged(nameof(LowColorGString));
            OnPropertyChanged(nameof(LowColorBString));
        }

        // ライトバー強制色のプレビューは、編集スロット（Device）ではなく実機（targetDevice）に対して行う
        public void UpdateForcedColor(DS4Color color)
        {
            if (HasTargetDevice)
            {
                overrides.forcedColor[targetDevice] = color;
                overrides.forcedFlash[targetDevice] = 0;
                overrides.forcelight[targetDevice] = true;
            }
        }

        public void StartForcedColor(DS4Color color)
        {
            if (HasTargetDevice)
            {
                overrides.forcedColor[targetDevice] = color;
                overrides.forcedFlash[targetDevice] = 0;
                overrides.forcelight[targetDevice] = true;
            }
        }

        public void EndForcedColor()
        {
            if (HasTargetDevice)
            {
                overrides.forcedColor[targetDevice] = new DS4Color(0, 0, 0);
                overrides.forcedFlash[targetDevice] = 0;
                overrides.forcelight[targetDevice] = false;
            }
        }

        public void UpdateChargingColor(DS4Color color)
        {
            lightbar.Info.ds4winSettings.m_ChargingLed = color;
            OnPropertyChanged(nameof(ChargingColor));
        }
    }
}
