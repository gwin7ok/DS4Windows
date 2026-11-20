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

namespace DS4Windows.Core.Settings
{
    public abstract class PresetOption
    {
        public enum OutputContChoice : ushort
        {
            None,
            Xbox360,
            DualShock4,
        }

        protected string name;
        protected string description;
        protected bool outputControllerChoice;
        protected OutputContChoice outputCont;
        // 全サブクラス共通のControlService注入(Program.rootHubフォールバック)。
        // これらのPresetクラスはPresetOptionViewModelで new() 経由でパラメータレスに生成されるため、
        // サブクラス側の変更は不要(既定のフォールバック付きコンストラクタが暗黙的に呼ばれる)。
        protected readonly DS4Windows.Core.Abstractions.IProfileLoadTarget loadTarget;
        // Global.Load…Profile への直接呼び出しを、プリセット読み込みの契約経由に置き換えた（必須引数）。
        protected readonly DS4Windows.Core.Abstractions.IProfilePresetLoader presetLoader;

        public string Name { get => name; }
        public string Description { get => description; }
        public bool OutputControllerChoice { get => outputControllerChoice; }
        public OutputContChoice OutputCont
        {
            get => outputCont;
            set => outputCont = value;
        }

        protected PresetOption(DS4Windows.Core.Abstractions.IProfilePresetLoader presetLoader, DS4Windows.Core.Abstractions.IProfileLoadTarget loadTarget)
        {
            ArgumentNullException.ThrowIfNull(presetLoader);
            this.presetLoader = presetLoader;
            ArgumentNullException.ThrowIfNull(loadTarget);
            this.loadTarget = loadTarget;
        }

        public abstract void ApplyPreset(int idx);
    }
}