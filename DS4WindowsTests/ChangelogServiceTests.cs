using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Updater;
using DS4Windows.Core.Utilities;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step17（決定 D10）: <see cref="Changelog"/> を、注入した HttpClient を使う <see cref="IChangelogService"/> の実装にした（旧: 静的クラスが App.requestClient を使っていた）。
    /// 通信を偽物に差し替えて、問い合わせ先・結果の使い回し・変更履歴の整形を確かめる。
    /// </summary>
    public class ChangelogServiceTests
    {
        [Fact]
        public void TheContainer_ResolvesTheServiceAndTheHttpClientAsSingletons()
        {
            Assert.IsType<Changelog>(TestHost.GetService<IChangelogService>());
            Assert.Same(TestHost.GetService<IChangelogService>(), TestHost.GetService<IChangelogService>());

            var client = TestHost.GetService<HttpClient>();
            Assert.Same(client, TestHost.GetService<HttpClient>());
            Assert.Contains("DS4Windows", client.DefaultRequestHeaders.UserAgent.ToString());   // 旧 App.requestClient と同じ User-Agent
        }

        [Fact]
        public void CheckNewerVersionExists_AsksTheLatestReleaseOnce_AndReusesTheResult()
        {
            var handler = new FakeHandler(_ => "{\"tag_name\":\"v999.0.0\",\"prerelease\":false,\"body\":\"x\"}");
            var changelog = new Changelog(new HttpClient(handler));

            Assert.True(changelog.CheckNewerVersionExists(out var version, null, allowCached: false, respectSkip: false));
            Assert.Equal(new Version(999, 0, 0), version);
            Assert.Equal(ReleaseSource.LatestReleaseApiUri, handler.Requests[0]);

            Assert.True(changelog.CheckNewerVersionExists(out var cached, null));   // allowCached: 2 回目は問い合わせない
            Assert.Equal(new Version(999, 0, 0), cached);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public void CheckNewerVersionExists_RespectsTheSkippedVersion()
        {
            var handler = new FakeHandler(_ => "{\"tag_name\":\"v999.0.0\",\"prerelease\":false,\"body\":\"x\"}");
            var changelog = new Changelog(new HttpClient(handler));

            Assert.False(changelog.CheckNewerVersionExists(out _, "999.0.0", allowCached: false, respectSkip: true));
        }

        [Fact]
        public async Task GetChangelogMarkdown_SkipsPreReleases_AndDropsTheFullChangelogLine()
        {
            var handler = new FakeHandler(_ => "[" +
                "{\"tag_name\":\"v999.1.0\",\"prerelease\":true,\"body\":\"beta\"}," +
                "{\"tag_name\":\"v999.0.0\",\"prerelease\":false,\"body\":\"- fixed\\n**Full Changelog**: link\"}]");
            var changelog = new Changelog(new HttpClient(handler));

            string markdown = await changelog.GetChangelogMarkdown(allVersions: true);

            Assert.Contains("## Version 999.0.0", markdown);
            Assert.Contains("- fixed", markdown);
            Assert.DoesNotContain("999.1.0", markdown);
            Assert.DoesNotContain("Full Changelog", markdown);
            Assert.Equal(ReleaseSource.ReleasesApiUri, handler.Requests[0]);
        }

        [Fact]
        public void Constructor_RequiresTheHttpClient()
        {
            Assert.Throws<ArgumentNullException>(() => new Changelog(null));
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string> _json;
            public List<string> Requests { get; } = new List<string>();

            public FakeHandler(Func<HttpRequestMessage, string> json) => _json = json;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri.ToString());
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json(request), Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
