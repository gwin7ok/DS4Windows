using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのLightbarの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class LightbarSlotSettings
    {
        public LightbarSlotSettings(int slot)
        {
            Slot = slot;
            Info = new LightbarSettingInfo();
        }

        public int Slot { get; }

        public LightbarSettingInfo Info { get; }

        /// <summary>ライトバーの設定を既定へ戻す（プロファイルの読み込みの前）。既定の色は、スロットの番号で決まる。</summary>
        public void ResetForProfileLoad()
        {
            LightbarSettingInfo lightbarSettings = Info;
            LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
            lightbarSettings.Mode = LightbarMode.DS4Win;
            lightInfo.m_LowLed = new DS4Color(System.Drawing.Color.Black);

            System.Drawing.Color tempColor;
            switch (Slot)
            {
                case 0: tempColor = System.Drawing.Color.Blue; break;
                case 1: tempColor = System.Drawing.Color.Red; break;
                case 2: tempColor = System.Drawing.Color.Green; break;
                case 3: tempColor = System.Drawing.Color.Pink; break;
                case 4: tempColor = System.Drawing.Color.Blue; break;
                case 5: tempColor = System.Drawing.Color.Red; break;
                case 6: tempColor = System.Drawing.Color.Green; break;
                case 7: tempColor = System.Drawing.Color.Pink; break;
                case 8: tempColor = System.Drawing.Color.White; break;
                default: tempColor = System.Drawing.Color.Blue; break;
            }

            lightInfo.m_Led = new DS4Color(tempColor);
            lightInfo.m_ChargingLed = new DS4Color(System.Drawing.Color.Black);
            lightInfo.m_FlashLed = new DS4Color(System.Drawing.Color.Black);
            lightInfo.flashAt = 0;
            lightInfo.flashType = 0;
            lightInfo.chargingType = 0;
            lightInfo.rainbow = 0;
            lightInfo.maxRainbowSat = LightbarDS4WinInfo.DEFAULT_MAX_RAINBOW_SAT;
            lightInfo.ledAsBattery = false;
        }
    }
}
