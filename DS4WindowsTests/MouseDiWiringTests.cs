using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using DS4Windows.Input;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-5: `Mouse` が、コンストラクタで受け取った `IProfileSettingsService`／`IVirtualKBM` を
    /// 内部の `MouseCursor`／`MouseWheel` へ同一インスタンスのまま伝搬していることを検証する。
    /// `DS4Device` は実機・HID が必要なため、コンストラクタを実行せずに生成した未初期化オブジェクトを渡す
    /// （`Mouse` のコンストラクタは `GyroMouseSensSettings`（null でよい）しか参照しない）。
    /// </summary>
    public class MouseDiWiringTests
    {
        private const int Slot = 0;

        private static DS4Device CreateUninitializedDevice()
        {
            return (DS4Device)RuntimeHelpers.GetUninitializedObject(typeof(TestDs4Device));
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field); // フィールド名変更等で失われた場合に検出する
            return field.GetValue(target);
        }

        [Fact]
        public void Constructor_NullTimeProvider_Throws()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), settings, new RecordingVirtualKbm(), new StickMotionState(), new SyntheticInputState(), null));
        }

        // タップのクリックを押したままにする時間（50 ms）は、渡された TimeProvider の時刻で数える（実時間を待たない）
        [Fact]
        public void TapClickHold_UsesTheInjectedTimeProvider()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            var time = new ManualTimeProvider();
            var mouse = new Mouse(Slot, CreateUninitializedDevice(), settings, new RecordingVirtualKbm(), new StickMotionState(), new SyntheticInputState(), time);

            typeof(Mouse).GetMethod("StartTapClickHold", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mouse, null);
            DateTime until = (DateTime)GetField(mouse, "tapClickHoldUntil");

            Assert.Equal(time.GetUtcNow().UtcDateTime + TimeSpan.FromMilliseconds(50), until);
            time.Advance(TimeSpan.FromMilliseconds(49));
            Assert.True(time.GetUtcNow().UtcDateTime < until);
            time.Advance(TimeSpan.FromMilliseconds(2));
            Assert.False(time.GetUtcNow().UtcDateTime < until);
        }

        [Fact]
        public void Constructor_NullProfileSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), null, new RecordingVirtualKbm(), new StickMotionState(), new SyntheticInputState(), System.TimeProvider.System));
        }

        [Fact]
        public void Constructor_NullVirtualKbm_Throws()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), settings, null, new StickMotionState(), new SyntheticInputState(), System.TimeProvider.System));
        }

        // Phase7-Step8 W1: 保持クラスはコンストラクターで受ける（旧: StickMotionState.Shared／Mapping.MapClick）
        [Fact]
        public void Constructor_NullStateHolders_Throws()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            var kbm = new RecordingVirtualKbm();
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), settings, kbm, null, new SyntheticInputState(), System.TimeProvider.System));
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), settings, kbm, new StickMotionState(), null, System.TimeProvider.System));
        }

        [Fact]
        public void Constructor_PropagatesServicesToCursorAndWheel()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            var kbm = new RecordingVirtualKbm();

            var mouse = new Mouse(Slot, CreateUninitializedDevice(), settings, kbm, new StickMotionState(), new SyntheticInputState(), System.TimeProvider.System);

            Assert.Same(settings, GetField(mouse, "_profileSettings"));
            Assert.Same(kbm, GetField(mouse, "_virtualKBM"));

            object cursor = GetField(mouse, "cursor");
            Assert.Same(settings, GetField(cursor, "_profileSettings"));
            Assert.Same(kbm, GetField(cursor, "_virtualKBM"));

            object wheel = GetField(mouse, "wheel");
            Assert.Same(settings, GetField(wheel, "_profileSettings"));
            Assert.Same(kbm, GetField(wheel, "_virtualKBM"));
        }
    }
}
