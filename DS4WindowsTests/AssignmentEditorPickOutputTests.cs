#nullable enable
using System;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 割り当て編集の出力ピッカー呼び出し（<see cref="AssignmentEditorSession.PickOutput"/>）を、窓を偽物（<see cref="IBindingDialog"/>）に差し替えて確かめる。
    /// Controls のモードは、ボタンと Extras を載せた設定を Control のモードで渡し、結果の Extras を編集用のコピーへ取り込み、Default／Unbound を選んだときだけ
    /// 種別の一覧の添字を返す。SA のモードは SpecialAction のモードで渡し、一覧は切り替えない。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AssignmentEditorPickOutputTests
    {
        private const string SomeExtras = "10,20,1,255,0,0,1,0,25";

        private sealed class FakeDialog : IBindingDialog
        {
            public Action<DS4ControlSettings>? Pick;
            public Func<DS4ControlSettings>? SettingsOf;
            public int Shown;
            public bool DefaultPicked { get; set; }

            public void ShowModal()
            {
                Shown++;
                Pick?.Invoke(SettingsOf!());
            }
        }

        private sealed class Rig
        {
            public readonly FakeDialog Dialog = new FakeDialog();
            public readonly RecordingProxy Factory;
            public readonly AssignmentEditorSession Session;
            public readonly DS4ControlSettings Original;

            public Rig(bool controlMode, DS4ControlSettings.ActionType type = DS4ControlSettings.ActionType.Button, string? extras = SomeExtras, int targetDevice = -1)
            {
                var factory = RecordingProxy.Create<IEditorWindowFactory>();
                Factory = RecordingProxy.Of(factory);
                Factory.Returns["CreateBindingDialog"] = Dialog;
                Dialog.SettingsOf = () => (DS4ControlSettings)Factory.ArgsOf("CreateBindingDialog").Last()[1]!;

                Original = new DS4ControlSettings(DS4Controls.Circle) { actionType = type, extras = extras };
                var profileList = new ProfileList(TestHost.GetService<IPathService>());
                Session = new AssignmentEditorSession(ControllerLimits.TestProfileIndex, controlMode ? Original : null, null, profileList, targetDevice,
                    factory, TestHost.GetService<IProfileEditViewModelFactory>(), TestHost.GetService<IActionViewModelFactory>(),
                    TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<IProfileSettingsService>(),
                    TestHost.GetService<IEnvironmentService>(), TestHost.GetService<IWindowSystem>());
            }

            public object?[] LastCall => Factory.ArgsOf("CreateBindingDialog").Last();
        }

        [Fact]
        public void TheControlMode_PassesTheButtonAndTheExtras_InTheControlMode()
        {
            var rig = new Rig(controlMode: true);

            rig.Session.PickOutput();

            Assert.Equal(1, rig.Dialog.Shown);
            var settings = (DS4ControlSettings)rig.LastCall[1]!;
            Assert.Equal(DS4Controls.Circle, settings.control);
            Assert.Equal(SomeExtras, settings.extras);
            Assert.Equal(BindingWindow.ExposeMode.Control, rig.LastCall[3]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ThePicker_ReceivesTheTargetDeviceAndTheSlot_InBothModes(bool controlMode)
        {
            var rig = new Rig(controlMode, targetDevice: 0);

            rig.Session.PickOutput();

            Assert.Equal(ControllerLimits.TestProfileIndex, rig.LastCall[0]);   // 設定を読み書きする作業スロット
            Assert.Equal(0, rig.LastCall[2]);   // ランブル・ライトバー・マクロ記録が使う実機のスロット
        }

        [Fact]
        public void TheControlMode_TakesTheExtrasBack_IntoTheWorkingCopy_AndAppliesThemOnlyOnSave()
        {
            var rig = new Rig(controlMode: true);
            rig.Dialog.Pick = s => s.extras = "1,2,3,4,5,6,7,8,9";

            rig.Session.PickOutput();
            Assert.Equal(SomeExtras, rig.Original.extras);   // 元の設定は、Save まで変わらない

            Assert.True(rig.Session.TrySaveControl());
            Assert.Equal("1,2,3,4,5,6,7,8,9", rig.Original.extras);
        }

        [Fact]
        public void PickingDefault_SwitchesTheTypeListToDefault()
        {
            var rig = new Rig(controlMode: true);
            rig.Dialog.Pick = s => { s.actionType = DS4ControlSettings.ActionType.Default; };
            rig.Dialog.DefaultPicked = true;

            int? index = rig.Session.PickOutput();

            Assert.Equal(rig.Session.ActionVM.TypeCatalog.DefaultIndex, index);
        }

        [Fact]
        public void ADefaultButton_ClosedWithoutPicking_KeepsTheTypeListAsItIs()
        {
            var rig = new Rig(controlMode: true, DS4ControlSettings.ActionType.Default);   // × で閉じた（DefaultPicked は false）

            Assert.Null(rig.Session.PickOutput());
        }

        [Fact]
        public void ADefaultButton_WhenDefaultIsPickedAgain_SwitchesTheList()
        {
            var rig = new Rig(controlMode: true, DS4ControlSettings.ActionType.Default);
            rig.Dialog.DefaultPicked = true;

            Assert.Equal(rig.Session.ActionVM.TypeCatalog.DefaultIndex, rig.Session.PickOutput());
        }

        [Fact]
        public void ANonDefaultButton_ChangedToDefaultInThePicker_SwitchesTheList()
        {
            var rig = new Rig(controlMode: true);
            rig.Dialog.Pick = s => s.actionType = DS4ControlSettings.ActionType.Default;

            Assert.Equal(rig.Session.ActionVM.TypeCatalog.DefaultIndex, rig.Session.PickOutput());
        }

        [Fact]
        public void PickingNothing_LeavesTheTypeListAlone()
        {
            var rig = new Rig(controlMode: true);   // × で閉じた（設定は変わらない）

            Assert.Null(rig.Session.PickOutput());
        }

        [Fact]
        public void TheSpecialActionMode_UsesTheSpecialActionMode_AndNeverSwitchesTheList()
        {
            var rig = new Rig(controlMode: false);
            rig.Dialog.Pick = s => s.extras = SomeExtras;

            int? index = rig.Session.PickOutput();

            Assert.Null(index);
            Assert.Equal(BindingWindow.ExposeMode.SpecialAction, rig.LastCall[3]);
            Assert.Equal(1, rig.Dialog.Shown);
        }
    }
}
