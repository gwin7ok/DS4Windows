using DS4Windows.Core.Settings;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 振動の設定（強さ・自動停止・モーターの入れ替え）の持ち主 <see cref="RumbleSlotSettings"/> が、
    /// 読み込み・画面の書き込みと、実行時に読む値（<c>GetRumbleBoost</c>／<c>GetRumbleAutostopTime</c>）で同じ値を共有することを守る。
    /// 以前は、実行時に読む値が別のコピーを見ていたため、プロファイルの値が効かなかった。
    /// </summary>
    public class RumbleSlotSettingsTests
    {
        [Fact]
        public void TheRuntimeReads_FollowTheValuesWrittenToTheSlot()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));

            Assert.Equal(100, settings.GetRumbleBoost(2));
            Assert.Equal(0, settings.GetRumbleAutostopTime(2));

            settings.RumbleSlot(2).Boost = 60;
            settings.RumbleSlot(2).AutostopTime = 3000;

            Assert.Equal(60, settings.GetRumbleBoost(2));
            Assert.Equal(3000, settings.GetRumbleAutostopTime(2));
            Assert.Equal(100, settings.GetRumbleBoost(1));   // 他のスロットは変わらない
        }

        [Fact]
        public void ResetForProfileLoad_RestoresTheDefaults()
        {
            var slot = new RumbleSlotSettings(0) { Boost = 10, AutostopTime = 5, InverseMotors = true };

            slot.ResetForProfileLoad();

            Assert.Equal(100, slot.Boost);
            Assert.Equal(0, slot.AutostopTime);
            Assert.False(slot.InverseMotors);
        }
    }
}
