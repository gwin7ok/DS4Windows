using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 Steering 削除 W1（Phase9-SteeringWheel-Removal-Plan.md Q3＝A）: 廃止した割り当て種別「Steering Wheel Calibrate」（<c>SASteeringWheelEmulationCalibrate</c>）が、
    /// 古い Actions.xml・プロファイルに残っていても、読み込み時に取り除かれ（ほかの項目は壊れず）、<c>[DI]</c> 接頭辞付きのログで通知されること。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase9SteeringW1LegacyAssignmentTests
    {
        private sealed class LogCapture : IDisposable
        {
            public readonly List<string> Lines = new List<string>();
            private readonly EventHandler<DebugEventArgs> _sink;

            public LogCapture()
            {
                _sink = (sender, e) => { lock (Lines) Lines.Add(e.Data); };
                AppLogger.GuiLog += _sink;
            }

            public void Dispose() => AppLogger.GuiLog -= _sink;
        }

        [Fact]
        public void ALegacySteeringAction_InActionsXml_IsDropped_TheOthersLoad_AndTheLogSaysSo()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore saved = PersistenceHarness.NewStore();
            string xml = PersistenceHarness.SaveActions(ctx, saved);
            Assert.Contains("<Type>DisconnectBT</Type>", xml);

            // Golden_Disconnect を、廃止した種別に書き換える（古い Actions.xml の再現）
            string legacy = xml.Replace("<Type>DisconnectBT</Type>", "<Type>SASteeringWheelEmulationCalibrate</Type>");
            Assert.NotEqual(xml, legacy);

            using var log = new LogCapture();
            BackingStore loaded = PersistenceHarness.LoadActions(ctx, legacy);

            Assert.DoesNotContain(loaded.actions, a => a.name == "Golden_Disconnect");
            Assert.Contains(loaded.actions, a => a.name == "Golden_Key");
            Assert.Contains(loaded.actions, a => a.name == "Golden_Macro");
            Assert.Equal(PersistenceHarness.LoadActions(ctx, xml).actions.Count - 1, loaded.actions.Count);
            Assert.Contains(log.Lines, l => l.StartsWith("[DI]") && l.Contains("SASteeringWheelEmulationCalibrate") && l.Contains("Golden_Disconnect"));
        }

        [Fact]
        public void ALegacySteeringAssignment_InAProfilesControls_IsDropped_AndTheLogSaysSo()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.R1).Reset();
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.L1).Reset();
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.R1).actionType = DS4ControlSettings.ActionType.Special;
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.R1).special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT };
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.L1).actionType = DS4ControlSettings.ActionType.Special;
            store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.L1).special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.GyroCalibrate };

            string xml = PersistenceHarness.SaveProfile(ctx, store, "SteeringLegacy");
            Assert.Contains("Type=\"DisconnectBT\"", xml);
            string legacy = xml.Replace("Type=\"DisconnectBT\"", "Type=\"SASteeringWheelEmulationCalibrate\"");

            using var log = new LogCapture();
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, legacy, "SteeringLegacyLoad");

            // 廃止した種別の割り当ては取り除かれ（既定に戻る）、ほかの割り当ては残る
            Assert.NotEqual(DS4ControlSettings.ActionType.Special, loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.R1).actionType);
            Assert.Null(loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.R1).special);
            Assert.Equal(SpecialAction.ActionTypeId.GyroCalibrate, loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.L1).special.Type);
            Assert.Contains(log.Lines, l => l.StartsWith("[DI]") && l.Contains("SASteeringWheelEmulationCalibrate"));
        }

        [Fact]
        public void TheRemovedType_IsNoLongerAnActionTypeId_AndAnUnknownTypeStringIsNone()
        {
            Assert.DoesNotContain("SASteeringWheelEmulationCalibrate", Enum.GetNames(typeof(SpecialAction.ActionTypeId)));

            // 直接この型の文字列で作ると、種別が不明（None）の項目になる（XboxGameDVR の削除と同じ扱い）
            var sa = new SpecialAction("w", "Cross", "SASteeringWheelEmulationCalibrate", "", 1.0);
            Assert.Equal(SpecialAction.ActionTypeId.None, sa.typeID);
        }
    }
}
