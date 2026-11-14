using System;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels.Util;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 画面の土台（コマンド、UI スレッドの窓口、ダイアログの窓口）の守り。
    /// コマンドは WPF なしで動くこと、窓口の実装は DI で解決できること。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class RelayCommandTests
    {
        [Fact]
        public void Execute_RunsTheAction_OnlyWhenItCanExecute()
        {
            int count = 0;
            bool allowed = false;
            var command = new RelayCommand(() => count++, () => allowed);

            command.Execute(null);
            Assert.Equal(0, count);
            Assert.False(command.CanExecute(null));

            allowed = true;
            command.Execute(null);
            Assert.Equal(1, count);
        }

        [Fact]
        public void RaiseCanExecuteChanged_NotifiesTheSubscribers()
        {
            var command = new RelayCommand(() => { });
            int raised = 0;
            command.CanExecuteChanged += (s, e) => raised++;

            command.RaiseCanExecuteChanged();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void TheParameter_IsPassedToTheAction()
        {
            object received = null;
            var command = new RelayCommand(p => received = p, p => p != null);

            command.Execute(null);
            Assert.Null(received);
            command.Execute("x");
            Assert.Equal("x", received);
        }

        [Fact]
        public void ANullAction_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand((Action)null));
        }

        [Fact]
        public void TheFakeDialog_RecordsTheCallsAndReturnsTheSetAnswers()
        {
            var dialog = new FakeDialogService { ConfirmAnswer = false, OpenFileAnswer = "a.xml" };
            IDialogService service = dialog;

            service.ShowMessage("hello");
            Assert.False(service.Confirm("sure?"));
            Assert.Equal("a.xml", service.PickOpenFile(new FileDialogOptions { Title = "t" }));
            Assert.Null(service.PickSaveFile(null));

            Assert.Equal(new[] { "hello" }, dialog.Messages);
            Assert.Equal(new[] { "sure?" }, dialog.Confirmations);
            Assert.Equal("t", dialog.LastOpenOptions.Title);
        }

        [Fact]
        public void TheHost_ResolvesTheFoundationServices()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            Assert.IsType<WpfUiDispatcher>(TestHost.GetService<IUiDispatcher>());
            Assert.IsType<WpfDialogService>(TestHost.GetService<IDialogService>());
        }
    }
}
