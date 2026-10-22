using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>Phase9 UI 変更: 割り当ての説明文（Controls の一覧、出力ピッカーのボタン、SA の一覧で共通の書式）。</summary>
    public class Phase9AssignmentDescriberTests
    {
        private static string Name(X360Controls x) => x.ToString();

        private static DS4ControlSettings Setting(DS4ControlSettings.ActionType type, DS4KeyType keyType = DS4KeyType.None)
            => new DS4ControlSettings(DS4Controls.Cross) { actionType = type, keyType = keyType };

        [Fact]
        public void Key_ShowsTheKeyTheScanCodeAndTheMode()
        {
            Assert.Equal("W (Press)", AssignmentDescriber.ForKey(0x57, false, false));
            Assert.Equal("Q (SC) (Toggle)", AssignmentDescriber.ForKey(0x51, true, true));
        }

        [Fact]
        public void Default_ShowsTheDefaultOutput_AndTheToggle()
        {
            Assert.Equal("Default (A)", AssignmentDescriber.ForDefault("A"));
            Assert.Equal("Default", AssignmentDescriber.ForDefault(null));
            Assert.Equal("Default (A) (Toggle)", AssignmentDescriber.ForDefault("A", toggle: true));
        }

        [Fact]
        public void Macro_ShowsTheStepCountAndTheOptions()
        {
            Assert.Equal("Macro: 12 steps (Play once)", AssignmentDescriber.ForMacro(12, DS4KeyType.Macro, false, false, false));
            Assert.Equal("Macro: 3 steps (Repeat while held) (SC) (Run on release) (Synchronized) (Keep key state)",
                AssignmentDescriber.ForMacro(3, DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, true, true, true));
        }

        [Fact]
        public void ForControl_CoversEveryAssignmentType()
        {
            Assert.Equal("Default (Cross)", AssignmentDescriber.ForControl(Setting(DS4ControlSettings.ActionType.Default), Name, X360Controls.Cross, false, "Unassigned"));
            Assert.Equal("Unassigned", AssignmentDescriber.ForControl(Setting(DS4ControlSettings.ActionType.Default), Name, X360Controls.None, false, "Unassigned"));
            Assert.Equal("Unassigned", AssignmentDescriber.ForControl(Setting(DS4ControlSettings.ActionType.Default), Name, X360Controls.Cross, true, "Unassigned"));

            DS4ControlSettings button = Setting(DS4ControlSettings.ActionType.Button, DS4KeyType.Toggle);
            button.action.actionBtn = X360Controls.B;
            Assert.Equal("B (Toggle)", AssignmentDescriber.ForControl(button, Name, X360Controls.A, false, "Unassigned"));

            DS4ControlSettings unbound = Setting(DS4ControlSettings.ActionType.Button, DS4KeyType.Unbound);
            unbound.action.actionBtn = X360Controls.Unbound;
            Assert.Equal("Unbound", AssignmentDescriber.ForControl(unbound, Name, X360Controls.A, false, "Unassigned"));

            DS4ControlSettings macro = Setting(DS4ControlSettings.ActionType.Macro, DS4KeyType.Macro);
            macro.action.actionMacro = new[] { 65, 65, 66 };
            Assert.Equal("Macro: 3 steps (Play once)", AssignmentDescriber.ForControl(macro, Name, X360Controls.A, false, "Unassigned"));
        }

        [Fact]
        public void ForControl_AddsExtras_OnlyWhenSomethingIsSet()
        {
            DS4ControlSettings withExtras = Setting(DS4ControlSettings.ActionType.Default);
            withExtras.extras = "100,50,1,10,20,30,5,1,7";
            Assert.Equal("Default (Cross) + Extras", AssignmentDescriber.ForControl(withExtras, Name, X360Controls.Cross, false, "Unassigned"));

            DS4ControlSettings empty = Setting(DS4ControlSettings.ActionType.Default);
            empty.extras = "0,0,0,0,0,0,0,0,0";
            Assert.Equal("Default (Cross)", AssignmentDescriber.ForControl(empty, Name, X360Controls.Cross, false, "Unassigned"));
        }

        [Fact]
        public void SpecialItems_ShowTheirDetails()
        {
            Assert.Equal("Launch Program: notepad.exe (hold 1.0s)",
                AssignmentDescriber.ForSpecialItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = @"C:\Windows\notepad.exe", HoldSeconds = 1.0 }));
            Assert.Equal("Load Profile: Game (Auto untrigger)",
                AssignmentDescriber.ForSpecialItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "Game", AutomaticUntrigger = true }));
            Assert.Equal("Disconnect Controller", AssignmentDescriber.ForSpecialItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT }));
        }

        [Fact]
        public void TheSpecialActionList_UsesTheSameFormat()
        {
            var key = new SpecialAction("k", "Cross", "Key", "87") { keyType = DS4KeyType.Toggle };
            key.typeID = SpecialAction.ActionTypeId.Key;
            Assert.Equal("Key: W (Toggle)", AssignmentDescriber.ForSpecialAction(key, Name));

            var button = new SpecialAction("b", "Cross", "Button", "1") { typeID = SpecialAction.ActionTypeId.Button };
            button.details = ((int)X360Controls.B).ToString();
            button.KeyButtonSwitchMode = SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            Assert.Equal("Button: B (Toggle)", AssignmentDescriber.ForSpecialAction(button, Name));
        }
    }
}
