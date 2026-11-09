using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「ジャイロ」タブの ViewModel。1 つのスロットのジャイロの設定（<see cref="GyroSlotSettings"/>）だけを扱う。
    /// プロパティ名は、タブの Binding と同じ名前のまま（画面の部品は、この ViewModel を DataContext にして、そのまま使える）。
    /// 値は設定サービスの配列にあり、保存・読込の経路は変わらない。
    /// </summary>
    public class GyroSettingsViewModel : ObservableObject
    {
        private const string AlwaysOn = "Always On";

        private readonly GyroSlotSettings gyro;
        private readonly IProfileLoadTarget loadTarget;
        private readonly IEnvironmentService environmentService;

        // トリガーの項目（メニューの並びがそのまま保存する番号になる。最後の "Always On" は番号 -1 に当たる）
        private readonly List<string> triggerItems = new List<string>()
        {
            "Cross", "Circle", "Square", "Triangle",
            "L1", "L2", "R1", "R2",
            "Up", "Down", "Left", "Right",
            "L3", "R3", "Finger on Touchpad", "2 Fingers on Touchpad",
            "Options", "Share", "PS", "Touchpad Click",
            "Mute", "Side L", "Side R",
            // DualSense Edge の追加ボタン
            "Function Left", "Function Right", "Bottom Left Paddle", "Bottom Right Paddle",
            AlwaysOn,
        };

        public GyroSettingsViewModel(GyroSlotSettings gyro, IProfileLoadTarget loadTarget, IEnvironmentService environmentService)
        {
            this.gyro = gyro ?? throw new ArgumentNullException(nameof(gyro));
            this.loadTarget = loadTarget;
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            RefreshSmoothMethodIndexes();
        }

        /// <summary>プロファイルの読み込みのあとに、スムージング方式の選択を設定へ合わせ直す。</summary>
        public void RefreshSmoothMethodIndexes()
        {
            gyroMouseSmoothMethodIndex = FindGyroMouseSmoothMethodIndex();
            gyroMouseStickSmoothMethodIndex = FindGyroMouseStickSmoothMethodIndex();
        }

        public bool UsingMinViGEm173333 => environmentService.IsUsingMinViGEm117333();

        // DS3 の傾きの擬似。プロファイル全体の設定だが、ジャイロのタブにある
        public bool HasUseDs3PitchRollSimChanged { get; private set; }

        public bool UseDs3PitchRollSim
        {
            get => gyro.UseDs3PitchRollSim;
            set
            {
                // 再起動が要るのは、実行中の値と違うとき。元に戻したら、求めない
                HasUseDs3PitchRollSimChanged = gyro.UseDs3PitchRollSim != value;
                gyro.UseDs3PitchRollSim = value;
            }
        }

        /// <summary>サービスの再起動を求めたあとに、<see cref="HasUseDs3PitchRollSimChanged"/> の印を戻す（同じ変更で再起動をくり返し求めない）。</summary>
        public void AcknowledgeUseDs3PitchRollSimChange()
        {
            HasUseDs3PitchRollSimChanged = false;
        }

        public int GyroOutModeIndex
        {
            get
            {
                switch (gyro.OutputMode)
                {
                    case GyroOutMode.Mouse: return 1;
                    case GyroOutMode.MouseJoystick: return 2;
                    case GyroOutMode.Passthru: return 3;
                    default: return 0;
                }
            }
            set
            {
                GyroOutMode temp = GyroOutMode.Controls;
                switch (value)
                {
                    case 1: temp = GyroOutMode.Mouse; break;
                    case 2: temp = GyroOutMode.MouseJoystick; break;
                    case 3: temp = GyroOutMode.Passthru; break;
                    default: break;
                }

                GyroOutMode current = gyro.OutputMode;
                if (temp == current) return;

                // モードを選び直したら、保持していた廃止されたモード（DirectionalSwipe）は破棄する（保存で書き戻さない）
                gyro.RemovedSwipe?.ForgetDirectionalSwipeMode();
                GyroOutModeIndexChanging?.Invoke(this, current, temp);
                gyro.OutputMode = temp;
                OnPropertyChanged(nameof(GyroOutModeIndex));
            }
        }

        public event PropertyChangingHandler<GyroOutMode> GyroOutModeIndexChanging;

        public bool GyroMouseTurns
        {
            get => gyro.TriggerTurns;
            set => gyro.TriggerTurns = value;
        }

        public int GyroSensitivity
        {
            get => gyro.Sensitivity;
            set => gyro.Sensitivity = value;
        }

        public int GyroVertScale
        {
            get => gyro.SensVerticalScale;
            set => gyro.SensVerticalScale = value;
        }

        public int GyroMouseEvalCondIndex
        {
            get => gyro.MouseTriggerCondAnd ? 0 : 1;
            set => gyro.MouseTriggerCondAnd = value == 0;
        }

        public int GyroMouseXAxis
        {
            get => gyro.MouseHorizontalAxis;
            set => gyro.MouseHorizontalAxis = value;
        }

        public double GyroMouseMinThreshold
        {
            get => gyro.Mouse.minThreshold;
            set
            {
                if (gyro.Mouse.minThreshold == value) return;
                gyro.Mouse.minThreshold = value;
            }
        }

        public bool GyroMouseInvertX
        {
            get => (gyro.Invert & 2) == 2;
            set => gyro.Invert = value ? gyro.Invert | 2 : gyro.Invert & ~2;
        }

        public bool GyroMouseInvertY
        {
            get => (gyro.Invert & 1) == 1;
            set => gyro.Invert = value ? gyro.Invert | 1 : gyro.Invert & ~1;
        }

        public bool GyroMouseSmooth
        {
            get => gyro.Mouse.enableSmoothing;
            set
            {
                if (gyro.Mouse.enableSmoothing == value) return;
                gyro.Mouse.enableSmoothing = value;
                OnPropertyChanged(nameof(GyroMouseSmooth));
            }
        }

        private int gyroMouseSmoothMethodIndex;
        public int GyroMouseSmoothMethodIndex
        {
            get => gyroMouseSmoothMethodIndex;
            set
            {
                if (gyroMouseSmoothMethodIndex == value) return;

                GyroMouseInfo info = gyro.Mouse;
                switch (value)
                {
                    case 0:
                        info.ResetSmoothingMethods();
                        info.smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;
                        break;
                    case 1:
                        info.ResetSmoothingMethods();
                        info.smoothingMethod = GyroMouseInfo.SmoothingMethod.WeightedAverage;
                        break;
                    default:
                        break;
                }

                gyroMouseSmoothMethodIndex = value;
                OnPropertyChanged(nameof(GyroMouseSmoothMethodIndex));
                // 方式で入力欄の表示が切り替わる
                OnPropertyChanged(nameof(GyroMouseWeightAvgPanelVisibility));
                OnPropertyChanged(nameof(GyroMouseOneEuroPanelVisibility));
            }
        }

        public Visibility GyroMouseWeightAvgPanelVisibility =>
            gyro.Mouse.smoothingMethod == GyroMouseInfo.SmoothingMethod.WeightedAverage ? Visibility.Visible : Visibility.Collapsed;

        public Visibility GyroMouseOneEuroPanelVisibility =>
            gyro.Mouse.smoothingMethod == GyroMouseInfo.SmoothingMethod.OneEuro
            || gyro.Mouse.smoothingMethod == GyroMouseInfo.SmoothingMethod.None ? Visibility.Visible : Visibility.Collapsed;

        public double GyroMouseSmoothWeight
        {
            get => gyro.Mouse.smoothingWeight;
            set => gyro.Mouse.smoothingWeight = value;
        }

        public double GyroMouseOneEuroMinCutoff
        {
            get => gyro.Mouse.MinCutoff;
            set => gyro.Mouse.MinCutoff = value;
        }

        public double GyroMouseOneEuroBeta
        {
            get => gyro.Mouse.Beta;
            set => gyro.Mouse.Beta = value;
        }

        private int gyroMouseStickSmoothMethodIndex;
        public int GyroMouseStickSmoothMethodIndex
        {
            get => gyroMouseStickSmoothMethodIndex;
            set
            {
                if (gyroMouseStickSmoothMethodIndex == value) return;

                GyroMouseStickInfo info = gyro.MouseStick;
                switch (value)
                {
                    case 0:
                        info.ResetSmoothingMethods();
                        info.smoothingMethod = GyroMouseStickInfo.SmoothingMethod.OneEuro;
                        break;
                    case 1:
                        info.ResetSmoothingMethods();
                        info.smoothingMethod = GyroMouseStickInfo.SmoothingMethod.WeightedAverage;
                        break;
                    default:
                        break;
                }

                gyroMouseStickSmoothMethodIndex = value;
                OnPropertyChanged(nameof(GyroMouseStickSmoothMethodIndex));
                OnPropertyChanged(nameof(GyroMouseStickWeightAvgPanelVisibility));
                OnPropertyChanged(nameof(GyroMouseStickOneEuroPanelVisibility));
            }
        }

        public Visibility GyroMouseStickWeightAvgPanelVisibility =>
            gyro.MouseStick.smoothingMethod == GyroMouseStickInfo.SmoothingMethod.WeightedAverage ? Visibility.Visible : Visibility.Collapsed;

        public Visibility GyroMouseStickOneEuroPanelVisibility =>
            gyro.MouseStick.smoothingMethod == GyroMouseStickInfo.SmoothingMethod.OneEuro
            || gyro.MouseStick.smoothingMethod == GyroMouseStickInfo.SmoothingMethod.None ? Visibility.Visible : Visibility.Collapsed;

        public double GyroMouseStickSmoothWeight
        {
            get => gyro.MouseStick.smoothWeight;
            set => gyro.MouseStick.smoothWeight = value;
        }

        public double GyroMouseStickOneEuroMinCutoff
        {
            get => gyro.MouseStick.MinCutoff;
            set => gyro.MouseStick.MinCutoff = value;
        }

        public double GyroMouseStickOneEuroBeta
        {
            get => gyro.MouseStick.Beta;
            set => gyro.MouseStick.Beta = value;
        }

        public int GyroMouseDeadZone
        {
            get => gyro.MouseDeadZone;
            set => gyro.SetMouseDeadZone(value, loadTarget);
        }

        public bool GyroMouseToggle
        {
            get => gyro.MouseToggle;
            set => gyro.SetMouseToggle(value, loadTarget);
        }

        public bool GyroMouseJitterCompensation
        {
            get => gyro.Mouse.jitterCompensation;
            set => gyro.Mouse.jitterCompensation = value;
        }

        public bool GyroMouseStickTurns
        {
            get => gyro.MouseStickTriggerTurns;
            set => gyro.MouseStickTriggerTurns = value;
        }

        public bool GyroMouseStickToggle
        {
            get => gyro.MouseStickToggle;
            set => gyro.SetMouseStickToggle(value, loadTarget);
        }

        public int GyroMouseStickDeadZone
        {
            get => gyro.MouseStick.deadZone;
            set => gyro.MouseStick.deadZone = value;
        }

        public int GyroMouseStickMaxZone
        {
            get => gyro.MouseStick.maxZone;
            set => gyro.MouseStick.maxZone = value;
        }

        public int GyroMouseStickOutputStick
        {
            get => (int)gyro.MouseStick.outputStick;
            set => gyro.MouseStick.outputStick = (GyroMouseStickInfo.OutputStick)value;
        }

        public int GyroMouseStickOutputAxes
        {
            get => (int)gyro.MouseStick.outputStickDir;
            set => gyro.MouseStick.outputStickDir = (GyroMouseStickInfo.OutputStickAxes)value;
        }

        public double GyroMouseStickAntiDeadX
        {
            get => gyro.MouseStick.antiDeadX * 100.0;
            set => gyro.MouseStick.antiDeadX = value * 0.01;
        }

        public double GyroMouseStickAntiDeadY
        {
            get => gyro.MouseStick.antiDeadY * 100.0;
            set => gyro.MouseStick.antiDeadY = value * 0.01;
        }

        public int GyroMouseStickVertScale
        {
            get => gyro.MouseStick.vertScale;
            set => gyro.MouseStick.vertScale = value;
        }

        public bool GyroMouseStickMaxOutputEnabled
        {
            get => gyro.MouseStick.maxOutputEnabled;
            set
            {
                if (gyro.MouseStick.maxOutputEnabled == value) return;
                gyro.MouseStick.maxOutputEnabled = value;
                OnPropertyChanged(nameof(GyroMouseStickMaxOutput));
            }
        }

        public double GyroMouseStickMaxOutput
        {
            get => gyro.MouseStick.maxOutput;
            set => gyro.MouseStick.maxOutput = value;
        }

        public int GyroMouseStickEvalCondIndex
        {
            get => gyro.MouseStickTriggerCondAnd ? 0 : 1;
            set => gyro.MouseStickTriggerCondAnd = value == 0;
        }

        public int GyroMouseStickXAxis
        {
            get => gyro.MouseStickHorizontalAxis;
            set => gyro.MouseStickHorizontalAxis = value;
        }

        public bool GyroMouseStickInvertX
        {
            get => (gyro.MouseStick.inverted & 1) == 1;
            set => gyro.MouseStick.inverted = value ? gyro.MouseStick.inverted | 1 : (uint)(gyro.MouseStick.inverted & ~1);
        }

        public bool GyroMouseStickInvertY
        {
            get => (gyro.MouseStick.inverted & 2) == 2;
            set => gyro.MouseStick.inverted = value ? gyro.MouseStick.inverted | 2 : (uint)(gyro.MouseStick.inverted & ~2);
        }

        public bool GyroMouseStickJitterCompensation
        {
            get => gyro.MouseStick.jitterCompensation;
            set => gyro.MouseStick.jitterCompensation = value;
        }

        public bool GyroMouseStickSmooth
        {
            get => gyro.MouseStick.useSmoothing;
            set => gyro.MouseStick.useSmoothing = value;
        }

        public double GyroMousetickSmoothWeight
        {
            get => gyro.MouseStick.smoothWeight;
            set => gyro.MouseStick.smoothWeight = value;
        }

        public bool GyroControlsTurns
        {
            get => gyro.Controls.triggerTurns;
            set => gyro.Controls.triggerTurns = value;
        }

        public int GyroControlsEvalCondIndex
        {
            get => gyro.Controls.triggerCond ? 0 : 1;
            set => gyro.Controls.triggerCond = value == 0;
        }

        public bool GyroControlsToggle
        {
            get => gyro.ControlsToggle;
            set => gyro.SetControlsToggle(value, loadTarget);
        }

        // ---- トリガーの選択（メニュー）の表示と保存 ----

        private string gyroControlsTrigDisplay = AlwaysOn;
        public string GyroControlsTrigDisplay
        {
            get => gyroControlsTrigDisplay;
            set
            {
                gyroControlsTrigDisplay = value;
                OnPropertyChanged(nameof(GyroControlsTrigDisplay));
            }
        }

        private string gyroMouseTrigDisplay = AlwaysOn;
        public string GyroMouseTrigDisplay
        {
            get => gyroMouseTrigDisplay;
            set
            {
                gyroMouseTrigDisplay = value;
                OnPropertyChanged(nameof(GyroMouseTrigDisplay));
            }
        }

        private string gyroMouseStickTrigDisplay = AlwaysOn;
        public string GyroMouseStickTrigDisplay
        {
            get => gyroMouseStickTrigDisplay;
            set
            {
                gyroMouseStickTrigDisplay = value;
                OnPropertyChanged(nameof(GyroMouseStickTrigDisplay));
            }
        }

        public void CreateGyroTriggerMenuItems(ContextMenu menu, RoutedEventHandler itemClickHandler)
        {
            foreach (string btnName in triggerItems)
            {
                MenuItem item = new MenuItem()
                {
                    Header = btnName,
                    IsCheckable = true,
                };

                item.Click += itemClickHandler;
                menu.Items.Add(item);
            }
        }

        public void UpdateGyroMouseTrig(ContextMenu menu, bool alwaysOnChecked)
        {
            gyro.MouseTriggers = ReadMenu(menu, alwaysOnChecked, out string display);
            GyroMouseTrigDisplay = display;
        }

        public void PopulateGyroMouseTrig(ContextMenu menu)
        {
            GyroMouseTrigDisplay = ApplyToMenu(menu, gyro.MouseTriggers);
        }

        public void UpdateGyroMouseStickTrig(ContextMenu menu, bool alwaysOnChecked)
        {
            gyro.MouseStickTriggers = ReadMenu(menu, alwaysOnChecked, out string display);
            GyroMouseStickTrigDisplay = display;
        }

        public void PopulateGyroMouseStickTrig(ContextMenu menu)
        {
            GyroMouseStickTrigDisplay = ApplyToMenu(menu, gyro.MouseStickTriggers);
        }

        public void UpdateGyroControlsTrig(ContextMenu menu, bool alwaysOnChecked)
        {
            gyro.Controls.triggers = ReadMenu(menu, alwaysOnChecked, out string display);
            GyroControlsTrigDisplay = display;
        }

        public void PopulateGyroControlsTrig(ContextMenu menu)
        {
            GyroControlsTrigDisplay = ApplyToMenu(menu, gyro.Controls.triggers);
        }

        /// <summary>
        /// メニューのチェックを読み、保存する文字列（カンマ区切りの番号。何も選ばなければ -1＝常にオン）と表示の文字列を返す。
        /// 「Always On」を選んだら、ほかのチェックは外す。
        /// </summary>
        private static string ReadMenu(ContextMenu menu, bool alwaysOnChecked, out string display)
        {
            int index = 0;
            List<int> triggerList = new List<int>();
            List<string> triggerName = new List<string>();

            int itemCount = menu.Items.Count;
            MenuItem alwaysOnItem = menu.Items[itemCount - 1] as MenuItem;
            if (alwaysOnChecked)
            {
                for (int i = 0; i < itemCount - 1; i++)
                {
                    MenuItem item = menu.Items[i] as MenuItem;
                    item.IsChecked = false;
                }
            }
            else
            {
                alwaysOnItem.IsChecked = false;
                foreach (MenuItem item in menu.Items)
                {
                    if (item.IsChecked)
                    {
                        triggerList.Add(index);
                        triggerName.Add(item.Header.ToString());
                    }

                    index++;
                }
            }

            if (triggerList.Count == 0)
            {
                triggerList.Add(-1);
                triggerName.Add(AlwaysOn);
                alwaysOnItem.IsChecked = true;
            }

            display = string.Join(", ", triggerName.ToArray());
            return string.Join(",", triggerList.ToArray());
        }

        /// <summary>保存された番号（カンマ区切り）をメニューのチェックへ反映し、表示の文字列を返す。</summary>
        private static string ApplyToMenu(ContextMenu menu, string savedTriggers)
        {
            string[] triggers = savedTriggers.Split(',');
            int itemCount = menu.Items.Count;
            List<string> triggerName = new List<string>();
            foreach (string trig in triggers)
            {
                bool valid = int.TryParse(trig, out int trigid);
                if (valid && trigid >= 0 && trigid < itemCount - 1)
                {
                    MenuItem current = menu.Items[trigid] as MenuItem;
                    current.IsChecked = true;
                    triggerName.Add(current.Header.ToString());
                }
                else if (valid && trigid == -1)
                {
                    MenuItem current = menu.Items[itemCount - 1] as MenuItem;
                    current.IsChecked = true;
                    triggerName.Add(AlwaysOn);
                    break;
                }
            }

            if (triggerName.Count == 0)
            {
                MenuItem current = menu.Items[itemCount - 1] as MenuItem;
                current.IsChecked = true;
                triggerName.Add(AlwaysOn);
            }

            return string.Join(", ", triggerName.ToArray());
        }

        private int FindGyroMouseSmoothMethodIndex()
        {
            GyroMouseInfo.SmoothingMethod method = gyro.Mouse.smoothingMethod;
            return method == GyroMouseInfo.SmoothingMethod.WeightedAverage ? 1 : 0;
        }

        private int FindGyroMouseStickSmoothMethodIndex()
        {
            return gyro.MouseStick.smoothingMethod == GyroMouseStickInfo.SmoothingMethod.WeightedAverage ? 1 : 0;
        }
    }
}
