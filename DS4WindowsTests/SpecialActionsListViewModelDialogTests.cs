using System.Linq;
using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// 特殊アクション一覧の「削除の確認」「複製できない通知」「保存後の追加・差し替え」が、
    /// 窓（MessageBox）なしの ViewModel だけで動くこと（偽の <c>IDialogService</c> で確かめる）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class SpecialActionsListViewModelDialogTests
    {
        private static SpecialActionsListViewModel CreateVm(FakeDialogService dialogs) =>
            new SpecialActionsListViewModel(0, TestRepositoryFactory.CreateSpecialActionRepository(),
                TestRepositoryFactory.CreateProfileRepository(), dialogs);

        /// <summary>Actions.xml の保存先を一時ディレクトリへ向け、テスト用のアクションを登録する。終了時に元へ戻す。</summary>
        private sealed class ActionsScope : System.IDisposable
        {
            private readonly List<SpecialAction> _original;
            private readonly string _originalPath;
            private readonly string _dir;

            public ActionsScope(params string[] names)
            {
                _dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SpActDlg_" + System.Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(_dir);
                _originalPath = TestGlobals.Store.m_Actions;
                TestGlobals.Store.m_Actions = System.IO.Path.Combine(_dir, "Actions.xml");
                var list = TestGlobals.Store.actions;
                _original = new List<SpecialAction>(list);
                list.Clear();
                foreach (string name in names) list.Add(new SpecialAction(name, "Cross", "Program", "C:/a.exe"));
                TestRepositoryFactory.CreateActionsXml().SaveActions();
            }

            public void Dispose()
            {
                TestGlobals.Store.m_Actions = _originalPath;
                var list = TestGlobals.Store.actions;
                list.Clear();
                list.AddRange(_original);
                try { System.IO.Directory.Delete(_dir, true); } catch { }
            }
        }

        private static SpecialActionItem AddMissingItem(SpecialActionsListViewModel vm, string name)
        {
            vm.AddSavedAction(name);
            SpecialActionItem item = vm.ActionCol.Last();
            vm.SpecialActionIndex = vm.ActionCol.Count - 1;
            vm.CurrentSpecialActionItem = item;
            return item;
        }

        [Fact]
        public void RemoveSelectedWithConfirm_WhenDeclined_KeepsTheItemAndReturnsNull()
        {
            var dialogs = new FakeDialogService { ConfirmAnswer = false };
            using var scope = new ActionsScope("NoSuchAction_A");
            var vm = CreateVm(dialogs);
            AddMissingItem(vm, "NoSuchAction_A");
            int before = vm.ActionCol.Count;

            Assert.Null(vm.RemoveSelectedWithConfirm());
            Assert.Equal(before, vm.ActionCol.Count);
            Assert.Single(dialogs.Confirmations);
            Assert.Contains("NoSuchAction_A", dialogs.Confirmations[0]);
        }

        [Fact]
        public void RemoveSelectedWithConfirm_WhenAccepted_RemovesAndReturnsName()
        {
            var dialogs = new FakeDialogService { ConfirmAnswer = true };
            using var scope = new ActionsScope("NoSuchAction_B");
            var vm = CreateVm(dialogs);
            AddMissingItem(vm, "NoSuchAction_B");
            int before = vm.ActionCol.Count;

            Assert.Equal("NoSuchAction_B", vm.RemoveSelectedWithConfirm());
            Assert.Equal(before - 1, vm.ActionCol.Count);
        }

        [Fact]
        public void RemoveSelectedWithConfirm_WithoutSelection_AsksNothing()
        {
            var dialogs = new FakeDialogService();
            var vm = CreateVm(dialogs);

            Assert.Null(vm.RemoveSelectedWithConfirm());
            Assert.Empty(dialogs.Confirmations);
        }

        [Fact]
        public void DuplicateSelectedWithNotice_ForMissingItem_ShowsNotice()
        {
            var dialogs = new FakeDialogService();
            var vm = CreateVm(dialogs);
            // Actions.xml に無い名前は、LoadActions と同じく名前つきの項目（IsMissing）で一覧へ加わる
            var missing = vm.CreateActionItem(vm.GetActionOrPlaceholder("NoSuchAction_C"), "NoSuchAction_C");
            vm.ActionCol.Add(missing);
            vm.SpecialActionIndex = vm.ActionCol.Count - 1;
            vm.CurrentSpecialActionItem = missing;

            Assert.False(vm.DuplicateSelectedWithNotice());
            Assert.Single(dialogs.Messages);
            Assert.Contains("NoSuchAction_C", dialogs.Messages[0]);
        }

        [Fact]
        public void ReplaceEditedAction_KeepsPositionAndActiveState()
        {
            using var scope = new ActionsScope("NoSuchAction_D", "NoSuchAction_E");
            var vm = CreateVm(new FakeDialogService());
            SpecialActionItem old = AddMissingItem(vm, "NoSuchAction_D");
            old.Active = true;
            int idx = vm.ActionCol.IndexOf(old);

            vm.ReplaceEditedAction(old, idx, "NoSuchAction_E");

            Assert.Equal("NoSuchAction_E", vm.ActionCol[idx].ActionName);
            Assert.True(vm.ActionCol[idx].Active);
            Assert.Equal(idx, vm.ActionCol[idx].Index);
        }
    }
}
