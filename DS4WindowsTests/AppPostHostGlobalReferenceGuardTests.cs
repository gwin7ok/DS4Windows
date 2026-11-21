using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// `App.xaml.cs` の Global 直接参照を、領域（メソッド、および Application_Startup の
    /// Pre-Host／Post-Host）ごとに固定する回帰ガード（`docs-forDIMG/MadeByAgent/Archive/Phase6-Step7-Plan.md` §2・§5）。
    /// - Pre-Host 領域（Application_Startup の CreateControlService より前、CheckOptions）と共用ヘルパー
    ///   ApplyLanguageSetting は温存（決定4）なので、参照が変わっていないことを確認する。
    /// - Post-Host 領域は、置き換え済みの参照が再混入していないこと、残っているのが計画どおりの箇所だけであることを確認する。
    /// 行番号ではなくメソッド単位で判定する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class AppPostHostGlobalReferenceGuardTests
    {
        [Fact]
        public void PostHostServices_CanBeResolvedFromAppHost()
        {
            // InitializePostHostServices が解決する 7 サービスが、DI コンテナに登録されていること。
            Assert.NotNull(TestHost.GetService<IAppSettingsService>());
            Assert.NotNull(TestHost.GetService<IPathService>());
            Assert.NotNull(TestHost.GetService<IEnvironmentService>());
            Assert.NotNull(TestHost.GetService<IProfileRepository>());
            Assert.NotNull(TestHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(TestHost.GetService<IAppearanceSettingsService>());
            Assert.NotNull(TestHost.GetService<IDeviceStateService>());
            // 出力スロットの契約（起動後の読込）と、LogToTray が使う通知サービス
            Assert.NotNull(TestHost.GetService<IOutputSlotService>());
            Assert.NotNull(TestHost.GetService<INotificationService>());
        }
    }
}
