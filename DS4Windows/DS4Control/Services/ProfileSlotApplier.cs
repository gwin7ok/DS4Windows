using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IProfileSlotApplier"/> の既定実装。接続時などに、指定スロットへ通常プロファイルを適用する。
    /// Phase7-Step3c W4: 旧 <c>Global.ApplyProfileToSlot</c> を削除し、<see cref="IProfileApplicationService.ApplyProfileWithoutHalt"/> へ委譲する。
    /// 旧実装と同じく、スロット範囲・名前の検査で副作用なしに <c>false</c> を返し、Halt はしない
    /// （接続処理中の Halt の入れ子を避けるため。呼び出し元の ControlService は接続処理で、すでに入力を停止している）。
    /// </summary>
    public class ProfileSlotApplier : IProfileSlotApplier
    {
        private readonly IProfileApplicationService _profileApplication;
        // Phase7-Step10e W1: スロット数の上限は、静的な ControlService.CURRENT_DS4_CONTROLLER_LIMIT ではなく、注入された環境サービスから読む
        private readonly IEnvironmentService _environment;
        // Phase8-Step6 W2: 切断の後始末（ReleaseSlot）で、エンジンの状態と登録簿の束縛を消す
        private readonly DS4Windows.Actions.ITriggerDispatchEngine _engine;

        public ProfileSlotApplier(IProfileApplicationService profileApplication, IEnvironmentService environment,
            DS4Windows.Actions.ITriggerDispatchEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _profileApplication = profileApplication ?? throw new ArgumentNullException(nameof(profileApplication));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source)
        {
            if (slotIndex < 0 || slotIndex >= _environment.ControllerSlotLimit)
                return false;

            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            // 引数仕様: (deviceIndex, profileName, isTemp: false, launchProgram: false, source, prolog: null)
            return _profileApplication.ApplyProfileWithoutHalt(slotIndex, profileName, false, false, source, null);
        }

        // TODO(Phase8-Step7。規約 2.4): 移行対象（BindingMigrationOptions.MigratedTypes）が空の間、切断で消すものは空の登録簿とエンジンの空の状態だけ。
        // 種別ごとの切替が始まると、抑制の集合・実行中の状態・復帰の合図が実際に消える。
        public void ReleaseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _environment.ControllerSlotLimit)
                return;

            try
            {
                _engine.OnDeviceDisconnected(slotIndex);
                AppLogger.LogDebug($"[DI] ProfileSlotApplier.ReleaseSlot: released the dispatch state of slot {slotIndex}");
            }
            catch (Exception ex)
            {
                // 切断の処理（出力デバイスの取り外しほか）を止めない
                AppLogger.LogError($"[DI] ProfileSlotApplier.ReleaseSlot FAILED for slot {slotIndex}: {ex.Message}");
            }
        }

        public void PublishSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _environment.ControllerSlotLimit)
                return;

            _profileApplication.PublishBindings(slotIndex);
        }
    }
}
