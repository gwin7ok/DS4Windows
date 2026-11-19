#nullable enable
using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IRuntimeFeatureSwitch"/> の偽物。入・切の呼び出しを、順序つきで記録する。</summary>
    public sealed class FakeRuntimeFeatureSwitch : IRuntimeFeatureSwitch
    {
        public readonly List<string> Calls = new List<string>();

        public void ChangeOscListenerStatus(bool state) => Calls.Add("OscListener:" + state);

        public void ChangeOscSenderStatus(bool state) => Calls.Add("OscSender:" + state);

        public void ChangeUdpStatus(bool state, bool openPort = true) => Calls.Add("Udp:" + state);

        public void ChangeMotionEventStatus(bool state) => Calls.Add("Motion:" + state);

        public void CheckHidHidePresence(string exePath = "", string exeName = "Autoprofile Exe", bool addExe = true) => Calls.Add("CheckHidHide");

        public void UpdateHidHiddenAttributes() => Calls.Add("UpdateHidHidden");

        public string TouchpadSlide(int ind) => string.Empty;

        public DS4Windows.Core.Model.DS4Controls GetActiveInputControl(int ind) => default;

        public void SetRumble(byte heavyMotor, byte lightMotor, int deviceNum) => Calls.Add("Rumble");
    }
}
