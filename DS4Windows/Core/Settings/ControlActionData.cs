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

#nullable enable

using DS4Windows.Core.Model;
namespace DS4Windows.Core.Settings
{

    public class ControlActionData
    {
        // Store base mapping value. Uses Windows virtual key values as the base
        // https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        public int actionKey;

        // Alias to real value for current output KB+M event system.
        // Allows skipping a translation call every frame
        public uint actionAlias = 0;
        public X360Controls actionBtn;
        public int[] actionMacro = new int[1];
    }
}

