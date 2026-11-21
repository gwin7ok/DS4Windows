using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>ボタンごとの割り当て（DS4ControlSettings）の更新・取得。</summary>
    public partial class BackingStore
    {
        public void UpdateDS4CSetting(int deviceNum, string buttonName, object action, string exts, DS4KeyType kt)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = DS4ControlNames.GetDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                dcs.UpdateSettings(action, exts, kt);
                ProfileSettings.RefreshActionAlias(dcs);
            }
        }

        public void UpdateDS4CExtra(int deviceNum, string buttonName, string exts)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = DS4ControlNames.GetDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                dcs.extras = exts;
            }
        }

        public void UpdateDS4CKeyType(int deviceNum, string buttonName, DS4KeyType keyType)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = DS4ControlNames.GetDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                dcs.keyType = keyType;
            }
        }

        public DS4ControlSettings? GetDS4CSetting(int deviceNum, string buttonName)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = DS4ControlNames.GetDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public DS4ControlSettings GetRequiredDS4CSetting(int deviceNum, DS4Controls dc)
        {
            return GetDS4CSetting(deviceNum, dc) ?? throw new InvalidOperationException($"コントロール {dc} の設定がない");
        }

        public DS4ControlSettings? GetDS4CSetting(int deviceNum, DS4Controls dc)
        {
            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public bool HasCustomActions(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.actionType != DS4ControlSettings.ActionType.Default)
                    return true;
            }

            return false;
        }

        public bool HasCustomExtras(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.extras != null)
                    return true;
            }

            return false;
        }
    }
}
