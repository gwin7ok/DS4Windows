using System;
using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9b-2（F02）の回帰テスト。
    /// F02: 2026-09-04（31dddc43）で IManagedActionManager／IControllerRegistry の登録が巻き込み削除され、
    ///      トグルの状態の更新が例外になり（呼び出し元が握りつぶす）IsToggledOn が true にならなかった。
    /// Phase13-Step4: 静的 ActionManager と ServiceProviderHolder（F07: GetRequiredService の検査を含む）を廃止したため、
    ///      管理役を注入して直接使う形に書き換えた。
    /// </summary>
    // 共有のテスト用ホストを使うテストは同じコレクションで直列に実行する
    [Collection("ServiceProviderHolder")]
    public class ManagedActionManagerRegistrationTests
    {
        [Fact]
        public void ServiceRegistration_RegistersManagedActionManagerAndControllerRegistry_AsSingletons()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var manager1 = TestHost.GetService<IManagedActionManager>();
            var manager2 = TestHost.GetService<IManagedActionManager>();
            Assert.NotNull(manager1);
            Assert.IsType<DefaultActionManager>(manager1);
            Assert.Same(manager1, manager2);

            var registry1 = TestHost.GetService<IControllerRegistry>();
            var registry2 = TestHost.GetService<IControllerRegistry>();
            Assert.NotNull(registry1);
            Assert.IsType<DefaultControllerRegistry>(registry1);
            Assert.Same(registry1, registry2);
        }

        [Fact]
        public void ManagedActionManager_SetToggledOn_SetsStateAndDoesNotThrow()
        {
            var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var action = new SpecialAction("Step9b2ToggleTest", "Cross", "Key", "Key", 0);
            const int device = 0;

            manager.SetToggledOn(action, device, true);
            Assert.True(manager.GetStateFor(action, device).IsToggledOn);

            manager.SetToggledOn(action, device, false);
            Assert.False(manager.GetStateFor(action, device).IsToggledOn);
        }

        /// <summary>
        /// Controls タブのキー割り当て（Toggle）は、識別用の合成 SpecialAction（SyntheticKeyActionFactory。新経路の束縛と同じ名前）に変換される。
        /// 合成アクションの状態が IManagedActionManager 経由で読み書きできることを固定する。
        /// </summary>
        [Fact]
        public void ControlsTab_SyntheticToggleKey_TogglesIsToggledOn_ThroughTheManager()
        {
            const int device = 0;
            SpecialAction sa = null;
            // Phase7-Step8 W2: DefaultActionManager は Func<IMapping>（切断時の後始末）を必須とする。合成アクションのキャッシュは Mapping の instance が持つ
            var mapper = TestMappingFactory.Create();
            var manager = new DefaultActionManager(() => mapper);
            try
            {
                // 一意の control 値（ほかのテストの合成アクションとキャッシュ・状態を共有しないため）
                sa = SyntheticKeyActionFactory.Create(device, 91, 0x41, toggle: true, useScan: false);
                Assert.True(sa.keyType.HasFlag(DS4KeyType.Toggle));

                // Phase8-Step8 8c: 系統 B の DispatchTriggerEdge は削除した。Toggle の状態の遷移は、新経路の束縛（ControlToggleOutputAction ほか）のテストが確かめる。
                manager.SetToggledOn(sa, device, true);
                Assert.True(manager.GetStateFor(sa, device).IsToggledOn);

                manager.SetToggledOn(sa, device, false);
                Assert.False(manager.GetStateFor(sa, device).IsToggledOn);
            }
            finally
            {
                try { if (sa != null) manager.SetToggledOn(sa, device, false); } catch { }
                try { manager.ClearDeviceState(device); } catch { }
            }
        }

        [Fact]
        public void ManagedActionManager_IsBeingTriggered_ReflectsTheState_AndIsFalseWithoutState()
        {
            var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var action = new SpecialAction("Phase13Step4BeingTriggered", "Cross", "Key", "Key", 0);

            Assert.False(manager.IsBeingTriggered(action, 0));
            Assert.False(manager.IsBeingTriggered(null, 0));
            Assert.False(manager.IsBeingTriggered(action, -1));

            // SetBeingTriggeredInternal は internal（本番ではエンジンだけが呼ぶ）。テストはリフレクションで立てる
            typeof(ActionInstanceState).GetMethod("SetBeingTriggeredInternal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(manager.GetStateFor(action, 0), new object[] { true });
            Assert.True(manager.IsBeingTriggered(action, 0));
            Assert.False(manager.IsBeingTriggered(action, 1));
        }

        [Fact]
        public void ManagedActionManager_ToggledOnChanged_FiresOnlyOnTheInstance_AndOnlyOnAChange()
        {
            var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var other = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
            var action = new SpecialAction("Phase13Step4Event", "Cross", "Key", "Key", 0);
            int fired = 0, otherFired = 0;
            manager.ToggledOnChanged += (a, d, o, n) => fired++;
            other.ToggledOnChanged += (a, d, o, n) => otherFired++;

            manager.SetToggledOn(action, 0, true);
            manager.SetToggledOn(action, 0, true);   // 変化なし
            manager.SetToggledOn(action, 0, false);

            Assert.Equal(2, fired);
            Assert.Equal(0, otherFired);   // 静的イベントを廃止したため、別の instance には届かない
        }
    }
}
