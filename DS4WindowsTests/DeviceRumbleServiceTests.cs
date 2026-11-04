using Xunit;
using DS4Windows.Output.Services;

namespace DS4WindowsTests
{
    /// <summary>Phase13-Step3: 旧 <c>ControlService.SetDevRumble</c> の式（ブースト・上限 255・反転）の特性化。</summary>
    public class DeviceRumbleServiceTests
    {
        [Theory]
        [InlineData(100, 200, 100, false, 200, 100)]   // ブースト 100％、反転なし: right=light, left=heavy
        [InlineData(100, 200, 100, true, 100, 200)]    // 反転: right=heavy, left=light
        [InlineData(100, 200, 200, false, 255, 200)]   // ブースト 200％: light 400→255 に丸め、heavy 200
        [InlineData(200, 100, 150, false, 150, 255)]   // heavy 300→255
        [InlineData(0, 0, 255, false, 0, 0)]
        [InlineData(255, 255, 0, false, 0, 0)]         // ブースト 0％は無音
        public void Adjust_AppliesBoostClampAndInversion(byte heavy, byte light, byte boost, bool inverse, byte expectedRight, byte expectedLeft)
        {
            DeviceRumbleService.Adjust(heavy, light, boost, inverse, out byte right, out byte left);
            Assert.Equal(expectedRight, right);
            Assert.Equal(expectedLeft, left);
        }
    }
}
