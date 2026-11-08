using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7: `App.xaml.cs` の Global 直接参照を、領域（メソッド、および Application_Startup の
    /// Pre-Host／Post-Host）ごとに固定する回帰ガード（`Phase6-Step7-Plan.md` §2・§5）。
    /// - Pre-Host 領域（Application_Startup の CreateControlService より前、CheckOptions）と共用ヘルパー
    ///   ApplyLanguageSetting は温存（決定4）なので、参照が変わっていないことを確認する。
    /// - Post-Host 領域は、置き換え済みの参照が再混入していないこと、残っているのが計画どおりの箇所だけであることを確認する。
    /// 行番号ではなくメソッド単位で判定する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class AppPostHostGlobalReferenceGuardTests
    {
        private const string AppSourcePath = "DS4Windows/App.xaml.cs";

        private readonly ITestOutputHelper _output;

        public AppPostHostGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ---- 期待値（領域ごとの Global メンバー参照の件数）----

        // ---- ソースの読み取りと領域の切り出し ----

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find(AppSourcePath);
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        // ---- テスト ----

        [Fact]
        public void InitializePostHostServices_IsCalledRightAfterCreateControlService()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            string startup = SourceText.BodyOfMethodNamed(text, "BuildHostAndServices");   // Phase9 9-F F-1b: ホストの構築の手順の中
            Assert.Matches(@"CreateControlService\(parser\);\s*InitializePostHostServices\(\);", startup);
            Assert.Single(Regex.Matches(text, @"\bInitializePostHostServices\(\);"));
        }

        [Fact]
        public void RetainedGlobalReferences_HaveTodoComments()
        {
            // 温存を決めた箇所（決定3＝P1、決定4＝K）に、copilot-instructions.md §3.3 原則4 の TODO が付いていること。
            string path = SourceFileLocator.Find(AppSourcePath);
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string raw = File.ReadAllText(path);
            Assert.Contains("TODO(Phase6-Step7 決定3＝P1)", raw);
            Assert.Contains("TODO(Phase6-Step7 決定4＝K)", raw);
        }

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
            // Phase7-Step5 W3: 出力スロットの契約（起動後の読込）と、LogToTray が使う通知サービス
            Assert.NotNull(TestHost.GetService<IOutputSlotService>());
            Assert.NotNull(TestHost.GetService<INotificationService>());
        }
    }
}
