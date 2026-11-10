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
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;

namespace DS4Windows.Processing
{
    public class ControllerSlotManager : IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        private ReaderWriterLockSlim collectionLocker = new ReaderWriterLockSlim();
        public ReaderWriterLockSlim CollectionLocker { get => collectionLocker; }

        private List<InputDeviceCore> controllerColl;
        public List<InputDeviceCore> ControllerColl { get => controllerColl; set => controllerColl = value; }
        
        private Dictionary<int, InputDeviceCore> controllerDict;
        private Dictionary<InputDeviceCore, int> reverseControllerDict;
        public Dictionary<int, InputDeviceCore> ControllerDict { get => controllerDict; }
        public Dictionary<InputDeviceCore, int> ReverseControllerDict { get => reverseControllerDict; }

        public ControllerSlotManager()
        {
            controllerColl = new List<InputDeviceCore>();
            controllerDict = new Dictionary<int, InputDeviceCore>();
            reverseControllerDict = new Dictionary<InputDeviceCore, int>();
        }

        public void AddController(InputDeviceCore device, int slotIdx)
        {
            using (WriteLocker locker = new WriteLocker(collectionLocker))
            {
                controllerColl.Add(device);
                controllerDict.Add(slotIdx, device);
                reverseControllerDict.Add(device, slotIdx);
            }
        }

        public void RemoveController(InputDeviceCore device, int slotIdx)
        {
            using (WriteLocker locker = new WriteLocker(collectionLocker))
            {
                controllerColl.Remove(device);
                controllerDict.Remove(slotIdx);
                reverseControllerDict.Remove(device);
            }
        }

        public void ClearControllerList()
        {
            using (WriteLocker locker = new WriteLocker(collectionLocker))
            {
                controllerColl.Clear();
                controllerDict.Clear();
                reverseControllerDict.Clear();
            }
        }
    }
}
