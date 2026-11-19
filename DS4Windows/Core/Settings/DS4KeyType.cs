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
    [Flags]
    public enum DS4KeyType : byte { None = 0, ScanCode = 1, Toggle = 2, Unbound = 4, Macro = 8, RepeatMacro = 32, RepeatLatchMacro = 64 }; // Increment by exponents of 2*, starting at 2^0。値 16（旧 HoldMacro）は欠番（Phase8-Fix-MacroRepeatUnify で RepeatMacro に統合。保存はビット値ではなく名前）。RepeatLatchMacro（R-2）は、マクロの「繰り返しをトグルする」Execute Mode の実行時の表現で、保存には書かない（保存は MacroExecuteMode の設定項目。R2-MacroToggleRepeat-Plan.md §3.1）
}

