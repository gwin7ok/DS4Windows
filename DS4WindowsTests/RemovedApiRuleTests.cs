using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DS4Windows.Composition;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14d-Step2: 撤去した API（型・メンバー）が戻っていないことと、<c>AppHost</c> の利用者の規則を、型の検査 1 か所にまとめる。
    /// 旧: 各 Guard テストがソースを文字列で読み、名前が現れないことを個別に確かめていた（棚卸し表 <c>docs-forDIMG/MadeByAgent/Phase14d-Test-Inventory.md</c> の F）。
    /// 型がないことは、その型への参照をコンパイルで止める（たとえば <c>Global.xxx</c> とは書けない）。メンバーは、宣言されたもの（非公開・静的を含む）を調べる。
    /// 新しく型やメンバーを撤去したときは、ここに 1 行足す（由来の Phase を行のコメントに書く）。
    /// </summary>
    public class RemovedApiRuleTests
    {
        private static readonly Assembly Production = typeof(AppHost).Assembly;

        private const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>撤去した型（単純名。どの名前空間にもないこと）。</summary>
        private static readonly string[] RemovedTypeNames =
        {
            "ControlService",               // Phase27（取り付けは RuntimeBootstrapper へ。静的なスロット数の上限 CURRENT_DS4_CONTROLLER_LIMIT は Phase7-Step10e W1・W2 で撤去済み）
            "Global",                       // Phase7-Step10d W5（Phase6〜7 の Global の参照の検査はこの 1 行に畳んだ）
            "Program",                      // Phase7-Step5 W2（Program.rootHub）
            "ServiceProviderHolder",        // Phase13-Step4（Phase7-Step1 W4 のラチェットもこの 1 行に畳んだ）
            "SyntheticDispatcher",          // Phase14c-Step3（静的な橋渡し）
            "ActionRegistry",               // Phase14c-Step5（本番で未初期化の静的な登録簿）
            "DS4LightBar",                  // Phase16-Step7（静的なライトバーの色の計算は ILightbarService／LightbarService へ）
            "InputDeviceFactory",           // Phase16-Step5b（種類ごとの switch は IInputDeviceCartridge の Create へ）
            "MouseOutputAction",            // Phase7-Step8 W2
            "IUdpServerService",            // Phase9 9-E E-3（未接続の並行実装）
            "UdpServerService",             // Phase9 9-E E-3
            "DeviceStateChangedEventArgs",  // Phase7-Step2（孤立した契約の型）
            "ProfileSettingChangedEventArgs", // Phase7-Step2
            "ControllersViewModel",         // Phase7-Step2
            "LightbarMacro",                // Phase8-Step7 7.8e（ライトバーのマクロの廃止）
            "LightbarMacroCreator",         // Phase8-Step7 7.8e
            "BatteryReportArgs",            // Phase6-Step12（呼び出し元 0 件の旧シム）
            "ControllerRemovedArgs",        // Phase6-Step12
            "DeviceStatusChangeEventArgs",  // Phase6-Step12
            "DS4ControlAssignementSerializer",            // Phase6-Step12（旧 DTOXml）
            "DS4ControlAssignmentSerializerBase",         // Phase6-Step12
            "DS4ControlButtonAssignmentSerializer",       // Phase6-Step12
            "DS4ControlExtrasAssignmentSerializer",       // Phase6-Step12
            "DS4ControlKeyAssignmentSerializer",          // Phase6-Step12
            "DS4ControlKeyTypeAssignmentSerializer",      // Phase6-Step12
            "DS4ControlLightbarMacroAssignmentSerializer",// Phase6-Step12
            "DS4ControlMacroAssignmentSerializer",        // Phase6-Step12
            "DualSenseControllerSettings",  // Phase6-Step12
            "GyroControlsSettings",         // Phase6-Step12
            "GyroMouseSmoothingSettings",   // Phase6-Step12
            "GyroMouseStickSmoothingSettings", // Phase6-Step12
            "GyroSwipeSettings",            // Phase6-Step12
            "ProfileDTO",                   // Phase6-Step12
            "SASteeringWheelSmoothingOptions", // Phase6-Step12
            "StickAxialDeadOptionsSerializer", // Phase6-Step12
            "StickDeltaAccelSettings",      // Phase6-Step12
            "StickModeOutputSettings",      // Phase6-Step12
            "TouchpadMouseStickSerializer", // Phase6-Step12
        };

        /// <summary>撤去・移動したメンバー（型の単純名 → メンバー名）。型は本体に 1 つだけあること。</summary>
        private static readonly (string Type, string[] Members)[] RemovedMembers =
        {
            // Phase7-Step1 W1: 呼び出し元 0 件のメソッド
            ("BackingStore", new[] { "SaveOld", "UsedSavedProfileString", "RebuildProfileActionDicts", "GetDS4Action", "GetDS4Extra", "GetDS4KeyType", "GetDS4STrigger", "GetGyroOutModeString", "GetGyroOutModeType", "UpdateDS4CLightbarMacro" }),
            // Phase7-Step2: 本体の呼び出しが 0 件だった契約のメンバー
            ("IProfileSettingsService", new[]
            {
                "GetLowColor", "GetChargingColor", "GetCustomColor", "GetFlashColor", "GetUseCustomLed", "GetDS4CSettings",
                "GetGyroMouseDeadZone", "GetScrollSensitivity", "GetUseDInputOnly", "SetUseDInputOnly", "GetTempProfileDistance",
                "GetReverseX360ButtonMapping", "SAMouseStickTriggerCond", "SetTouchpadActive", "LinkedProfileCheckArray",
                "TempProfileNameArray", "ConfigDecimalCulture", "ResetAllToDefaults", "ResetToDefaults", "RumbleAutostopTime",
                "ProfileSettingChanged", "WireSubSettingsEvents", "UnwireSubSettingsEvents",
            }),
            ("IDeviceStateService", new[]
            {
                "Devices", "GetDevice", "IsDeviceConnected", "ConnectedControllersCount", "GetDeviceMacAddress",
                "GetConnectionType", "GetBatteryLevel", "SetDevice", "DeviceStateChanged",
            }),
            ("IOutputSlotService", new[] { "OutputDevices", "GetOutputDevice", "IsSlotPlugin" }),
            // Phase16-Step4b: 出力デバイスの配列と実体を契約から外した（HasOutputDevice／SendReport などのメソッドへ）
            ("IVirtualPadService", new[] { "OutputDevices" }),
            ("IProfileLoadTarget", new[] { "GetOutputDevice" }),
            ("IVirtualKBM", new[] { "ErrorMessage", "Version" }),
            ("INotificationService", new[] { "NotificationsEnabled", "FlashTaskbar" }),
            ("IPathService", new[] { "GetAutoProfilesPath", "ActionsPath" }),
            ("IProfileRepository", new[] { "GetProfileNames", "ProfileExists", "LoadProfileToSlot", "LoadDefaultProfile" }),
            ("ISpecialActionRepository", new[] { "AddAction", "ReplaceAction", "RemoveAction", "GetActionIndex", "GetAction", "ActionExists", "SaveActions" }),
            ("IInputDeviceRegistry", new[] { "ConnectedDevices", "DeviceCount", "IsHidHideInstalled" }),
            // Phase7-Step4 W4: 起動前の設定の読み込みは App のブートストラップへ
            ("WelcomeDialog", new[] { "loadConfig" }),
            // Phase7-Step7 W3: マクロの実行時状態は MacroRuntimeState へ
            ("Mapping", new[] { "macrodone", "DS4ControltoInt", "macroDispatchInFlight", "macroDispatchLock", "macroControl", "macroTaskQueue", "DS4_CONTROL_MACRO_ARRAY_LEN" }),
            // Phase7-Step8 W1・W2: SyntheticInputState への移動、本体の呼び出し 0 件、キー／ボタンのコントローラー登録簿の移動
            ("Mapping", new[] { "MapClick", "ResetRuntimeStateForKey", "DispatchProfileActionEdge", "TakePendingRestoreProfileName", "ApplyProfileDirect", "RestoreProfileDirect", "keyButtonControllerLock", "keyButtonControllers" }),
            // Phase7-Step8 W4: 状態の保持クラスの暫定の静的な窓口
            ("SyntheticInputState", new[] { "Shared" }),
            ("MacroRuntimeState", new[] { "Shared" }),
            ("StickMotionState", new[] { "Shared" }),
            // Phase6-Step9-2: 呼び出し元 0 件だった更新確認の旧実装
            ("MainWindowsViewModel", new[] { "RunUpdaterCheck", "DownloadUpstreamVersionInfo", "LauchDS4Updater", "IsNET8Available", "DownloadUpstreamUpdaterVersion", "updaterExe" }),
            // Phase6-Step9-3: 購読者 0 件のハンドラー
            ("SettingsViewModel", new[] { "SettingsViewModel_CheckForUpdatesChanged" }),
            // Phase6-Step10-1: 呼び出し元 0 件
            ("Util", new[] { "ElevatedCopyUpdater" }),
        };

        [Fact]
        public void RemovedTypes_DoNotExist()
        {
            var reappeared = Production.GetTypes()
                .Select(t => t.Name.Split('`')[0])
                .Where(n => RemovedTypeNames.Contains(n, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
            Assert.True(reappeared.Count == 0, "撤去した型が本体に戻っている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void RemovedMembers_AreNotDeclaredAgain()
        {
            var problems = new List<string>();
            foreach ((string typeName, string[] members) in RemovedMembers)
            {
                Type[] matches = Production.GetTypes().Where(t => t.Name == typeName).ToArray();
                if (matches.Length != 1)
                {
                    problems.Add($"{typeName}: 本体に {matches.Length} 件ある（1 件のはず。型の名前を変えたら、この表も直す）");
                    continue;
                }

                foreach (string member in members)
                {
                    if (matches[0].GetMember(member, Declared).Length > 0)
                        problems.Add($"{matches[0].FullName}.{member}");
                }
            }

            Assert.True(problems.Count == 0, "撤去・移動したメンバーが戻っている（または表が古い）:\n" + string.Join("\n", problems));
        }

        [Fact]
        public void OnlyTheAppAndTheCompositionRoot_DependOnAppHost()
        {
            // 規約 §3.1（Pure DI。Service Locator を持ち込まない）。旧: Step10FourGlobalRatchetGuardTests の AppHost の列と、各 Guard の DoesNotContain("AppHost")
            string appHost = typeof(AppHost).FullName;
            // 空振りの防止: 走査が AppHost への参照を拾えていること（App は AppHost を使う）
            Assert.Contains(LayerDependencyScanner.Edges, e => e.Source == "DS4Windows.App" && e.Target == appHost);

            var users = LayerDependencyScanner.Edges
                .Where(e => e.Target == appHost)
                .Select(e => e.Source)
                .Where(s => s != "DS4Windows.App" && !s.StartsWith("DS4Windows.Composition.", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            Assert.True(users.Count == 0, "AppHost を使ってよいのは App と DS4Windows.Composition だけ: " + string.Join(", ", users));
        }
    }
}
