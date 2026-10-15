using System;
using DS4Windows;

namespace DS4Windows.DI
{
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public int SlotIndex { get; }
        public bool IsConnected { get; }

        public DeviceStateChangedEventArgs(int slotIndex, bool isConnected)
        {
            SlotIndex = slotIndex;
            IsConnected = isConnected;
        }
    }
}