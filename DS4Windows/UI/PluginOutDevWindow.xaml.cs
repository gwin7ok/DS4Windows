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

using System.Windows;
using DS4Windows.Core.Model;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for PluginOutDevWindow.xaml
    /// </summary>
    public partial class PluginOutDevWindow : Window
    {
        private MessageBoxResult result = MessageBoxResult.Cancel;
        public MessageBoxResult Result { get => result; }

        private DS4Windows.Core.Model.OutContType contType = DS4Windows.Core.Model.OutContType.None;
        public OutContType ContType { get => contType; }

        private OutputSlotReserveStatus reserveType;
        public OutputSlotReserveStatus ReserveType { get => reserveType; }

        public PluginOutDevWindow()
        {
            InitCommands();
            InitializeComponent();
        }

        private void AcceptBtn()
        {
            if (OutContTypeInfo.TryFromProfileChoiceIndex(devTypeCombo.SelectedIndex, out OutContType selectedType))
            {
                contType = selectedType;
            }

            switch(reserveTypeCombo.SelectedIndex)
            {
                case 0:
                    reserveType = OutputSlotReserveStatus.Dynamic;
                    break;
                case 1:
                    reserveType = OutputSlotReserveStatus.Permanent;
                    break;
                default:
                    break;
            }

            if (contType != OutContType.None)
            {
                result = MessageBoxResult.OK;
            }

            Close();
        }

        private void CancelBtn()
        {
            contType = OutContType.None;
            result = MessageBoxResult.Cancel;

            Close();
        }
    }
}
