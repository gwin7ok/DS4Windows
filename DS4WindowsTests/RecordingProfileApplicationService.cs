using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W2（Q2＝A）: <see cref="IProfileApplicationService"/> の記録用の実装。呼ばれたことだけを記録し、実際のプロファイルの読込・適用はしない。
    /// 特殊アクションのプロファイル切替・復帰が、どの条件で何回、どの入口（<c>ApplyFromAction</c>／<c>RestoreFromAction</c>／直接の <c>ApplyProfile</c>）を呼ぶかを
    /// 特性化するために使う（<see cref="SpecialActionTestScope.InstallProfileRecorder"/>）。
    /// </summary>
    public sealed class RecordingProfileApplicationService : IProfileApplicationService
    {
        public readonly List<(int Device, string ProfileName)> ApplyFromActionCalls = new List<(int, string)>();
        public readonly List<int> RestoreFromActionCalls = new List<int>();
        public readonly List<(int Device, string ProfileName, bool IsTemp, ProfileChangeSource Source)> ApplyProfileCalls =
            new List<(int, string, bool, ProfileChangeSource)>();
        public readonly List<int> PublishBindingsCalls = new List<int>();

        /// <summary><see cref="RestoreFromAction"/> の戻り値（既定は true）。</summary>
        public bool RestoreResult { get; set; } = true;

        /// <summary>
        /// 適用を呼ばれたとき、本番の <c>ProfileApplicationService.CompleteProfileApplication</c> と同じ、アクションの状態の掃除
        /// （<c>ActionManager.ClearAllEntries</c>、<c>ActionManager.ClearDeviceState</c>）を行うか（既定は true）。
        /// 本番では、この掃除でアクションの <c>BeingTriggered</c> が false に戻る。戻らないと、トリガーを離したとき
        /// <c>ProfileSwitchActionAdapter.OnRelease</c> → <c>RestoreFromAction</c> が追加で呼ばれる（Phase8 台帳 §8 項目 11）。
        /// </summary>
        public bool SimulateStateReset { get; set; } = true;

        private void ResetActionState(int deviceIndex)
        {
            if (!SimulateStateReset) return;
            try { DS4Windows.ActionManager.ClearAllEntries(); } catch { }
            try { DS4Windows.ActionManager.ClearDeviceState(deviceIndex); } catch { }
        }

        public void ApplyFromAction(int deviceIndex, SpecialAction action)
        {
            lock (ApplyFromActionCalls) ApplyFromActionCalls.Add((deviceIndex, action?.details));
            ResetActionState(deviceIndex);
        }

        public bool RestoreFromAction(int deviceIndex)
        {
            lock (RestoreFromActionCalls) RestoreFromActionCalls.Add(deviceIndex);
            return RestoreResult;
        }

        public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
            ProfileChangeSource source = ProfileChangeSource.Manual, string prolog = null)
        {
            lock (ApplyProfileCalls) ApplyProfileCalls.Add((deviceIndex, profileName, isTemp, source));
            ResetActionState(deviceIndex);
            return true;
        }

        public bool ApplyProfileWithoutHalt(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
            ProfileChangeSource source = ProfileChangeSource.Manual, string prolog = null)
        {
            lock (ApplyProfileCalls) ApplyProfileCalls.Add((deviceIndex, profileName, isTemp, source));
            ResetActionState(deviceIndex);
            return true;
        }

        public void PublishBindings(int deviceIndex)
        {
            lock (PublishBindingsCalls) PublishBindingsCalls.Add(deviceIndex);
        }
    }
}
