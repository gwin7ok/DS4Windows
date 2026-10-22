using System;
using System.IO;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 再接続時のプロファイルを、コントローラー単位で管理する修正（docs-forDIMG/MadeByAgent/Reconnect-Profile-PerController-Fix-Plan.md）の確認。
    /// 症状: コントローラー 1（Linked = A）を接続して切断し、コントローラー 2（Linked = B）を接続すると、2 は初回接続なのに、
    /// 同じスロットの「再接続」と判定されて A が適用された（初回／再接続の判定と直前のプロファイルが、スロット番号で管理されていたため）。
    /// 初回接続のフラグ（BackingStore の配列）を共有するため、他の Host 構築テストと同じコレクションで直列に実行し、終了時に元へ戻す。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class ReconnectProfilePerControllerTests : IDisposable
    {
        private const string Mac1 = "A4:53:85:3A:D5:84";
        private const string Mac2 = "00:11:22:33:44:55";

        private readonly bool[] _savedFlags = (bool[])TestGlobals.Store.firstConnectionAfterStartup.Clone();
        private readonly DeviceStateService _service = TestRepositoryFactory.CreateDeviceStateService();

        public ReconnectProfilePerControllerTests()
        {
            _service.ResetConnectionFlags();   // 起動直後の状態
        }

        public void Dispose()
        {
            _service.ResetConnectionFlags();
            Array.Copy(_savedFlags, TestGlobals.Store.firstConnectionAfterStartup, _savedFlags.Length);
        }

        [Fact]
        public void AnotherControllerInTheSameSlot_IsAFirstConnection()
        {
            // 症状の再現: スロット 0 で、コントローラー 1 を接続して切断 → コントローラー 2 を接続
            Assert.True(_service.IsFirstConnection(0, Mac1));
            _service.MarkConnected(0, Mac1);
            Assert.False(_service.IsFirstConnection(0, Mac1));   // 同じコントローラーの再接続

            Assert.True(_service.IsFirstConnection(0, Mac2));      // 別のコントローラーは、同じスロットでも初回
        }

        [Fact]
        public void TheSameControllerInAnotherSlot_IsAReconnection()
        {
            _service.MarkConnected(0, Mac1);

            Assert.False(_service.IsFirstConnection(1, Mac1));   // スロット 1 のフラグは true のままでも、このコントローラーは再接続
            Assert.True(_service.IsFirstConnection(1));           // スロット単位の判定は、従来のまま
        }

        [Fact]
        public void TheMacAddressIsComparedCaseInsensitively()
        {
            _service.MarkConnected(0, Mac1);

            Assert.False(_service.IsFirstConnection(0, Mac1.ToLowerInvariant()));
        }

        [Fact]
        public void WithoutAMacAddress_TheSlotBasedBehaviorRemains()
        {
            Assert.True(_service.IsFirstConnection(2, null));
            Assert.True(_service.IsFirstConnection(2, string.Empty));

            _service.MarkConnected(2, null);

            Assert.False(_service.IsFirstConnection(2, null));   // 有効なシリアルがないコントローラーは、従来どおり、スロットのフラグ
            Assert.False(_service.IsFirstConnection(2));
            Assert.True(_service.IsFirstConnection(2, Mac1));     // MAC つきの判定は、スロットのフラグを見ない
        }

        [Fact]
        public void RememberedProfiles_AreKeptPerController()
        {
            Assert.Null(_service.GetRememberedProfile(Mac1));

            _service.RememberProfile(Mac1, "A");
            _service.RememberProfile(Mac2, "B");

            Assert.Equal("A", _service.GetRememberedProfile(Mac1));
            Assert.Equal("B", _service.GetRememberedProfile(Mac2));

            _service.RememberProfile(Mac1, "C");   // 切り替えるたびに、最後のプロファイルへ更新する
            Assert.Equal("C", _service.GetRememberedProfile(Mac1));
            Assert.Equal("B", _service.GetRememberedProfile(Mac2));
            Assert.Equal("C", _service.GetRememberedProfile(Mac1.ToLowerInvariant()));
        }

        [Fact]
        public void BlankValues_AreIgnored()
        {
            _service.RememberProfile(null, "A");
            _service.RememberProfile(string.Empty, "A");
            _service.RememberProfile(Mac1, null);
            _service.RememberProfile(Mac1, "  ");

            Assert.Null(_service.GetRememberedProfile(null));
            Assert.Null(_service.GetRememberedProfile(Mac1));
        }

        [Fact]
        public void ResetConnectionFlags_ClearsTheControllerBasedState()
        {
            _service.MarkConnected(0, Mac1);
            _service.RememberProfile(Mac1, "A");

            _service.ResetConnectionFlags();

            Assert.True(_service.IsFirstConnection(0, Mac1));
            Assert.Null(_service.GetRememberedProfile(Mac1));
        }

        [Fact]
        public void TheControlService_DecidesPerController_AndRemembersEveryAppliedProfile()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (path == null) return;

            string controlServiceText = File.ReadAllText(path);
            // Phase9 9-E E-6a: 判定の本体は ConnectionProfileSelector へ移った
            string selectorPath = SourceFileLocator.Find("DS4Windows/DS4Control/Services/ConnectionProfileSelector.cs");
            if (selectorPath == null) return;
            string text = File.ReadAllText(selectorPath);
            int first = text.IndexOf("_deviceStateService.IsFirstConnection(index, connectedMac)", StringComparison.Ordinal);
            int older = text.IndexOf("profileToApply = _profileRepository.OlderProfilePath[index];", StringComparison.Ordinal);
            int remembered = text.IndexOf("profileToApply = _deviceStateService.GetRememberedProfile(connectedMac);", StringComparison.Ordinal);
            int selected = text.IndexOf("profileToApply = _profileRepository.SelectedProfile[index];", StringComparison.Ordinal);
            Assert.True(first >= 0 && older > first && remembered > older && selected > remembered,
                "初回接続は Linked／OlderProfilePath、再接続はコントローラーの記憶（なければ SelectedProfile）");

            Assert.Contains("_deviceStateService.MarkConnected(index, connectedMac)", text);
            Assert.Contains("_deviceStateService.RememberProfile(connectedMac, profileToApply);", text);
            Assert.DoesNotContain("_deviceStateService.IsFirstConnection(index)", text);   // スロット単位の判定は使わない

            // 適用のたびに記憶する（こちらは ControlService に残っている）
            text = controlServiceText;
            Assert.Contains("_profileRepository.SelectedProfileChanged += RememberAppliedProfile;", text);
            Assert.Contains("_deviceStateService.RememberProfile(device.getMacAddress(), e.ProfileName);", text);
        }
    }
}
