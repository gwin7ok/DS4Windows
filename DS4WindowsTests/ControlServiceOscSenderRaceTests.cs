using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-3c（K9-1）: OSC 送信を有効にしたときの NullReferenceException の回帰テスト。
    /// <c>UseOscSender</c> が先に true になり、送信側の生成（<c>ChangeOSCSenderStatus</c>）が終わる前に
    /// 入力スレッドが送信すると、アプリが未処理例外で終了していた（Step2 より前から存在した競合）。
    /// 送信側が未生成・破棄済みの間は、送信せずに通過することを固定する。
    /// Phase9 9-E E-1: OSC 系は <c>ControlService</c> から <c>OscService</c> へ移ったため、検査の対象も <c>OscService</c> に変えた
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

            var cState = new DS4State { Battery = 50, Cross = true };
            var tempMapState = new DS4State();

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
            ControlService service = ControlServiceTestFactory.Create();

            Exception ex = Record.Exception(() => service.ChangeOSCSenderStatus(false));

            Assert.Null(ex);
        }

        [Fact]
        public void OscServiceSource_HasNoDirectSenderSend_AndControlServiceHasNoSendOsc()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/OscService.cs");
            if (path == null) return;

            string text = StripComments(File.ReadAllText(path));

            // 送信はすべて SendOsc（null 安全）経由にする
            Assert.DoesNotContain("_sender.Send(", text);
            Assert.Contains("private void SendOsc(string address, object value)", text);

            string controlPath = SourceFileLocator.Find("DS4Windows/DS4Control/ControlService.cs");
            if (controlPath == null) return;
            string control = StripComments(File.ReadAllText(controlPath));
            Assert.DoesNotContain("SendOsc(", control);
            Assert.DoesNotContain("oscSender", control);
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }
    }
}
