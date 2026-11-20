using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「LS」「RS」（スティック）の ViewModel。1 つのスロットのスティックの設定（<see cref="StickSlotSettings"/>）を、
    /// 左右それぞれの <see cref="StickSideViewModel"/> として持つ。項目の実装は片側の型にだけあり、ここには左右で共通の項目を足さない。
    /// </summary>
    public class StickSettingsViewModel : ObservableObject
    {
        public StickSettingsViewModel(StickSlotSettings sticks)
        {
            if (sticks == null) throw new ArgumentNullException(nameof(sticks));
            Left = new StickSideViewModel(sticks, true);
            Right = new StickSideViewModel(sticks, false);
        }

        /// <summary>左スティックの設定。</summary>
        public StickSideViewModel Left { get; }

        /// <summary>右スティックの設定。</summary>
        public StickSideViewModel Right { get; }
    }
}
