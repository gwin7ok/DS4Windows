using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Xbox;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Xbox 系コントローラーの USB の親を、HidHide の非表示の一覧へ足す・外す <see cref="XboxParentHider"/> と、接続のときにそれを呼ぶ <see cref="XboxDeviceModule"/> の確認。
    /// 足した分だけを戻すこと、HidHide を使えないとき（未導入、開けない、許可リストの反転、隠す機能の無効）は何も変えないこと、異常終了のあとの復旧。
    /// HidHide のドライバー、導入済みかの確認、パスからインスタンス ID への変換（OS に問い合わせる）、保存先は、偽物と一時フォルダに替える。
    /// </summary>
    public class XboxParentHiderTests : IDisposable
    {
        private const string Path1 = @"\\?\usb#vid_045e&pid_028e#a02014100201#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private const string Id1 = @"USB\VID_045E&PID_028E\A02014100201";
        private const string UserEntry = @"HID\VID_054C&PID_09CC\USERADDED";

        private readonly string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-xboxhider-" + Guid.NewGuid().ToString("N"));
        private const string Path2 = @"\\?\usb#vid_045e&pid_02ff#b0000000001#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private bool installed = true;
        private readonly FakeHidHideApi api = new FakeHidHideApi { Active = true, Opened = true };
        private readonly FakeHidHideService hidHideService = new FakeHidHideService();

        public XboxParentHiderTests()
        {
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        private static readonly Dictionary<string, string> InstanceIds = new Dictionary<string, string>
        {
            [Path1] = Id1,
            [Path2] = @"USB\VID_045E&PID_02FF\B0000000001",
        };

        private XboxParentHider NewHider() =>
            new XboxParentHider(() => installed, path => InstanceIds.TryGetValue(path, out string id) ? id : string.Empty, new TempPathService(tempDir), api, hidHideService);

        private string RecordPath => System.IO.Path.Combine(tempDir, XboxParentHider.RecordFileName);

        [Fact]
        public void Hide_AddsTheDeviceInstance_KeepsTheUsersEntries_AndRegistersTheAppInTheWhitelistFirst()
        {
            api.Blacklist = new List<string> { UserEntry };

            XboxParentHideResult result = NewHider().Apply(Path1, hide: true);

            Assert.Equal(XboxParentHideResult.Hidden, result);
            Assert.Equal(new[] { UserEntry, Id1 }, api.Blacklist);
            Assert.Equal(1, hidHideService.CheckPresenceCalls);   // 隠したあとも読めるよう、先に許可リストへ
            Assert.Equal(new[] { Id1 }, File.ReadAllLines(RecordPath));
        }

        [Fact]
        public void Hide_DoesNothing_WhenTheDeviceIsAlreadyInTheList_AndItIsNotRestoredLater()
        {
            api.Blacklist = new List<string> { Id1.ToLowerInvariant() };
            XboxParentHider hider = NewHider();

            Assert.Equal(XboxParentHideResult.AlreadyHidden, hider.Apply(Path1, hide: true));
            Assert.Empty(api.SetBlacklistCalls);

            // 利用者が入れた分は、自分が足した分ではないので、戻さない
            Assert.Equal(XboxParentHideResult.NothingToRestore, hider.Apply(Path1, hide: false));
            hider.RestoreAll();
            Assert.Equal(new[] { Id1.ToLowerInvariant() }, api.Blacklist);
        }

        [Fact]
        public void Restore_RemovesOnlyWhatTheHiderAdded_AndDeletesTheRecord()
        {
            api.Blacklist = new List<string> { UserEntry };
            XboxParentHider hider = NewHider();
            hider.Apply(Path1, hide: true);

            XboxParentHideResult result = hider.Apply(Path1, hide: false);

            Assert.Equal(XboxParentHideResult.Restored, result);
            Assert.Equal(new[] { UserEntry }, api.Blacklist);
            Assert.False(File.Exists(RecordPath));
        }

        [Fact]
        public void RestoreAll_RemovesEveryAddedDevice_ButNotTheUsersEntries()
        {
            api.Blacklist = new List<string> { UserEntry };
            XboxParentHider hider = NewHider();
            hider.Apply(Path1, hide: true);
            hider.Apply(Path2, hide: true);
            Assert.Equal(3, api.Blacklist.Count);

            hider.RestoreAll();

            Assert.Equal(new[] { UserEntry }, api.Blacklist);
            Assert.False(File.Exists(RecordPath));
        }

        [Fact]
        public void Hide_ChangesNothing_WhenHidHideIsNotInstalled()
        {
            installed = false;

            Assert.Equal(XboxParentHideResult.HidHideNotInstalled, NewHider().Apply(Path1, hide: true));
            Assert.Empty(api.SetBlacklistCalls);
            Assert.Equal(0, hidHideService.CheckPresenceCalls);
        }

        [Fact]
        public void Hide_ChangesNothing_WhenTheControlDeviceCannotBeOpened()
        {
            api.Opened = false;

            Assert.Equal(XboxParentHideResult.NoAccess, NewHider().Apply(Path1, hide: true));
            Assert.Empty(api.SetBlacklistCalls);
            Assert.False(File.Exists(RecordPath));
        }

        [Fact]
        public void Hide_ChangesNothing_WhenTheWhitelistIsInverted_OrHidingIsTurnedOff()
        {
            api.Inverse = true;
            Assert.Equal(XboxParentHideResult.InverseWhitelist, NewHider().Apply(Path1, hide: true));

            api.Inverse = false;
            api.Active = false;
            Assert.Equal(XboxParentHideResult.CloakOff, NewHider().Apply(Path1, hide: true));

            Assert.Empty(api.SetBlacklistCalls);   // 利用者の設定（反転、全体のスイッチ）は、切り替えない
        }

        [Fact]
        public void Hide_ReportsAFailure_WhenTheListCannotBeWritten_AndRecordsNothing()
        {
            api.AllowBlacklistWrite = false;

            Assert.Equal(XboxParentHideResult.WriteFailed, NewHider().Apply(Path1, hide: true));
            Assert.False(File.Exists(RecordPath));
        }

        [Fact]
        public void RecoverStale_RemovesWhatAPreviousRunLeftBehind_AfterACrash()
        {
            api.Blacklist = new List<string> { UserEntry };
            NewHider().Apply(Path1, hide: true);
            // 異常終了: 新しいインスタンス（記録ファイルだけが残っている）
            XboxParentHider restarted = NewHider();

            restarted.RecoverStale();

            Assert.Equal(new[] { UserEntry }, api.Blacklist);
            Assert.False(File.Exists(RecordPath));
        }

        [Fact]
        public void RestoreAll_KeepsTheRecord_WhenTheControlDeviceCannotBeOpened()
        {
            XboxParentHider hider = NewHider();
            hider.Apply(Path1, hide: true);
            api.Opened = false;

            hider.RestoreAll();

            Assert.True(File.Exists(RecordPath));   // 次の開始で戻す
            Assert.Contains(Id1, api.Blacklist);
        }

        // ---- 接続のときの呼び出し（XboxDeviceModule）----

        private static InputDeviceCore NewXboxDevice()
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path1, 0x045E, 0x028E, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();
            return new XboxDeviceCartridge().Create(InputDeviceType.Xbox, candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TheModule_AppliesTheSetting_WhenAnXboxDeviceIsInitialised(bool hideUsbParent)
        {
            var hider = new FakeXboxParentHider();
            var options = new ControlServiceDeviceOptions();
            options.Get<XboxDeviceOptions>(InputDeviceFamily.Xbox).HideUsbParent = hideUsbParent;

            new XboxDeviceModule(hider).OnDeviceInit(NewXboxDevice(), options);

            Assert.Equal((Path1, hideUsbParent), hider.Applied.Single());
        }

        [Fact]
        public void TheSetting_IsOffByDefault_AndIsSavedWithTheXboxSection()
        {
            var options = new XboxDeviceOptions();

            Assert.False(options.HideUsbParent);
            Assert.Equal(new[] { "Enabled", "HideUsbParent" }, options.Settings.Select(s => s.XmlName));
        }
    }
}
