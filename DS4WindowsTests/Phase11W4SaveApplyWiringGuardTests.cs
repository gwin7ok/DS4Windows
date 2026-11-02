using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W4（Issue #4 の 1）: 保存・適用の合流点（<c>ProfileEditor.ExecuteSaveOrApply</c>）が、DS4 への補正を行い、
    /// 保存のときだけ、サービスの再起動を求める配線を固定する。
    /// - 補正は、保存する前（<c>SaveProfile</c> の前）に行う。名前の検査で止まる場合は、何も変えない。補正したら、画面の選択（outConTypeCombo）も DS4 にそろえる
    /// - <c>EnableOutputDataToDS4</c> の補正のログ文言は、旧 <c>SetLateProperties</c> のまま（§2.3）
    /// - 再起動は、旧 <c>SaveBtn_Click</c> と同じく、保存のときだけ（適用は、編集画面を開いたまま、実機を作り直さない）。
    ///   編集画面は <c>ServiceRestartRequested</c> を出すだけで、<c>Application.Current.MainWindow</c> を触らない（Pure DI）。
    ///   再起動は、MainWindow が、サービスが動いているときだけ、停止→開始を 1 つの処理で行う
    ///   （旧コードは、<c>ChangeService</c>［停止／開始の切り替え］を 2 回続けて呼んでおり、1 回目の完了を待たなかった）
    /// ProfileEditor・MainWindow は WPF の画面で単体駆動が困難なため、ソースの走査で担保する。ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase11W4SaveApplyWiringGuardTests
    {
        private const string EditorCode = "DS4Windows/DS4Forms/ProfileEditor.xaml.cs";
        private const string MainWindowCode = "DS4Windows/DS4Forms/MainWindow.xaml.cs";

        private readonly ITestOutputHelper _output;

        public Phase11W4SaveApplyWiringGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメントを除き、連続する空白を 1 つにしたソース（整形に依存しない照合のため）</summary>
        private string ReadNormalizedSource(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return Regex.Replace(text, @"\s+", " ");
        }

        /// <summary>宣言の先頭（marker）から、対応する閉じ波括弧までのメソッド全体</summary>
        private static string ExtractMethod(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, marker + " が見つからない（メソッド名・修飾子の変更？ガードの見直しが必要）");
            int open = text.IndexOf('{', start);
            Assert.True(open >= 0, marker + " の本体が見つからない");
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text.Substring(start, i - start + 1);
            }

            throw new Xunit.Sdk.XunitException(marker + " の閉じ波括弧が見つからない");
        }

        [Fact]
        public void ExecuteSaveOrApply_CorrectsTheOutputType_AfterTheNameCheckAndBeforeSaving()
        {
            string code = ReadNormalizedSource(EditorCode);
            if (code == null) return;

            string method = ExtractMethod(code, "private bool ExecuteSaveOrApply(bool isApply = false)");

            int nameCheck = method.IndexOf("return false; }", StringComparison.Ordinal);
            int correction = method.IndexOf("profileSettingsVM.CorrectOutputTypeToDs4()", StringComparison.Ordinal);
            int save = method.IndexOf("profileSettingsVM.SaveProfile(profileName)", StringComparison.Ordinal);

            Assert.True(nameCheck >= 0, "名前の検査（return false）が見つからない");
            Assert.True(correction > nameCheck, "補正が、名前の検査より前にある（検査で止まる場合に、何も変えないこと）");
            Assert.True(save > correction, "補正が、保存（SaveProfile）より後にある（補正後の値を保存すること）");

            // 補正したら、画面の選択も DS4 にそろえる（SelectionChanged が、設定・マッピング一覧の種類を更新する）
            Assert.Contains("outConTypeCombo.SelectedIndex = 1;", method);
        }

        [Fact]
        public void TheEnableOutputDataToDs4Correction_IsRemoved()
        {
            string code = ReadNormalizedSource(EditorCode);
            if (code == null) return;

            // Phase11 W6c（K-W6-12）: 補正 (c) を廃止した。補正の GUI ログも出さない
            Assert.DoesNotContain("EnableOutputDataToDS4 が有効なため", code);
            Assert.DoesNotContain("Ds4OutputCorrectionReason.EnableOutputDataToDS4", code);
        }

        [Fact]
        public void TheRestart_IsRequestedOnSaveOnly_ThroughAnEvent()
        {
            string code = ReadNormalizedSource(EditorCode);
            if (code == null) return;

            Assert.Contains("public event EventHandler ServiceRestartRequested;", code);

            // 保存: 成功したら、再起動を求めてから閉じる。適用: 求めない
            string save = ExtractMethod(code, "private void SaveBtn_Click(object sender, RoutedEventArgs e)");
            Assert.Contains("if (ExecuteSaveOrApply(isApply: false)) { RequestServiceRestartIfNeeded(); Close(); }", save);

            string apply = ExtractMethod(code, "private void ApplyBtn_Click(object sender, RoutedEventArgs e)");
            Assert.DoesNotContain("RequestServiceRestartIfNeeded", apply);

            // 設定が変わったときだけ求め、求めたあとは変更の印を戻す（くり返さない）
            string request = ExtractMethod(code, "private void RequestServiceRestartIfNeeded()");
            Assert.Contains("if (!profileSettingsVM.HasUseDs3PitchRollSimChanged) { return; }", request);
            Assert.Contains("ServiceRestartRequested?.Invoke(this, EventArgs.Empty);", request);
            Assert.Contains("profileSettingsVM.AcknowledgeUseDs3PitchRollSimChange();", request);

            // 旧コードの、Application.Current.MainWindow を取り出して ChangeService を直接呼ぶ形を、戻さない
            Assert.DoesNotContain("ChangeService", code);
            Assert.DoesNotContain("Application.Current.MainWindow).ChangeService", code);
        }

        [Fact]
        public void TheMainWindow_RestartsTheServiceInOneStopStartStep_OnlyWhileItIsRunning()
        {
            string code = ReadNormalizedSource(MainWindowCode);
            if (code == null) return;

            Assert.Contains("editor.ServiceRestartRequested += Editor_ServiceRestartRequested;", code);

            string handler = ExtractMethod(code, "private void Editor_ServiceRestartRequested(object sender, EventArgs e)");
            Assert.Contains("RestartService();", handler);

            string restart = ExtractMethod(code, "private async void RestartService()");
            // 止まっているときは何もしない（ユーザーが止めたサービスを、勝手に開始しない。設定は、次の開始で効く）
            int guard = restart.IndexOf("if (!service.running) { return; }", StringComparison.Ordinal);
            int disable = restart.IndexOf("StartStopBtn.IsEnabled = false;", StringComparison.Ordinal);
            Assert.True(guard >= 0, "サービスが止まっているときの早期終了がない");
            Assert.True(disable > guard, "ボタンを無効にするのは、早期終了のあと（戻らないと、ボタンが有効に戻らない）");

            // 停止→開始を、1 つの Task.Run の中で、順に行う（切り替えを 2 回呼ばない）
            int stop = restart.IndexOf("service.Stop(immediateUnplug: true);", StringComparison.Ordinal);
            int start = restart.IndexOf("service.Start();", StringComparison.Ordinal);
            Assert.True(stop >= 0 && start > stop, "停止→開始の順になっていない");
            Assert.DoesNotContain("ChangeService()", restart);
        }
    }
}
