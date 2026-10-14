using System.Collections.Generic;
using System.Linq;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3b-2: Actions.xml に無く、プロファイルの XML にだけ残っているアクション名が、
    /// スペシャルアクション一覧に「トリガー・アクション欄が空のグレーの項目」として表示されること
    /// （36d124a6 のリファクタで失われていた 40011c12 の動作の復活）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class SpecialActionsListViewModelMissingActionTests
    {
        private static SpecialActionsListViewModel CreateVm() =>
            new SpecialActionsListViewModel(0, new SpecialActionRepository(), new ProfileRepository());

        /// <summary>Global.store のアクション一覧を、テスト用の内容へ差し替え、終了時に戻す</summary>
        private sealed class ActionsScope : System.IDisposable
        {
            private readonly List<SpecialAction> _original;

            public ActionsScope(params string[] names)
            {
                var list = Global.GetActions();
                _original = new List<SpecialAction>(list);
                list.Clear();
                foreach (string name in names)
                {
                    list.Add(new SpecialAction(name, "Cross", "Program", "C:/a.exe"));
                }
            }

            public void Dispose()
            {
                var list = Global.GetActions();
                list.Clear();
                list.AddRange(_original);
            }
        }

        [Fact]
        public void LoadActions_ShowsProfileOnlyNames_AsMissingGrayItems()
        {
            using var scope = new ActionsScope("ActA", "ActB");
            var vm = CreateVm();

            vm.LoadActions(false, "ActA/Gone");

            Assert.Equal(3, vm.ActionCol.Count);
            var actA = vm.ActionCol.Single(i => i.ActionName == "ActA");
            var actB = vm.ActionCol.Single(i => i.ActionName == "ActB");
            var gone = vm.ActionCol.Single(i => i.ActionName == "Gone");

            Assert.False(actA.IsMissing);
            Assert.True(actA.Active);
            Assert.False(actB.IsMissing);
            Assert.False(actB.Active);

            Assert.True(gone.IsMissing);
            Assert.True(gone.Active);
            Assert.NotNull(gone.SpecialAction);
            Assert.Equal(string.Empty, gone.SpecialAction.controls);
        }

        [Fact]
        public void LoadActions_DoesNotDuplicateExistingNames_IgnoringCase()
        {
            using var scope = new ActionsScope("ActA");
            var vm = CreateVm();

            vm.LoadActions(false, "acta");

            Assert.Single(vm.ActionCol);
            Assert.False(vm.ActionCol[0].IsMissing);
            Assert.True(vm.ActionCol[0].Active);
        }

        [Fact]
        public void LoadActions_MissingItems_CanBeUncheckedToRemoveFromProfileActions()
        {
            using var scope = new ActionsScope("ActA");
            var vm = CreateVm();

            vm.LoadActions(false, "ActA/Gone");

            // 読み込み直後は、プロファイルの内容をそのまま保持する（チェックしたままなら、保存してもプロファイルに残る）
            Assert.Equal("ActA/Gone", vm.ProfileActions);
            Assert.Equal(new[] { "ActA", "Gone" }, vm.GetEnabledActionNames().ToArray());

            // チェックを外せば、保存対象（有効なアクション名）と同期文字列から外れる
            vm.ActionCol.Single(i => i.ActionName == "Gone").Active = false;
            Assert.Equal(new[] { "ActA" }, vm.GetEnabledActionNames().ToArray());
            Assert.Equal("ActA", vm.ProfileActions);
        }
    }
}
