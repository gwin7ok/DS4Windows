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
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// SA の編集画面（<c>SpecialActionEditor</c>）の ViewModel。Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）で、中身を
    /// <see cref="ActionEditorViewModel"/>（共通の土台）へ移し、これは、SA のモードの部品（<see cref="SpecialActionTriggerSource"/>、
    /// <see cref="SpecialActionEditTarget"/>、<see cref="ActionTypeCatalog"/>）を組み合わせるだけの薄い派生クラスにした。
    /// 公開メンバー・検証・エラーの文言は、従来と同じ。
    /// </summary>
    public class SpecialActEditorViewModel : ActionEditorViewModel
    {
        private readonly SpecialActionTriggerSource specialTriggers;

        // Phase6-Step10-3-2: Global.GetActions の置換先（必須引数。ActionList は Global.GetActions と同じリスト）
        public SpecialActEditorViewModel(int deviceNum, SpecialAction action, ISpecialActionRepository specialActionRepository)
            : this(deviceNum, new SpecialActionTriggerSource(), new SpecialActionEditTarget(specialActionRepository, action))
        {
        }

        private SpecialActEditorViewModel(int deviceNum, SpecialActionTriggerSource triggers, SpecialActionEditTarget target)
            : base(deviceNum, triggers, target, new ActionTypeCatalog())
        {
            specialTriggers = triggers;
        }

        /// <summary>選ばれているトリガー（名前）。画面（チェックボックスのクリック）が、直接書き換える（W2 でデータ駆動の部品に置き換えるまで）。</summary>
        public List<string> ControlTriggerList { get => specialTriggers.TriggerList; }

        /// <summary>解除トリガー（名前）。画面には出さないが、保存済みの値を保って書き戻す。</summary>
        public List<string> ControlUnloadTriggerList { get => specialTriggers.UnloadTriggerList; }
    }
}
