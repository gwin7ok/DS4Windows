using System;
using System.Drawing;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>ライトバーの色（カスタム色の文字列の解析・組み立て、色の入れ物）。</summary>
    public partial class BackingStore
    {
        private void SetupDefaultColors()
        {
            lightbarSlots[0].Info.ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSlots[1].Info.ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSlots[2].Info.ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSlots[3].Info.ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSlots[4].Info.ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSlots[5].Info.ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSlots[6].Info.ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSlots[7].Info.ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSlots[8].Info.ds4winSettings.m_Led = new DS4Color(Color.White);
        }

        public static void ParseCustomLedString(string source, LightbarDS4WinInfo destination)
        {
            try
            {
                string[] ss = source.Split(':');
                bool.TryParse(ss[0], out destination.useCustomLed);
                DS4Color.TryParse(ss[1], ref destination.m_CustomLed);
            }
            catch
            {
                destination.useCustomLed = false;
                destination.m_CustomLed = new DS4Color(Color.Blue);
            }
        }

        public static string CompileCustomLedString(LightbarDS4WinInfo ledInfo)
        {
            string result = $"{ledInfo.useCustomLed}:{ledInfo.m_CustomLed.red},{ledInfo.m_CustomLed.green},{ledInfo.m_CustomLed.blue}";
            return result;

        }

        public void PopulateLightbarDS4WinInfo(int index, LightbarDS4WinInfo source)
        {
            if (index < 0 && index > ControllerLimits.MaxControllerCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            lightbarSlots[index].Info.ds4winSettings.useCustomLed = source.useCustomLed;
            lightbarSlots[index].Info.ds4winSettings.m_CustomLed = source.m_CustomLed;
        }

        public LightbarDS4WinInfo ObtainLightbarDS4WinInfo(int index)
        {
            if (index < 0 && index > ControllerLimits.MaxControllerCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            return lightbarSlots[index].Info.ds4winSettings;
        }
    }
}
