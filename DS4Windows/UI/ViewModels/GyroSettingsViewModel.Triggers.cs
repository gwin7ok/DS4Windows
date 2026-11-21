using System;
using System.Collections.Generic;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>ジャイロの有効化トリガーのメニューの読み書き。</summary>
    public partial class GyroSettingsViewModel
    {
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

        /// <summary>
        /// メニューのチェックを読み、保存する文字列（カンマ区切りの番号。何も選ばなければ -1＝常にオン）と表示の文字列を返す。
        /// 「Always On」を選んだら、ほかのチェックは外す（<paramref name="entries"/> のチェックを書き換える）。
        /// </summary>
        private static string ReadMenu(IReadOnlyList<TriggerMenuEntry> entries, bool alwaysOnChecked, out string display)
        {
            int index = 0;
            List<int> triggerList = new List<int>();
            List<string> triggerName = new List<string>();

            int itemCount = entries.Count;
            TriggerMenuEntry alwaysOnItem = entries[itemCount - 1];
            if (alwaysOnChecked)
            {
                for (int i = 0; i < itemCount - 1; i++)
                {
                    entries[i].IsChecked = false;
                }
            }
            else
            {
                alwaysOnItem.IsChecked = false;
                foreach (TriggerMenuEntry item in entries)
                {
                    if (item.IsChecked)
                    {
                        triggerList.Add(index);
                        triggerName.Add(item.Header);
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

        /// <summary>保存された番号（カンマ区切り）をメニューのチェックへ反映し（<paramref name="entries"/> を書き換え）、表示の文字列を返す。</summary>
        private static string ApplyToMenu(IReadOnlyList<TriggerMenuEntry> entries, string? savedTriggers)
        {
            string[] triggers = savedTriggers?.Split(",") ?? Array.Empty<string>();
            int itemCount = entries.Count;
            List<string> triggerName = new List<string>();
            foreach (string trig in triggers)
            {
                bool valid = int.TryParse(trig, out int trigid);
                if (valid && trigid >= 0 && trigid < itemCount - 1)
                {
                    TriggerMenuEntry current = entries[trigid];
                    current.IsChecked = true;
                    triggerName.Add(current.Header);
                }
                else if (valid && trigid == -1)
                {
                    entries[itemCount - 1].IsChecked = true;
                    triggerName.Add(AlwaysOn);
                    break;
                }
            }

            if (triggerName.Count == 0)
            {
                entries[itemCount - 1].IsChecked = true;
                triggerName.Add(AlwaysOn);
            }

            return string.Join(", ", triggerName.ToArray());
        }
        private int FindGyroMouseSmoothMethodIndex()
        {
            GyroSmoothingMethod method = gyro.Mouse.smoothingMethod;
            return method == GyroSmoothingMethod.WeightedAverage ? 1 : 0;
        }
    }
}
