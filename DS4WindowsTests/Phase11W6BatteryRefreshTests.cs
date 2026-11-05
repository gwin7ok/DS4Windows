using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6（S9-5 の確認で判明）: Controllers タブの電池表示は、変化通知だけでなく 60 秒ごとにも読み直す
    /// （通知を取りこぼすと、表示が古い値のまま止まっていた）。文字列が変わったときだけ通知し、コントローラーが外れたら止める。
    /// </summary>
    public class Phase11W6BatteryRefreshTests
    {
        [Fact]
        public void TheInterval_Is60Seconds()
        {
            string code = SourceText.ReadNormalized("DS4Windows/UI/ViewModels/CompositeDeviceModel.cs");
            if (code == null) return;

            Assert.Contains("public const int BatteryRefreshIntervalMs = 60000;", code);
            Assert.Contains("new Timer(_ => RefreshBatteryState(), null, BatteryRefreshIntervalMs, BatteryRefreshIntervalMs);", code);
        }

        [Fact]
        public void TheRefresh_NotifiesOnlyWhenTheTextChanged()
        {
            string code = SourceText.ReadNormalized("DS4Windows/UI/ViewModels/CompositeDeviceModel.cs");
            if (code == null) return;

            int refresh = code.IndexOf("public void RefreshBatteryState()", System.StringComparison.Ordinal);
            Assert.True(refresh >= 0);
            string body = code.Substring(refresh, 300);
            Assert.Contains("if (current == lastBatteryState) return;", body);
            Assert.Contains("RaiseBatteryStateChanged(EventArgs.Empty);", body);
        }

        [Fact]
        public void TheNotification_ReachesTheBinding_ThroughPropertyChanged()
        {
            // 根本原因: CompositeDeviceModel は INotifyPropertyChanged を実装しているため、WPF は BatteryStateChanged（旧式のイベント）を無視する
            string code = SourceText.ReadNormalized("DS4Windows/UI/ViewModels/CompositeDeviceModel.cs");
            if (code == null) return;

            int raise = code.IndexOf("private void RaiseBatteryStateChanged(", System.StringComparison.Ordinal);
            Assert.True(raise >= 0);
            string body = code.Substring(raise, 700);
            Assert.Contains("PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BatteryState)));", body);
        }

        [Fact]
        public void TheTimer_StopsWhenTheControllerIsRemoved()
        {
            string code = SourceText.ReadNormalized("DS4Windows/UI/ViewModels/CompositeDeviceModel.cs");
            if (code == null) return;

            Assert.Contains("device.Removal += (sender, e) => StopBatteryRefresh();", code);
            Assert.Contains("Interlocked.Exchange(ref batteryRefreshTimer, null)", code);
        }
    }
}
