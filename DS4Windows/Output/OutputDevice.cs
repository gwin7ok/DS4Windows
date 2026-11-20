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

namespace DS4Windows.Output
{
    public abstract class OutputDevice
    {
        protected bool connected;

        // 旧「SteeringWheelAxisProvider／GetSteeringWheelEmulationAxis」（ステアリングの軸の出力先の取得口）は、機能ごと削除した。
        public abstract void ConvertandSendReport(ControllerState state, int device);

        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void ResetState(bool submit=true);
        public abstract string GetDeviceType();

        public abstract void RemoveFeedbacks();

        public abstract void RemoveFeedback(int inIdx);

        /// <summary>XInput のスロット番号を取れる種類・環境か。取れない種類は false。</summary>
        public virtual bool SupportsXInputSlotNumber => false;

        /// <summary>XInput のスロット番号（0 始まり）。取れない・まだ分からないときは -1。</summary>
        public virtual int XInputSlotNumber => -1;

        /// <summary>仮想パッドを出力スロットへ差し込んだことを、利用者向けのログに出す（種類ごとに文言が違う）。</summary>
        public virtual void LogPlugged(int slot, OutContType contType)
        {
            DS4Windows.Core.Logging.AppLogger.LogUser(DS4Windows.Core.Logging.AppLogLevel.Info, "Log_Out_PluginVirtual", contType, slot + 1);
        }
    }
}
