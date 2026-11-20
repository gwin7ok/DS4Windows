using System;
using System.IO;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// 設定ファイルの保存・読込を呼ぶ窓口（ゴールデンテスト用）。
    /// 切り出し（W2 以降）で永続化の実装が <c>BackingStore</c> から新しい型へ移るたびに、ここだけを付け替える。
    /// ゴールデンテストの本体（入力・期待値・照合）は変えない。
    /// </summary>
    internal static class PersistenceHarness
    {
        /// <summary>プロファイルの保存・読込・プリセットに使うスロット（編集用の作業スロット。ControlService の状態に触れない）。</summary>
        public const int Slot = ControllerLimits.TestProfileIndex;

        // 旧 Global.ProfileSettingsServiceInstance（出力 KBM のマッピングを設定した共有の instance）を、新しい BackingStore も使う
        // （プロファイルの読込・保存が、一時プロファイル・DInput のみ・タッチパッドの配列をこのサービスから読むため）
        public static BackingStore NewStore()
        {
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            store.ProfileSettings = TestGlobals.ProfileSettingsServiceInstance;
            return store;
        }

        // ---- プロファイル----

        public static string SaveProfile(PersistenceGoldenContext ctx, BackingStore store, string name)
        {
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).SaveProfile(Slot, name), "プロファイルの保存に失敗した");
            return File.ReadAllText(Path.Combine(ctx.AppData, "Profiles", name + ".xml"));
        }

        public static BackingStore LoadProfile(PersistenceGoldenContext ctx, string xml, string name)
        {
            string path = Path.Combine(ctx.AppData, "Profiles", name + ".xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).LoadProfile(Slot, ctx.LoadTarget, path, false, false), "プロファイルの読込に失敗した");
            return store;
        }

        // ---- アプリ設定（Profiles.xml）----

        public static string SaveAppSettings(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "Profiles.xml");
            store.m_Profile = path;
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(store).Save(), "アプリ設定の保存に失敗した");
            return File.ReadAllText(path);
        }

        public static BackingStore LoadAppSettings(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "Profiles.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_Profile = path;
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(store).Load(), "アプリ設定の読込に失敗した");
            return store;
        }

        // ---- スペシャルアクション（Actions.xml）----

        /// <summary>
        /// 旧い書き方（Extras を持つ Macro、Arguements、UnloadTrigger、ID なし）の Actions.xml。
        /// 古いファイルの読み込みを守る固定の入力。
        /// </summary>
        private const string LegacyActionsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Special Actions Configuration Data. -->

<Actions>
  <Action Name=""Golden_Macro"">
    <Trigger>Cross</Trigger>
    <Type>Macro</Type>
    <Details>65/66/67</Details>
    <Extras>Hold</Extras>
  </Action>
  <Action Name=""Golden_Program"">
    <Trigger>Circle</Trigger>
    <Type>Program</Type>
    <Details>C:/golden/a.exe</Details>
    <Arguements>-arg</Arguements>
    <Delay>3</Delay>
  </Action>
  <Action Name=""Golden_Profile"">
    <Trigger>Square</Trigger>
    <Type>Profile</Type>
    <Details>Golden_ProfileName</Details>
    <UnloadTrigger>Triangle</UnloadTrigger>
  </Action>
  <Action Name=""Golden_Key"">
    <Trigger>L1</Trigger>
    <Type>Key</Type>
    <Details>65</Details>
    <UnloadTrigger>R1</UnloadTrigger>
    <SwitchMode>Toggle</SwitchMode>
  </Action>
  <Action Name=""Golden_Disconnect"">
    <Trigger>L2</Trigger>
    <Type>DisconnectBT</Type>
    <Details>5</Details>
  </Action>
  <Action Name=""Golden_Battery"">
    <Trigger>R2</Trigger>
    <Type>BatteryCheck</Type>
    <Details>50</Details>
  </Action>
  <Action Name=""Golden_Multi"">
    <Trigger>Cross/Circle</Trigger>
    <Type>MultiAction</Type>
    <Details>Golden_Macro/Golden_Program</Details>
  </Action>
  <Action Name=""Golden_Edit"">
    <Trigger>Square</Trigger>
    <Type>Program</Type>
    <Details>C:/golden/c.exe</Details>
    <Delay>1</Delay>
  </Action>
