using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>遅延の窓（直近 20 フレームの平均）。</summary>
    public class LatencyWindowTests
    {
        [Fact]
        public void Add_AveragesTheValuesSoFar_UntilTheWindowIsFull()
        {
            var window = new LatencyWindow();
            Assert.Equal(4.0, window.Add(4));
            Assert.Equal(5.0, window.Add(6));
        }

        [Fact]
        public void Add_DropsTheOldestValue_OnceTwentyValuesAreHeld()
        {
            var window = new LatencyWindow();
            for (int i = 0; i < LatencyWindow.Capacity; i++)
            {
                window.Add(10);
            }

            // 21 件目で、最初の 10 が窓から出る: (19 * 10 + 30) / 20
            Assert.Equal(11.0, window.Add(30));
        }
    }
}
