using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Input.Devices;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「その他」タブの「仮想コントローラー」と「振動」の ViewModel。1 つのスロットの出力の設定
    /// （<see cref="OutputSlotSettings"/>）だけを扱う。プロパティ名は、画面の Binding と同じ名前のまま。
    /// 画面で選んだ出力の種類（<see cref="TempControllerIndex"/>）は、保存される値と、出力ピッカーが見る一時の種類
    /// （<see cref="IOutputSlotService.OutDevTypeTemp"/>）の両方へ書く。
    /// </summary>
    public class OutputSettingsViewModel : ObservableObject
    {
        private readonly OutputSlotSettings output;
        private readonly IOutputSlotService outputSlotService;
        private readonly int slot;

        private int tempControllerIndex;
        private bool heavyRumbleActive;
        private bool lightRumbleActive;

        public OutputSettingsViewModel(OutputSlotSettings output, IOutputSlotService outputSlotService, int slot)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
            this.outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            this.slot = slot;
            RefreshFromSettings();
            // 編集の開始時は、一時の種類を Xbox 360 にしておく（画面の選択が変わったときに、実際の種類で更新される）
            this.outputSlotService.OutDevTypeTemp[slot] = OutContType.X360;
        }

        /// <summary>
        /// 保存された設定から、画面の選択（<see cref="TempControllerIndex"/>）を読み直す。
        /// PropertyChanged は出さない。呼び出し側が DataContext を null にして再代入する規約のもとで使う（読み込み直しの流れ）。
        /// </summary>
        public void RefreshFromSettings()
        {
            tempControllerIndex = ControllerTypeIndex;
        }

        /// <summary>読み込み直しのあと、出力ピッカー用の一時の種類を保存された種類へそろえる。</summary>
        public void SyncOutDevTypeTempFromSettings()
        {
            outputSlotService.OutDevTypeTemp[slot] = output.OutContType;
        }

        /// <summary>キャンセル時に、編集中に変えた一時の出力の種類を Xbox 360 へ戻す。</summary>
        public void ResetOutDevTypeTemp()
        {
            outputSlotService.OutDevTypeTemp[slot] = OutContType.X360;
        }

        /// <summary>保存されている出力の種類（Xbox 360／DS4）。マッピング一覧のボタン名の表記を決める。</summary>
        public OutContType ContType => output.OutContType;

        /// <summary>保存される出力の種類（0: Xbox 360、1: DS4）。</summary>
        public int ControllerTypeIndex
        {
            get => output.OutContType == OutContType.DS4 ? 1 : 0;
            set
            {
                OutContType target = value == 1 ? OutContType.DS4 : OutContType.X360;
                if (output.OutContType == target) return;
                output.OutContType = target;
            }
        }

        public int TempControllerIndex
        {
            get => tempControllerIndex;
            set
            {
                if (tempControllerIndex == value) return;
                tempControllerIndex = value;
                OutContType type = value == 1 ? OutContType.DS4 : OutContType.X360;
                output.OutContType = type;
                // 出力ピッカー（BindingWindow）は OutDevTypeTemp から、Pad タブの表示（DS4／Xbox 360）を決める
                outputSlotService.OutDevTypeTemp[slot] = type;
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

        public bool DInputOnly
        {
            get => output.DInputOnly;
            set
            {
                if (output.DInputOnly == value) return;
                output.DInputOnly = value;
                OnPropertyChanged(nameof(DInputOnly));
            }
        }

        public bool VirtualTriggerButtons
        {
            get => output.VirtualTriggerButtons;
            set
            {
                if (output.VirtualTriggerButtons == value) return;
                output.VirtualTriggerButtons = value;
                OnPropertyChanged(nameof(VirtualTriggerButtons));
            }
        }

        private readonly List<EnumChoiceSelection<DS4TriggerOutputMode>> outputDS4TriggerModeOptions =
            new List<EnumChoiceSelection<DS4TriggerOutputMode>>
            {
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.DS4OutputTriggersDefault, DS4TriggerOutputMode.Default),
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.Analog, DS4TriggerOutputMode.Analog),
                new EnumChoiceSelection<DS4TriggerOutputMode>(Translations.Strings.Buttons, DS4TriggerOutputMode.Buttons),
            };

        public List<EnumChoiceSelection<DS4TriggerOutputMode>> OutputDS4TriggerModeOptions => outputDS4TriggerModeOptions;

        public DS4TriggerOutputMode OutputDS4TriggerMode
        {
            get => output.OutputDS4TriggerMode;
            set
            {
                if (output.OutputDS4TriggerMode == value) return;
                output.OutputDS4TriggerMode = value;
                OnPropertyChanged(nameof(OutputDS4TriggerMode));
            }
        }

        public int RumbleBoost
        {
            get => output.RumbleBoost;
            set => output.RumbleBoost = (byte)value;
        }

        public int RumbleAutostopTime
        {
            // 設定（XML）の値はミリ秒、画面は秒
            get => output.RumbleAutostopTimeMs / 1000;
            set => output.RumbleAutostopTimeMs = value * 1000;
        }

        public bool HeavyRumbleActive
        {
            get => heavyRumbleActive;
            set
            {
                heavyRumbleActive = value;
                OnPropertyChanged(nameof(HeavyRumbleActive));
            }
        }

        public bool LightRumbleActive
        {
            get => lightRumbleActive;
            set
            {
                lightRumbleActive = value;
                OnPropertyChanged(nameof(LightRumbleActive));
            }
        }

        public int DualSenseRumbleEmulationPerIndex
        {
            get
            {
                switch (output.DualSenseRumbleEmulationMode)
                {
                    case RumbleEmulationMode.Disabled: return 2;
                    case RumbleEmulationMode.Legacy: return 1;
                    case RumbleEmulationMode.Accurate: return 0;
                    default: return 0;
                }
            }
            set
            {
                RumbleEmulationMode temp;
                switch (value)
                {
                    case 2: temp = RumbleEmulationMode.Disabled; break;
                    case 1: temp = RumbleEmulationMode.Legacy; break;
                    case 0: temp = RumbleEmulationMode.Accurate; break;
                    default: temp = RumbleEmulationMode.Accurate; break;
                }
                output.DualSenseRumbleEmulationMode = temp;
            }
        }

        public int DualSenseHapticPowerLevelPerIndex
        {
            get => output.DualSenseHapticPowerLevel;
            set => output.DualSenseHapticPowerLevel = (byte)value;
        }

        public bool EnableGenericRumbleStrRescaleForDualSenseDevices
        {
            get => output.UseGenericRumbleStrRescaleForDualSenses;
            set => output.UseGenericRumbleStrRescaleForDualSenses = value;
        }

        public bool InverseRumbleMotors
        {
            get => output.InverseRumbleMotors;
            set => output.InverseRumbleMotors = value;
        }
    }
}