</Actions>
";

        public static string SaveActions(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            store.m_Actions = path;
            File.WriteAllText(path, LegacyActionsXml);
            return LegacyActionsXml;
        }

        /// <summary>
        /// 本番の保存の経路（<c>SaveActionNew</c> → <c>SaveActions</c>）で、全種別（1〜7、9、10、11）を書く。
        /// ID は固定（実行ごとに変わらないように）。
        /// </summary>
        public static string SaveActionsProduction(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            store.m_Actions = path;
            var xml = new DS4Windows.Core.Persistence.SpecialActionsXmlSerializer(store, TestActions.RuntimeReset);
            xml.SaveActionNew("Prod_Macro", "Cross", 1, "65/66/67", false, extras: "", actionId: "00000000000000000000000000000001", macroExecuteMode: DS4Windows.Core.Settings.MacroExecuteMode.Held);
            xml.SaveActionNew("Prod_Program", "Circle", 2, "C:/golden/a.exe?3", false, delayTime: 3.0, extras: "-arg", actionId: "00000000000000000000000000000002");
            xml.SaveActionNew("Prod_Profile", "Square", 3, "Golden_ProfileName", false, extras: "Triangle", actionId: "00000000000000000000000000000003");
            xml.SaveActionNew("Prod_Key", "L1", 4, "65", false, extras: "Toggle\nR1", actionId: "00000000000000000000000000000004");
            xml.SaveActionNew("Prod_Disconnect", "L2", 5, "5", false, delayTime: 1.5, actionId: "00000000000000000000000000000005");
            xml.SaveActionNew("Prod_Battery", "R2", 6, "50", false, delayTime: 2.0, actionId: "00000000000000000000000000000006");
            xml.SaveActionNew("Prod_Multi", "Cross/Circle", 7, "Prod_Macro/Prod_Program", false, actionId: "00000000000000000000000000000007");
            xml.SaveActionNew("Prod_Gyro", "Triangle", 9, "", false, delayTime: 4.0, actionId: "00000000000000000000000000000009");
            xml.SaveActionNew("Prod_Button", "R1", 10, "Cross", false, extras: "", actionId: "00000000000000000000000000000010");
            xml.SetActionAnalogStrength("Prod_Button", 55);
            xml.SaveActionNew("Prod_Focus", "L3", 11, "C:/golden/game.exe", false, delayTime: 0.5, extras: "Game Window", actionId: "00000000000000000000000000000011", centerCursor: true);
            xml.SetActionExtras("Prod_Key", "Turbo");
            return File.ReadAllText(path);
        }

        public static BackingStore LoadActions(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_Actions = path;
            Assert.True(new DS4Windows.Core.Persistence.SpecialActionsXmlSerializer(store, TestActions.RuntimeReset).LoadActions(), "アクションの読込に失敗した");
            return store;
        }

        // ---- リンクプロファイル（LinkedProfiles.xml）----

        public static string SaveLinkedProfiles(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            store.m_linkedProfiles = path;
            var linked = new DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer(store);
            Assert.True(linked.CreateLinkedProfiles(), "リンクプロファイルの新規作成に失敗した");
            store.linkedProfiles["0123456789AB"] = "Golden_ProfileA";
            store.linkedProfiles["BA9876543210"] = "Golden_ProfileB";
            Assert.True(linked.SaveLinkedProfiles(), "リンクプロファイルの保存に失敗した");
            return File.ReadAllText(path);
        }

        public static BackingStore LoadLinkedProfiles(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_linkedProfiles = path;
            Assert.True(new DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer(store).LoadLinkedProfiles(), "リンクプロファイルの読込に失敗した");
            return store;
        }

        // ---- プリセット ----

        public static readonly string[] PresetNames =
        {
            "LoadBlankDS4Profile", "LoadBlankProfile", "LoadDefaultGamepadGyroProfile", "LoadDefaultDS4GamepadGyroProfile",
            "LoadDefaultMixedGyroMouseProfile", "LoadDefaultDS4MixedGyroMouseProfile", "LoadDefaultDS4MixedControlsProfile",
            "LoadDefaultMixedControlsProfile", "LoadDefaultKBMProfile", "LoadDefaultKBMGyroMouseProfile",
        };

        public static BackingStore LoadPreset(PersistenceGoldenContext ctx, string presetName)
        {
            BackingStore store = NewStore();
            var s = new DS4Windows.Core.Persistence.ProfilePresets(store);
            IProfileLoadTarget c = ctx.LoadTarget;
            switch (presetName)
            {
                case "LoadBlankDS4Profile": s.LoadBlankDS4Profile(Slot, false, c, "", false, false); break;
                case "LoadBlankProfile": s.LoadBlankProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultGamepadGyroProfile": s.LoadDefaultGamepadGyroProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4GamepadGyroProfile": s.LoadDefaultDS4GamepadGyroProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultMixedGyroMouseProfile": s.LoadDefaultMixedGyroMouseProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4MixedGyroMouseProfile": s.LoadDefaultDS4MixedGyroMouseProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4MixedControlsProfile": s.LoadDefaultDS4MixedControlsProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultMixedControlsProfile": s.LoadDefaultMixedControlsProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultKBMProfile": s.LoadDefaultKBMProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultKBMGyroMouseProfile": s.LoadDefaultKBMGyroMouseProfile(Slot, false, c, "", false, false); break;
                default: throw new ArgumentException(presetName);
            }

            return store;
        }
    }
}
