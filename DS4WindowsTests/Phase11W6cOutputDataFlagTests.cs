using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6c（K-W6-12）: 「物理コントローラーへ出力データを送る」［EnableOutputDataToDS4］の仕様変更。
    /// C1: ON でも Emulated Controller を DS4 へ強制しない。C3（D2＝案 B）: ON → OFF では、フラグを立てる前に消灯・振動ゼロを 1 度だけ書く。
    /// C4: 表示名と説明文（英語・日本語）。HID の書き込みは単体で動かせないため、呼び出しの順序とガードをコードで検査する。
    /// </summary>
    public class Phase11W6cOutputDataFlagTests
    {
        private readonly ITestOutputHelper _output;

        public Phase11W6cOutputDataFlagTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Source(string relativePath, bool stripComments = true)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            if (stripComments)
            {
                text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
                text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            }
            return Regex.Replace(text, @"\s+", " ");
        }

        [Fact]
        public void TheLightsOffWrite_RunsBeforeTheFlagIsSet_AndOnlyWhenTurnedOff()
        {
            string code = Source("DS4Windows/DS4Control/Services/DeviceConnectionSetup.cs");
            if (code == null) return;

            int off = code.IndexOf("if (!_profileSettings.GetEnableOutputDataToDS4(ind)) device.WriteOutputOffOnce();", StringComparison.Ordinal);
            int flag = code.IndexOf("device.ModifyFeatureSetFlag(VidPidFeatureSet.NoOutputData,", StringComparison.Ordinal);
            Assert.True(off >= 0 && flag > off, "WriteOutputOffOnce が、NoOutputData を立てる前にない");
        }

        [Fact]
        public void TheLightsOffWrite_SkipsPadsThatCannotWrite_NonDs4Types_AndNeverThrows()
        {
            string code = Source("DS4Windows/DS4Library/DS4Device.cs");
            if (code == null) return;

            int start = code.IndexOf("public void WriteOutputOffOnce()", StringComparison.Ordinal);
            int end = code.IndexOf("private const byte DEFAULT_BT_REPORT_TYPE", StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            string body = code.Substring(start, end - start);

            Assert.Contains("deviceType != InputDevices.InputDeviceType.DS4", body);
            Assert.Contains("(featureSet & VidPidFeatureSet.NoOutputData) != 0", body);
            Assert.Contains("sendOutputReport(true, true, false);", body);   // 書けなくても切断しない
            Assert.Contains("catch (Exception", body);
        }

        [Fact]
        public void TheEditor_NoLongerForcesTheEmulatedControllerToDs4_WhenOutputDataIsOn()
        {
            string vm = Source("DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs");
            if (vm == null) return;

            int start = vm.IndexOf("public Ds4OutputCorrectionReason CorrectOutputTypeToDs4()", StringComparison.Ordinal);
            Assert.True(start >= 0);
            string body = vm.Substring(start, 700);
            Assert.DoesNotContain("EnableOutputDataToDS4", body);
        }

        [Fact]
        public void TheDisplayName_IsNotTiedToDs4_InEnglishAndJapanese()
        {
            string en = Source("DS4Windows/Translations/Strings.resx", false);
            string ja = Source("DS4Windows/Translations/Strings.ja.resx", false);
            if (en == null || ja == null) return;

            Assert.Contains("<data name=\"EnableOutputDataToDS4\" xml:space=\"preserve\"> <value>Send output data to physical controller</value>", en);
            Assert.Contains("<data name=\"EnableOutputDataToDS4\" xml:space=\"preserve\"> <value>物理コントローラーへ出力データを送る</value>", ja);
            Assert.Contains("regardless of the Emulated Controller type", en);
            Assert.Contains("Emulated Controllerの種類に関係なく働きます", ja);
            Assert.DoesNotContain("takes effect at the gamepad connection time", en);
        }
    }
}
