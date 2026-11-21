using DS4Windows.Core.Model;
using System;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Output.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// OSC 送信を有効にしたときの NullReferenceException の回帰テスト。
    /// <c>UseOscSender</c> が先に true になり、送信側の生成（<c>ChangeOSCSenderStatus</c>）が終わる前に
    /// 入力スレッドが送信すると、アプリが未処理例外で終了していた（Step2 より前から存在した競合）。
    /// 送信側が未生成・破棄済みの間は、送信せずに通過することを固定する。
    /// OSC 系は <c>ControlService</c> から <c>OscService</c> へ移ったため、検査の対象も <c>OscService</c> に変えた
    /// （<c>ControlService.ChangeOSCSenderStatus</c> は薄い委譲として残っている）。
    /// </summary>
    public class ControlServiceOscSenderRaceTests
    {
        private static OscService CreateService(RecordingOscTransportFactory factory = null)
        {
            return new OscService(TestHost.GetService<IAppSettingsService>(), factory ?? new RecordingOscTransportFactory());
        }

        [Fact]
        public void PreMappingStep_WhenSenderNotCreatedYet_DoesNotThrow()
        {
            OscService service = CreateService();
            Assert.False(service.HasSender);

            var cState = new ControllerState { Battery = 50, Cross = true };
            var tempMapState = new ControllerState();

            // バッテリー値が異なるため、送信経路（SendOsc）を通る
            Exception ex = Record.Exception(() => service.PreMappingStep(0, cState, tempMapState));

            Assert.Null(ex);
        }

        [Fact]
        public void ChangeSenderStatus_Disable_WhenSenderNotCreated_DoesNotThrow()
        {
            OscService service = CreateService();
            Assert.False(service.HasSender);

            Exception ex = Record.Exception(() => service.ChangeSenderStatus(false));

            Assert.Null(ex);
            Assert.False(service.HasSender);
        }

        [Fact]
        public void ControlService_ChangeOSCSenderStatus_Disable_WhenSenderNotCreated_DoesNotThrow()
        {
            // 旧 ControlService.ChangeOSCSenderStatus は削除した。画面が使う IRuntimeFeatureSwitch を経由する
            TestHost.InitializeRuntime();
            var service = TestHost.GetService<DS4Windows.Processing.Abstractions.IRuntimeFeatureSwitch>();

            Exception ex = Record.Exception(() => service.ChangeOscSenderStatus(false));

            Assert.Null(ex);
        }
    }
}
