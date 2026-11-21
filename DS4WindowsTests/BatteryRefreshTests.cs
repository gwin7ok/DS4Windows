#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Controllers タブの電池表示は、変化通知だけでなく 60 秒ごとにも読み直す
    /// （通知を取りこぼすと、表示が古い値のまま止まっていた）。文字列が変わったときだけ通知し、コントローラーが外れたら止める。
    /// 偽のコントローラーと、手動で進める時計を渡して、挙動で確かめる。
    /// </summary>
    public class BatteryRefreshTests : IDisposable
    {
        /// <summary>電池・充電の値と、イベントの登録（購読）を持つ偽のコントローラー。他のメンバーは既定値を返す。</summary>
        public class FakeView : DispatchProxy
        {
            public int Battery = 50;
            public bool Charging;
            private readonly Dictionary<string, Delegate?> _handlers = new Dictionary<string, Delegate?>();

            public static (IControllerSlotView View, FakeView State) Create()
            {
                var view = DispatchProxy.Create<IControllerSlotView, FakeView>();
                return (view, (FakeView)(object)view);
            }

            public void Raise(string eventName)
            {
                if (_handlers.TryGetValue(eventName, out Delegate? handler)) handler?.DynamicInvoke(this, EventArgs.Empty);
            }

            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                string name = method!.Name;
                if (name.StartsWith("add_", StringComparison.Ordinal))
                {
                    string e = name.Substring(4);
                    _handlers[e] = Delegate.Combine(_handlers.GetValueOrDefault(e), (Delegate)args![0]!);
                    return null;
                }
                if (name.StartsWith("remove_", StringComparison.Ordinal)) return null;
                if (name == "get_Battery") return Battery;
                if (name == "get_Charging") return Charging;
                Type returnType = method.ReturnType;
                if (returnType == typeof(void)) return null;
                if (returnType == typeof(string)) return string.Empty;
                return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
            }
        }

        private readonly string _dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BatRef_" + Guid.NewGuid().ToString("N"));

        public BatteryRefreshTests()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_dir, "Profiles"));
        }

        public void Dispose()
        {
            try { System.IO.Directory.Delete(_dir, true); } catch (System.IO.IOException) { /* 一時フォルダーの後始末の失敗は無視してよい */ }
        }

        private CompositeDeviceModel Create(IControllerSlotView view, ManualTimeProvider time)
        {
            var list = new ProfileList(new TempPathService(_dir));
            return new CompositeDeviceModel(view, 0, string.Empty, list,
                TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateProfileRepository(),
                null, time);
        }

        [Fact]
        public void TheInterval_Is60Seconds()
        {
            Assert.Equal(60000, CompositeDeviceModel.BatteryRefreshIntervalMs);
        }

        [Fact]
        public void TheRefresh_NotifiesOnlyWhenTheTextChanged()
        {
            var (view, state) = FakeView.Create();
            var time = new ManualTimeProvider();
            var model = Create(view, time);
            int notified = 0;
            model.BatteryStateChanged += (s, e) => notified++;

            time.Advance(TimeSpan.FromMilliseconds(CompositeDeviceModel.BatteryRefreshIntervalMs));   // 値が同じなら通知しない
            Assert.Equal(0, notified);

            state.Battery = 37;
            time.Advance(TimeSpan.FromMilliseconds(CompositeDeviceModel.BatteryRefreshIntervalMs));   // 文字列が変わったので通知する
            Assert.Equal(1, notified);

            time.Advance(TimeSpan.FromMilliseconds(CompositeDeviceModel.BatteryRefreshIntervalMs));   // 変わっていなければ、また通知しない
            Assert.Equal(1, notified);

            state.Charging = true;   // 充電中の印（+）が付いても、文字列が変わったので通知する
            time.Advance(TimeSpan.FromMilliseconds(CompositeDeviceModel.BatteryRefreshIntervalMs));
            Assert.Equal(2, notified);
            Assert.Equal("37%+", model.BatteryState);
        }

        [Fact]
        public void TheNotification_ReachesTheBinding_ThroughPropertyChanged()
        {
            // 根本原因: CompositeDeviceModel は INotifyPropertyChanged のため、WPF は BatteryStateChanged を無視し、PropertyChanged だけを見る
            var (view, state) = FakeView.Create();
            var time = new ManualTimeProvider();
            var model = Create(view, time);
            var changed = new List<string?>();
            model.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            state.Battery = 20;
            time.Advance(TimeSpan.FromMilliseconds(CompositeDeviceModel.BatteryRefreshIntervalMs));

            Assert.Contains(nameof(CompositeDeviceModel.BatteryState), changed);
        }

        [Fact]
        public void TheDeviceNotifications_AlsoReachTheBinding()
        {
            var (view, state) = FakeView.Create();
            var model = Create(view, new ManualTimeProvider());
            var changed = new List<string?>();
            model.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            state.Battery = 80;
            state.Raise("BatteryChanged");

            Assert.Contains(nameof(CompositeDeviceModel.BatteryState), changed);
        }

        [Fact]
        public void TheTimer_StopsWhenTheControllerIsRemoved()
        {
            var (view, state) = FakeView.Create();
            var time = new ManualTimeProvider();
            Create(view, time);
            Assert.Equal(1, time.ActiveTimerCount);

            state.Raise("Removal");

            Assert.Equal(0, time.ActiveTimerCount);
        }
    }
}
