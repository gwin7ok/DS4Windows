using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// メイン画面（<see cref="MainWindow"/>）が使う、サービス（コントローラーの処理）の開始・停止と、通信機能の切り替えの窓口。
    /// 画面がサービスの型を直接参照しない（V2）ようにする。呼び出しは、そのまま委譲する。
    /// </summary>
    public sealed class MainWindowServiceControl
    {
        private readonly IServiceLifecycle lifecycle;
        private readonly IRuntimeFeatureSwitch features;

        public MainWindowServiceControl(IServiceLifecycle lifecycle, IRuntimeFeatureSwitch features)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.features = features ?? throw new ArgumentNullException(nameof(features));
        }

        public bool Running => lifecycle.Running;

        public bool Suspending
        {
            get => lifecycle.Suspending;
            set => lifecycle.Suspending = value;
        }

        public event EventHandler ServiceStarted
        {
            add => lifecycle.ServiceStarted += value;
            remove => lifecycle.ServiceStarted -= value;
        }

        public event EventHandler RunningChanged
        {
            add => lifecycle.RunningChanged += value;
            remove => lifecycle.RunningChanged -= value;
        }

        public event EventHandler PreServiceStop
        {
            add => lifecycle.PreServiceStop += value;
            remove => lifecycle.PreServiceStop -= value;
        }

        public bool Start(bool showlog = true) => lifecycle.Start(showlog);
        public bool Stop(bool showlog = true, bool immediateUnplug = false) => lifecycle.Stop(showlog, immediateUnplug);
        public bool HotPlug() => lifecycle.HotPlug();

        public string TouchpadSlide(int index) => features.TouchpadSlide(index);
        public void UpdateHidHiddenAttributes() => features.UpdateHidHiddenAttributes();
        public void ChangeOscListenerStatus(bool state) => features.ChangeOscListenerStatus(state);
        public void ChangeOscSenderStatus(bool state) => features.ChangeOscSenderStatus(state);
        public void ChangeUdpStatus(bool state, bool openPort = true) => features.ChangeUdpStatus(state, openPort);
        public void ChangeMotionEventStatus(bool state) => features.ChangeMotionEventStatus(state);
    }
}
