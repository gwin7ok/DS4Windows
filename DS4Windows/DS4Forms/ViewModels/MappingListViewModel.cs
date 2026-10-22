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

using DS4Windows;
using DS4Windows.DI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class MappingListViewModel
    {
        //private int devIndex;
        private ObservableCollection<MappedControl> mappings = new ObservableCollection<MappedControl>();
        public ObservableCollection<MappedControl> Mappings { get => mappings; }

        /// <summary>
        /// Phase9 UI 変更: 一覧の<b>表示用</b>の並び。LS／RS Outer は LS／RS の下、L2／R2 Full Pull は L2／R2 の下に置く。
        /// <see cref="Mappings"/>（添字がコントローラーの入力による選択、ホバーの表などに使われる）の順序は、変えない。同じ <see cref="MappedControl"/> を、別の順で並べるだけ。
        /// </summary>
        private ObservableCollection<MappedControl> displayMappings = new ObservableCollection<MappedControl>();
        public ObservableCollection<MappedControl> DisplayMappings { get => displayMappings; }

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedMappingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;

        /// <summary>選ばれている項目（<see cref="SelectedIndex"/> は <see cref="Mappings"/> の添字）。表示用の一覧の選択と結びつける。</summary>
        public MappedControl SelectedMapping
        {
            get => selectedIndex >= 0 && selectedIndex < mappings.Count ? mappings[selectedIndex] : null;
            set
            {
                // 一覧の選択が外れたとき（null）は、選択を変えない（一覧の更新中に、ホバーの選択が消えないように）
                if (value == null) return;
                int index = mappings.IndexOf(value);
                if (index >= 0) SelectedIndex = index;
            }
        }
        public event EventHandler SelectedMappingChanged;

        private Dictionary<DS4Controls, MappedControl> controlMap =
            new Dictionary<DS4Controls, MappedControl>();
        public Dictionary<DS4Controls, MappedControl> ControlMap { get => controlMap; }

        /// <summary>
        /// DS4Controls -> Int index map. Store approriate list index for a stored MappedControl instance
        /// </summary>
        private Dictionary<DS4Controls, int> controlIndexMap =
            new Dictionary<DS4Controls, int>();
        public Dictionary<DS4Controls, int> ControlIndexMap { get => controlIndexMap; }

        private MappedControl l2FullPullControl;
        public MappedControl L2FullPullControl { get => l2FullPullControl; }

        private MappedControl r2FullPullControl;
        public MappedControl R2FullPullControl { get => r2FullPullControl; }

        private MappedControl lsOuterBindControl;
        public MappedControl LsOuterBindControl { get => lsOuterBindControl; }

        private MappedControl rsOuterBindControl;
        public MappedControl RsOuterBindControl { get => rsOuterBindControl; }

        public MappingListViewModel(int devIndex, OutContType devType, IProfileSettingsService profileSettingsService)
        {
            if (profileSettingsService == null) throw new ArgumentNullException(nameof(profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Cross, InputControlCatalog.LabelOf(DS4Controls.Cross), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Circle, InputControlCatalog.LabelOf(DS4Controls.Circle), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Square, InputControlCatalog.LabelOf(DS4Controls.Square), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Triangle, InputControlCatalog.LabelOf(DS4Controls.Triangle), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Options, InputControlCatalog.LabelOf(DS4Controls.Options), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Share, InputControlCatalog.LabelOf(DS4Controls.Share), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadUp, InputControlCatalog.LabelOf(DS4Controls.DpadUp), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadDown, InputControlCatalog.LabelOf(DS4Controls.DpadDown), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadLeft, InputControlCatalog.LabelOf(DS4Controls.DpadLeft), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadRight, InputControlCatalog.LabelOf(DS4Controls.DpadRight), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.PS, InputControlCatalog.LabelOf(DS4Controls.PS), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Mute, InputControlCatalog.LabelOf(DS4Controls.Mute), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L1, InputControlCatalog.LabelOf(DS4Controls.L1), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R1, InputControlCatalog.LabelOf(DS4Controls.R1), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L2, InputControlCatalog.LabelOf(DS4Controls.L2), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R2, InputControlCatalog.LabelOf(DS4Controls.R2), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L3, InputControlCatalog.LabelOf(DS4Controls.L3), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R3, InputControlCatalog.LabelOf(DS4Controls.R3), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Capture, InputControlCatalog.LabelOf(DS4Controls.Capture), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SideL, InputControlCatalog.LabelOf(DS4Controls.SideL), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SideR, InputControlCatalog.LabelOf(DS4Controls.SideR), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchLeft, InputControlCatalog.LabelOf(DS4Controls.TouchLeft), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchRight, InputControlCatalog.LabelOf(DS4Controls.TouchRight), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchMulti, InputControlCatalog.LabelOf(DS4Controls.TouchMulti), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchUpper, InputControlCatalog.LabelOf(DS4Controls.TouchUpper), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.LYNeg, InputControlCatalog.LabelOf(DS4Controls.LYNeg), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LYPos, InputControlCatalog.LabelOf(DS4Controls.LYPos), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LXNeg, InputControlCatalog.LabelOf(DS4Controls.LXNeg), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LXPos, InputControlCatalog.LabelOf(DS4Controls.LXPos), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.RYNeg, InputControlCatalog.LabelOf(DS4Controls.RYNeg), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RYPos, InputControlCatalog.LabelOf(DS4Controls.RYPos), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RXNeg, InputControlCatalog.LabelOf(DS4Controls.RXNeg), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RXPos, InputControlCatalog.LabelOf(DS4Controls.RXPos), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroZNeg, InputControlCatalog.LabelOf(DS4Controls.GyroZNeg), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroZPos, InputControlCatalog.LabelOf(DS4Controls.GyroZPos), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroXPos, InputControlCatalog.LabelOf(DS4Controls.GyroXPos), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroXNeg, InputControlCatalog.LabelOf(DS4Controls.GyroXNeg), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeUp, InputControlCatalog.LabelOf(DS4Controls.SwipeUp), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeDown, InputControlCatalog.LabelOf(DS4Controls.SwipeDown), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeLeft, InputControlCatalog.LabelOf(DS4Controls.SwipeLeft), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeRight, InputControlCatalog.LabelOf(DS4Controls.SwipeRight), devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.FnL, InputControlCatalog.LabelOf(DS4Controls.FnL), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.FnR, InputControlCatalog.LabelOf(DS4Controls.FnR), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.BLP, InputControlCatalog.LabelOf(DS4Controls.BLP), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.BRP, InputControlCatalog.LabelOf(DS4Controls.BRP), devType, profileSettingsService));

            /*
             * Phase10-Step3b W2: 仮想ボタン（スティックの Outer Bind、トリガーの Full Pull）の DS4ControlSettings も、
             * ほかのボタンと同じ形式で、一覧の末尾（添字 34〜37）に加える。Axis Config にあった入口を、Controls タブの一覧へ移したため
             * （コントローラーの入力による選択の添字 0〜33 は、変わらない）
             */
            lsOuterBindControl = new MappedControl(devIndex, DS4Controls.LSOuter, InputControlCatalog.LabelOf(DS4Controls.LSOuter), devType, profileSettingsService);
            rsOuterBindControl = new MappedControl(devIndex, DS4Controls.RSOuter, InputControlCatalog.LabelOf(DS4Controls.RSOuter), devType, profileSettingsService);

            l2FullPullControl = new MappedControl(devIndex, DS4Controls.L2FullPull, InputControlCatalog.LabelOf(DS4Controls.L2FullPull), devType, profileSettingsService);
            r2FullPullControl = new MappedControl(devIndex, DS4Controls.R2FullPull, InputControlCatalog.LabelOf(DS4Controls.R2FullPull), devType, profileSettingsService);

            mappings.Add(lsOuterBindControl);
            mappings.Add(rsOuterBindControl);
            mappings.Add(l2FullPullControl);
            mappings.Add(r2FullPullControl);

            // Phase10-Step4c W5（項目 9、ユーザー承認 2026-10-21）: SA のトリガーにあって Controls の一覧になかった Touch Started／Touch Ended を、末尾の後ろに足す
            // （既存の項目の添字、コントローラーの入力による選択の添字は変えない）。SA の一覧と入力の集合を同じにするため
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchStarted, InputControlCatalog.LabelOf(DS4Controls.TouchStarted), devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchEnded, InputControlCatalog.LabelOf(DS4Controls.TouchEnded), devType, profileSettingsService));

            int controlIndex = 0;
            foreach (MappedControl mapped in mappings)
            {
                controlMap.Add(mapped.Control, mapped);
                controlIndexMap.Add(mapped.Control, controlIndex);
                controlIndex++;
            }

            BuildDisplayMappings();
        }

        // Phase9 UI 変更: 表示用の並びは、InputControlCatalog（SA のトリガーの一覧と共通）の順。カタログにない項目は、末尾に足す
        private void BuildDisplayMappings()
        {
            var added = new HashSet<MappedControl>();
            foreach (InputControlEntry entry in InputControlCatalog.Entries)
            {
                if (controlMap.TryGetValue(entry.Control, out MappedControl mapped) && added.Add(mapped))
                {
                    displayMappings.Add(mapped);
                }
            }

            foreach (MappedControl mapped in mappings)
            {
                if (added.Add(mapped)) displayMappings.Add(mapped);
            }
        }

        public void UpdateMappingDevType(OutContType devType)
        {
            foreach (MappedControl mapped in mappings)
            {
                mapped.DevType = devType;
            }
        }

        public void UpdateMappings()
        {
            foreach (MappedControl mapped in mappings)
            {
                mapped.UpdateMappingName();
            }
        }
    }
}
