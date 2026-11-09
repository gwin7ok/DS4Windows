using System;
using System.Collections.Generic;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「L2 &amp; R2」（トリガー）の ViewModel。1 つのスロットのトリガーの設定（<see cref="TriggerSlotSettings"/>）だけを扱う。
    /// プロパティ名は、画面の Binding と同じ名前のまま（画面の部品は、この ViewModel を DataContext にして、そのまま使える）。
    /// 値は設定サービスの配列にあり、保存・読込の経路は変わらない。
    /// </summary>
    public class TriggerSettingsViewModel : ObservableObject
    {
        private readonly TriggerSlotSettings triggers;

        public TriggerSettingsViewModel(TriggerSlotSettings triggers)
        {
            this.triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
        }

        // Phase7b（既存の不具合。上流の元から）: 旧は deadZone / 255.0（丸めなし）と (byte)(value * 255.0)（切り捨て）で、
        // 上下ボタンで 0.10 ずつ増減すると誤差が溜まり、0.20 → 0.29 のように 0.09 しか変わらない所があった。スティック（LSDeadZone）と同じく、読み出しは小数 2 桁へ丸め、設定は四捨五入する。
        public double L2DeadZone
        {
            get => Math.Round(triggers.L2Mod.deadZone / 255.0, 2);
            set
            {
                double temp = Math.Round(triggers.L2Mod.deadZone / 255.0, 2);
                if (temp == value) return;
                triggers.L2Mod.deadZone = (byte)Math.Min(Math.Max(Math.Round(value * 255.0), 0.0), 255.0);
                OnPropertyChanged(nameof(L2DeadZone));
            }
        }
        public double R2DeadZone
        {
            get => Math.Round(triggers.R2Mod.deadZone / 255.0, 2);
            set
            {
                double temp = Math.Round(triggers.R2Mod.deadZone / 255.0, 2);
                if (temp == value) return;
                triggers.R2Mod.deadZone = (byte)Math.Min(Math.Max(Math.Round(value * 255.0), 0.0), 255.0);
                OnPropertyChanged(nameof(R2DeadZone));
            }
        }
        public double L2MaxZone
        {
            get => triggers.L2Mod.MaxZone / 100.0;
            set => triggers.L2Mod.MaxZone = (int)Math.Round(value * 100.0);
        }

        public double R2MaxZone
        {
            get => triggers.R2Mod.MaxZone / 100.0;
            set => triggers.R2Mod.MaxZone = (int)Math.Round(value * 100.0);
        }

        public double L2AntiDeadZone
        {
            get => triggers.L2Mod.antiDeadZone / 100.0;
            set => triggers.L2Mod.antiDeadZone = (int)Math.Round(value * 100.0);
        }

        public double R2AntiDeadZone
        {
            get => triggers.R2Mod.antiDeadZone / 100.0;
            set => triggers.R2Mod.antiDeadZone = (int)Math.Round(value * 100.0);
        }

        public double L2MaxOutput
        {
            get => triggers.L2Mod.MaxOutput / 100.0;
            set => triggers.L2Mod.MaxOutput = value * 100.0;
        }

        public double R2MaxOutput
        {
            get => triggers.R2Mod.MaxOutput / 100.0;
            set => triggers.R2Mod.MaxOutput = value * 100.0;
        }

        public double L2Sens
        {
            get => triggers.L2Sens;
            set => triggers.L2Sens = value;
        }

        public double R2Sens
        {
            get => triggers.R2Sens;
            set => triggers.R2Sens = value;
        }

        public int L2OutputCurveIndex
        {
            get => triggers.L2CurveMode;
            set
            {
                triggers.L2CurveMode = value;
                OnPropertyChanged(nameof(L2CustomCurveSelected));
            }
        }

        public int R2OutputCurveIndex
        {
            get => triggers.R2CurveMode;
            set
            {
                triggers.R2CurveMode = value;
                OnPropertyChanged(nameof(R2CustomCurveSelected));
            }
        }

        public bool L2CustomCurveSelected
        {
            get => triggers.L2CurveMode == 6;
        }
        public bool R2CustomCurveSelected
        {
            get => triggers.R2CurveMode == 6;
        }
        public string L2CustomCurve
        {
            get => triggers.L2Bezier.CustomDefinition;
            set => triggers.L2Bezier.InitBezierCurve(value, BezierCurve.AxisType.L2R2, true);
        }

        public string R2CustomCurve
        {
            get => triggers.R2Bezier.CustomDefinition;
            set => triggers.R2Bezier.InitBezierCurve(value, BezierCurve.AxisType.L2R2, true);
        }

        private List<TriggerModeChoice> triggerModeChoices = new List<TriggerModeChoice>()
        {
            new TriggerModeChoice("Normal", TriggerMode.Normal),
        };

        private List<TwoStageChoice> twoStageModeChoices = new List<TwoStageChoice>()
        {
            new TwoStageChoice("Disabled", TwoStageTriggerMode.Disabled),
            new TwoStageChoice("Normal", TwoStageTriggerMode.Normal),
            new TwoStageChoice("Exclusive", TwoStageTriggerMode.ExclusiveButtons),
            new TwoStageChoice("Hair Trigger", TwoStageTriggerMode.HairTrigger),
            new TwoStageChoice("Hip Fire", TwoStageTriggerMode.HipFire),
            new TwoStageChoice("Hip Fire Exclusive", TwoStageTriggerMode.HipFireExclusiveButtons),
        };
        public List<TwoStageChoice> TwoStageModeChoices { get => twoStageModeChoices; }

        public TwoStageTriggerMode L2TriggerMode
        {
            get => triggers.L2Output.twoStageMode;
            set
            {
                TwoStageTriggerMode temp = triggers.L2Output.TwoStageMode;
                if (temp == value) return;

                triggers.L2Output.TwoStageMode = value;
                OnPropertyChanged(nameof(L2TriggerMode));
            }
        }
        public TwoStageTriggerMode R2TriggerMode
        {
            get => triggers.R2Output.TwoStageMode;
            set
            {
                TwoStageTriggerMode temp = triggers.R2Output.TwoStageMode;
                if (temp == value) return;

                triggers.R2Output.twoStageMode = value;
                OnPropertyChanged(nameof(R2TriggerMode));
            }
        }
        public int L2HipFireTime
        {
            get => triggers.L2Output.hipFireMS;
            set => triggers.L2Output.hipFireMS = value;
        }

        public int R2HipFireTime
        {
            get => triggers.R2Output.hipFireMS;
            set => triggers.R2Output.hipFireMS = value;
        }

        private List<TriggerEffectChoice> triggerEffectChoices = new List<TriggerEffectChoice>()
        {
            new TriggerEffectChoice("None", DS4Windows.Core.Model.TriggerEffects.None),
            new TriggerEffectChoice("Full Click", DS4Windows.Core.Model.TriggerEffects.FullClick),
            new TriggerEffectChoice("Rigid", DS4Windows.Core.Model.TriggerEffects.Rigid),
            new TriggerEffectChoice("Pulse", DS4Windows.Core.Model.TriggerEffects.Pulse),
        };
        public List<TriggerEffectChoice> TriggerEffectChoices { get => triggerEffectChoices; }

        public DS4Windows.Core.Model.TriggerEffects L2TriggerEffect
        {
            get => triggers.L2Output.triggerEffect;
            set
            {
                DS4Windows.Core.Model.TriggerEffects temp = triggers.L2Output.TriggerEffect;
                if (temp == value) return;

                triggers.L2Output.TriggerEffect = value;
            }
        }

        public DS4Windows.Core.Model.TriggerEffects R2TriggerEffect
        {
            get => triggers.R2Output.triggerEffect;
            set
            {
                DS4Windows.Core.Model.TriggerEffects temp = triggers.R2Output.TriggerEffect;
                if (temp == value) return;

                triggers.R2Output.TriggerEffect = value;
            }
        }
    }
}
