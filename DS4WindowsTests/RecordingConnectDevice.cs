#nullable enable
using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4WindowsTests
{
    /// <summary>
    /// 接続の組み立て（<c>DeviceConnector.PrepareConnected</c>）、イベントの購読（<c>DeviceLifecycleHooks</c>）、開始・停止（<c>ServiceLifecycle</c>）の順序を確かめる偽の機器。
    /// 実機（HID）を持たず、接続の手順で呼ばれる仮想メンバーの呼び出しを記録し、切断・同期・シリアルのイベントを発生できる。
    /// 作成は <c>RuntimeHelpers.GetUninitializedObject</c>（コンストラクターは実行しない。<see cref="TestDs4Device"/> と同じ方式）。
    /// </summary>
    public sealed class RecordingConnectDevice : InputDeviceCore
    {
        private RecordingConnectDevice() : base(null, null) { }

        /// <summary>呼び出しの記録。外から順序を足すときは、同じ並びへ書く。</summary>
        public List<string> Calls { get; set; } = new List<string>();

        public override event EventHandler<EventArgs>? Removal;
        public override event ReportHandler<EventArgs> Report { add { Calls.Add("device.Report+="); } remove { } }

        public void SetIdentity(string mac, InputDeviceType type)
        {
            Mac = mac;
            deviceType = type;
        }

        /// <summary>入力レポートの処理（<c>InputReportProcessor.OnReport</c>）が読む、現在・前回の状態を用意する。</summary>
        public void InitStates()
        {
            cState = new ControllerState();
            pState = new ControllerState();
        }

        public void RaiseRemoval() => Removal?.Invoke(this, EventArgs.Empty);
        public void RaiseSync() => RaiseSyncChange();
        public void RaiseSerial()
        {
            // SerialChange は基底だけが発生できるため、イベントの保持先（自動実装のフィールド）を直接呼ぶ
            var field = typeof(InputDeviceCore).GetField(nameof(SerialChange), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            ((EventHandler<EventArgs>?)field!.GetValue(this))?.Invoke(this, EventArgs.Empty);
        }

        public override void LoadStoreSettings() => Calls.Add("device.LoadStoreSettings");
        public override void CheckControllerNumDeviceSettings(int numControllers) => Calls.Add("device.CheckControllerNumDeviceSettings");
        public override void PrimeBatteryReading() => Calls.Add("device.PrimeBatteryReading");
        public override void StartUpdate() => Calls.Add("device.StartUpdate");
        public override void StopUpdate() => Calls.Add("device.StopUpdate");
        public override void RemoveReportHandlers() { }
        public override void PostInit() { }
        protected override void RunInputLoop() { }

        public static RecordingConnectDevice Create()
        {
            var device = (RecordingConnectDevice)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RecordingConnectDevice));
            device.Calls = new List<string>();
            return device;
        }
    }
}
