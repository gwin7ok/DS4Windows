using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
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
    public partial class GyroSettingsViewModel : ObservableObject
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
            ArgumentNullException.ThrowIfNull(gyro);
            this.gyro = gyro;
            this.loadTarget = loadTarget;
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
            RefreshSmoothMethodIndexes();
        }

        /// <summary>プロファイルの読み込みのあとに、スムージング方式の選択を設定へ合わせ直す。</summary>
        public void RefreshSmoothMethodIndexes()
        {
            gyroMouseSmoothMethodIndex = FindGyroMouseSmoothMethodIndex();
        }

        public bool UsingMinViGEm173333 => environmentService.IsUsingMinViGEm117333();

        public int GyroOutModeIndex
        {
            get
            {
                switch (gyro.OutputMode)
                {
                    case GyroOutMode.Mouse: return 1;
                    case GyroOutMode.Passthru: return 2;
                    default: return 0;
                }
            }
            set
            {
                GyroOutMode temp = GyroOutMode.Controls;
                switch (value)
                {
                    case 1: temp = GyroOutMode.Mouse; break;
                    case 2: temp = GyroOutMode.Passthru; break;
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

        public event PropertyChangingHandler<GyroOutMode>? GyroOutModeIndexChanging;

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
                        info.smoothingMethod = GyroSmoothingMethod.OneEuro;
                        break;
                    case 1:
                        info.ResetSmoothingMethods();
                        info.smoothingMethod = GyroSmoothingMethod.WeightedAverage;
                        break;
                    default:
                        break;
                }

                gyroMouseSmoothMethodIndex = value;
                OnPropertyChanged(nameof(GyroMouseSmoothMethodIndex));
                // 方式で入力欄の表示が切り替わる
                OnPropertyChanged(nameof(GyroMouseWeightAvgPanelShown));
                OnPropertyChanged(nameof(GyroMouseOneEuroPanelShown));
            }
        }

        public bool GyroMouseWeightAvgPanelShown =>
            gyro.Mouse.smoothingMethod == GyroSmoothingMethod.WeightedAverage;

        public bool GyroMouseOneEuroPanelShown =>
            gyro.Mouse.smoothingMethod == GyroSmoothingMethod.OneEuro
            || gyro.Mouse.smoothingMethod == GyroSmoothingMethod.None;

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

        /// <summary>トリガーのメニューに並べる見出し（最後の "Always On" は保存する番号 -1 に当たる）。View がこの並びでメニューを作る。</summary>
        public IReadOnlyList<string> TriggerItemNames => triggerItems;

        public void UpdateGyroMouseTrig(IReadOnlyList<TriggerMenuEntry> entries, bool alwaysOnChecked)
        {
            gyro.MouseTriggers = ReadMenu(entries, alwaysOnChecked, out string display);
            GyroMouseTrigDisplay = display;
        }

        public void PopulateGyroMouseTrig(IReadOnlyList<TriggerMenuEntry> entries)
        {
            GyroMouseTrigDisplay = ApplyToMenu(entries, gyro.MouseTriggers);
        }

        public void UpdateGyroControlsTrig(IReadOnlyList<TriggerMenuEntry> entries, bool alwaysOnChecked)
        {
            gyro.Controls.triggers = ReadMenu(entries, alwaysOnChecked, out string display);
            GyroControlsTrigDisplay = display;
        }

        public void PopulateGyroControlsTrig(IReadOnlyList<TriggerMenuEntry> entries)
        {
            GyroControlsTrigDisplay = ApplyToMenu(entries, gyro.Controls.triggers);
        }
    }
}
