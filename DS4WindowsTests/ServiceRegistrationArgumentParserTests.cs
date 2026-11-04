using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows.Composition;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;
using DS4Windows.Output.Kbm;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7-4（決定5＝A、決定7＝案H）: ServiceRegistration が ControlService を生成するとき、
    /// <see cref="IStartupArguments"/> に設定された実際の起動引数（<see cref="ArgumentParser"/>）を渡すこと、
    /// および DS4Windows.Composition.AppHost.CreateHost(config, parser) が、ホストが既に構築済みの場合も起動引数を設定することを検証する。
    ///
    /// 経緯: 2026-09-04（4c89cd91）以降、ControlService には常に空の new ArgumentParser() が渡され、起動引数 -virtualkbm が
    /// 無視されていた。最初の是正（コンテナに登録した ArgumentParser を使う）は、ホストが Global の静的初期化で起動引数なしに
    /// 先に作られるため実機で効かなかった（`Phase6-Step7-Plan.md` §0.4 の訂正・決定7）。このため、以下のテストは
    /// 「ホストが先に作られている」実際の起動順序を再現するものを含める。
    ///
    /// ServiceRegistration の登録は、Program.rootHub が設定済みならそれを返すため、各テストで一時的に null にし、
    /// 終了時に元へ戻す（Phase6-Step2 の教訓: static 状態を必ず復元する）。
    /// </summary>
    public class ServiceRegistrationArgumentParserTests
    {
        // Phase13-Step3: 起動引数の読み取りは、ControlService の cmdParser から OutputKbmInitializer（Initialize の時点で読む）へ移った。
        // 検査の対象も、ControlService が保持する値から、初期化部品が Initialize() で使うパーサー（ResolveParser）に変えた。
        private static ArgumentParser GetCmdParser(IOutputKbmInitializer initializer)
        {
            MethodInfo method = typeof(OutputKbmInitializer).GetMethod("ResolveParser", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (ArgumentParser)method.Invoke(initializer, null);
        }

        private static ArgumentParser CreateVirtualKbmParser()
        {
            var parser = new ArgumentParser();
            parser.Parse(new[] { "-virtualkbm", "sendinput" });
            Assert.Equal("sendinput", parser.VirtualkbmHandler);
            return parser;
        }

        [Fact]
        public void StartupArguments_HoldsTheParserThatWasSet()
        {
            var startupArguments = new StartupArguments();
            Assert.Null(startupArguments.Parser);

            var parser = new ArgumentParser();
            startupArguments.SetParser(parser);
            Assert.Same(parser, startupArguments.Parser);
        }

        [Fact]
        public void ControlService_ReceivesParserSetOnStartupArguments_AfterContainerWasBuilt()
        {
            // 実際の起動順序の再現: コンテナは起動引数なしで先に作られ、その後で起動引数が設定される。
            var services = new ServiceCollection();
            services.RegisterServices();
            ServiceProvider provider = services.BuildServiceProvider();

            var parser = CreateVirtualKbmParser();
            provider.GetRequiredService<IStartupArguments>().SetParser(parser);

            IOutputKbmInitializer initializer = provider.GetRequiredService<IOutputKbmInitializer>();
            Assert.NotNull(provider.GetRequiredService<ControlService>());

            Assert.Same(parser, GetCmdParser(initializer));
            Assert.Equal("sendinput", GetCmdParser(initializer).VirtualkbmHandler);
        }

        [Fact]
        public void ControlService_WithoutStartupArguments_UsesEmptyParser()
        {
            // 引数なしの DS4Windows.Composition.AppHost.CreateHost() やテストのホストでは、従来どおり空のパーサー（既定の出力方式）で生成できる。
            var services = new ServiceCollection();
            services.RegisterServices();

            ServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetRequiredService<ControlService>());

            ArgumentParser cmdParser = GetCmdParser(provider.GetRequiredService<IOutputKbmInitializer>());
            Assert.NotNull(cmdParser);
            Assert.Equal(VirtualKBMFactory.DEFAULT_IDENTIFIER, cmdParser.VirtualkbmHandler);
        }

        [Fact]
        public void AppHostCreateHostWithParser_OnExistingHost_SetsStartupArguments()
        {
            // 実際の起動では、App.xaml.cs の CreateHost(config, parser) の時点でホストは構築済み（既存のホストを返す経路）。
            EnsureHostCreated();
            var startupArguments = TestHost.GetService<IStartupArguments>();
            Assert.NotNull(startupArguments);

            ArgumentParser original = startupArguments.Parser;
            try
            {
                var parser = CreateVirtualKbmParser();
                var host = DS4Windows.Composition.AppHost.CreateHost(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), parser);

                Assert.Same(DS4Windows.Composition.AppHost.Host, host);
                Assert.Same(parser, startupArguments.Parser);
            }
            finally
            {
                startupArguments.SetParser(original);
            }
        }

        private static void EnsureHostCreated()
        {
            // Phase7-Step10e W3: AppHost.GetService は暗黙にホストを作らなくなったため、TestHost が（未構築なら）引数なしの CreateHost() で明示的に構築する。
            Assert.NotNull(TestHost.GetService<IStartupArguments>());
            Assert.NotNull(DS4Windows.Composition.AppHost.Host);
        }
    }
}
