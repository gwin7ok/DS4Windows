using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W3（Issue #4 の 2）: 「画面サイズを保持」のチェック（<c>ProfileEditor.Keepsize</c>）の仕組みを固定する。
    /// - XAML の <c>IsChecked="{Binding Keepsize, Mode=TwoWay}"</c> は、ProfileEditor ではなく、メインウィンドウの DataContext
    ///   （<see cref="MainWindowsViewModel"/>）に対して評価され、そこに Keepsize が無いため、最初から解決していなかった（書き込み不能の get-only かどうか以前の問題）。
    ///   実際にチェックの状態を持つのは Click ハンドラー（<c>KeepSizeCheckBox_Click</c>）だけである。
    /// - よって、死んでいるバインディングを外し、Click ハンドラーを唯一の経路にする。保存・復元の分岐（Keepsize を読む側）は変えない。
    /// ProfileEditor は WPF の UserControl で単体駆動が困難なため、構成はソースの走査（XAML は XDocument）で検査する。
    /// ソースファイルが見つからない環境では、その検査を行わず合格とする。
    /// </summary>
    public class Phase11W3KeepsizeTests
    {
        private const string EditorXaml = "DS4Windows/UI/ProfileEditor.xaml";
        private const string EditorCode = "DS4Windows/UI/ProfileEditor.xaml.cs";
        private const string MainWindowCode = "DS4Windows/UI/MainWindow.xaml.cs";

        private readonly ITestOutputHelper _output;

        public Phase11W3KeepsizeTests(ITestOutputHelper output)
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

        /// <summary>
        /// 証拠: メインウィンドウの DataContext に対する「Keepsize」の TwoWay バインディングは、解決しない（PathError）。
        /// ユーザーがチェックを操作しても、例外にならず、何も書き込まれない。
        /// </summary>
        [Fact]
        public void AKeepsizeBinding_AgainstTheMainWindowDataContext_NeverResolves()
        {
            Assert.Null(typeof(MainWindowsViewModel).GetProperty("Keepsize"));

            BindingStatus status = BindingStatus.Active;
            bool? checkedAfterToggle = null;
            RunOnSta(() =>
            {
                // コンストラクターを通さずに生成する（依存を用意せずに、型だけを DataContext に使う）
                object dataContext = RuntimeHelpers.GetUninitializedObject(typeof(MainWindowsViewModel));
                var checkBox = new CheckBox { DataContext = dataContext };
                BindingOperations.SetBinding(checkBox, CheckBox.IsCheckedProperty, new Binding("Keepsize") { Mode = BindingMode.TwoWay });
                status = BindingOperations.GetBindingExpression(checkBox, CheckBox.IsCheckedProperty).Status;

                checkBox.IsChecked = true;   // ユーザーの操作に相当。書き戻し先が無くても、例外にならない
                checkedAfterToggle = checkBox.IsChecked;
            });

            Assert.Equal(BindingStatus.PathError, status);
            Assert.True(checkedAfterToggle);
        }

        [Fact]
        public void TheKeepSizeCheckBox_IsDrivenByItsClickHandlerOnly_NotByABinding()
        {
            XDocument xaml = XamlTestHelper.Load(EditorXaml);
            if (xaml == null)
            {
                _output.WriteLine(EditorXaml + " が見つからないため検査を省略した。");
                return;
            }

            XElement checkBox = XamlTestHelper.FindByName(xaml, "keepSizeCheckBox");
            Assert.Equal("CheckBox", checkBox.Name.LocalName);
            Assert.Equal("KeepSizeCheckBox_Click", XamlTestHelper.Attr(checkBox, "Click"));

            // 状態を持つのは Click ハンドラーだけ。IsChecked のバインディング（解決しない Keepsize）を置かない
            Assert.Null(XamlTestHelper.Attr(checkBox, "IsChecked"));
            Assert.DoesNotContain(XamlTestHelper.AllAttributeValues(xaml.Root), v => v.Contains("Keepsize"));
        }

        [Fact]
        public void TheClickHandler_StoresTheCheckBoxState_AndKeepsizeIsReadOnlyToTheOutside()
        {
            string code = ReadNormalizedSource(EditorCode);
            if (code == null) return;

            // Click ハンドラー: チェックの状態を、そのままフィールドへ
            Assert.Matches(@"private void KeepSizeCheckBox_Click\(object sender, RoutedEventArgs e\) \{ var checkBox = sender as System\.Windows\.Controls\.CheckBox; keepsize = checkBox != null && checkBox\.IsChecked == true; \}", code);

            // 外へは読み取り専用（読む側は MainWindow。書き込みの経路は Click ハンドラーだけ）
            Assert.Matches(@"public bool Keepsize \{ get => keepsize; \}", code);
        }

        [Fact]
        public void TheLayoutIsSavedAndTheWindowSizeIsKept_OnlyThroughKeepsize()
        {
            string editor = ReadNormalizedSource(EditorCode);
            string main = ReadNormalizedSource(MainWindowCode);
            if (editor == null || main == null) return;

            // 編集画面: 閉じるときに、チェックが ON のときだけ、仕切り・列の幅を保存する（Close と ProfileEditor_Closed）
            Assert.Equal(2, Regex.Matches(editor, @"if \(keepsize\) \{ SaveSplitterAndColumnWidths\(\); \}").Count);

            // メインウィンドウ: サイズ・位置の保存（SizeChanged・LocationChanged）と、閉じたときの復元の分岐
            Assert.Equal(2, Regex.Matches(main, @"editor != null && editor\.Keepsize && !IsInitialShow").Count);
            Assert.Single(Regex.Matches(main, @"if \(!editor\.Keepsize\) \{ this\.Width = oldSize\.Width; this\.Height = oldSize\.Height; \}"));
            Assert.Equal(3, Regex.Matches(main, @"editor\.Keepsize").Count);
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                if (failure is Xunit.Sdk.XunitException) throw failure;
                throw new InvalidOperationException("STA スレッドで失敗: " + failure.Message, failure);
            }
        }
    }
}
