using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.5a-W2: 統一ディスパッチ（エンジン）から使うとき（添字などつきのコンストラクター）は、旧 <c>MapCustomAction</c> の Profile の分岐が行っていた次の処理も、同じ条件・順序で行う（Phase8-Step7-5-Plan.md Q2〜Q4）。
    /// (1) 連鎖の防止（<see cref="CanExecute"/>）、(2) ログ（Trace の立ち上がり 4 行、Debug 3 行）、(3) トリガーのボタンの通常の割り当て（キー、マクロ）のキーを離す、
    /// (4) 切替のあとでフレームを打ち切る（<c>RequestEndFrame</c>。Intent-Decisions 項目 5）。
    /// 旧経路のアダプター（<c>ProfileSwitchActionAdapter</c>）は、<c>Mapping</c> が同じ処理を行うため、添字なしのコンストラクターのままで、これらを行わない。
    /// 復帰の合図は、7.5b で、エンジンが持つ（自動の復帰 ＝ Unload on trigger release）。解除トリガー（<c>uTrigger</c>）を持つ Profile は、ビルダーが束縛を作らず、旧経路が処理し、その合図は <c>Mapping</c> が持つ。
    /// 旧経路が書く <c>prevProfileName</c>／<c>prevProfileWasTemporary</c> は、読む場所がないため、移さない（Q3＝A）。
    /// </summary>
    public class ProfileSwitchAction : IGatedOutputAction
    {
        private readonly bool _engineMode;
        private readonly int _actionIndex;
        private readonly ISpecialActionRepository _actionRepository;
        private readonly IProfileSettingsService _settings;
        private readonly Func<IMapping> _mappingProvider;
        private readonly TriggerButtonKeyReleaser _keyReleaser;

        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IProfileSwitcher profileSwitcher { get; }

        // Phase6-Step10-4-4: プロファイル切替役は必須の依存（AppHost／new DefaultProfileSwitcher のフォールバックを廃止）
        public ProfileSwitchAction(SpecialAction sa, IProfileSwitcher profileSwitcher) : this(sa, -1, profileSwitcher) { }

        /// <summary>エンジン用。<paramref name="actionIndex"/>・<paramref name="actionRepository"/> は、ログの「index」「ActionEntries」用。<paramref name="mappingProvider"/> は、連鎖の防止（復帰の合図の参照）用。</summary>
        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher, int actionIndex,
            ISpecialActionRepository actionRepository, IProfileSettingsService settings, Func<IMapping> mappingProvider)
            : this(sa, deviceIndex, profileSwitcher)
        {
            _engineMode = true;
            _actionIndex = actionIndex;
            _actionRepository = actionRepository;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _keyReleaser = new TriggerButtonKeyReleaser(_settings);
            _mappingProvider = mappingProvider;
        }

        public ProfileSwitchAction(SpecialAction sa, int deviceIndex, IProfileSwitcher profileSwitcher)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public string Id => sa?.name ?? "ProfileSwitch";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            if (_engineMode && ctx != null)
            {
                LogRising(ctx, dev);
                ReleaseTheTriggerButtonKeys(ctx, dev);
            }

            profileSwitcher.SwitchProfile(dev, sa);

            // 旧ループの return と同じ: 切替のあとは、このフレームの残りの特殊アクションを処理しない
            if (_engineMode) ctx?.RequestEndFrame();
        }

        /// <summary>
        /// 連鎖の防止（旧経路の条件と同じ）: 一時プロファイルの間、復帰の合図が Profile の特殊アクションなら、別の切替を始めない。
        /// 復帰の合図は、エンジン（<see cref="IPendingUntriggerQuery"/>）と旧経路（<c>Mapping</c>。解除トリガーつきの Profile）のどちらにもありうるため、両方を見る。
        /// </summary>
        public bool CanExecute(IOutputContext ctx)
        {
            if (!_engineMode || ctx == null) return true;

            int dev = (deviceIndex >= 0) ? deviceIndex : ctx.Device;
            if (!_settings.GetUseTempProfile(dev)) return true;

            SpecialAction pending = (ctx as IPendingUntriggerQuery)?.PendingUntriggerAction;
            if (pending != null && pending.typeID == SpecialAction.ActionTypeId.Profile) return false;

            pending = _mappingProvider?.Invoke()?.CaptureProfileUntrigger(dev).Action;
            return pending == null || pending.typeID != SpecialAction.ActionTypeId.Profile;
        }

        // 旧 MapCustomAction の Profile の分岐と同じ順序: LogSpecialActionTrace（立ち上がり）→ PROFILE ×2 → LogActionDoneCountOnTrigger
        private void LogRising(IOutputContext ctx, int dev)
        {
            try
            {
                if (AppLogger.IsTraceEnabled)
                {
                    AppLogger.LogTrace($"Trigger detected for SA '{sa.name}' on device {dev}");
                    AppLogger.LogTrace($"SA '{sa.name}' maps to Type:{sa.type} Details:{sa.details}");
                    // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                    AppLogger.LogTrace($"Trigger keys for SA '{sa.name}': <none>");
                    var parts = new List<string>();
                    (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{sa.name}' output combo: {combo}");
                }

                bool useTemp = _settings.GetUseTempProfile(dev);
                AppLogger.LogDebug($"SpecialAction PROFILE: Triggered for device {dev}, action={sa.name}, target={sa.details}");
                AppLogger.LogDebug($"SpecialAction PROFILE: beingTriggered={false}, useTempProfile={useTemp}");   // 旧経路は、立ち上がりのとき、常に False
                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { }
                AppLogger.LogDebug($"SpecialAction Profile: device={dev}, name={sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { }
        }

        // 旧経路と同じ: トリガーのボタンの通常の割り当てがキー／マクロなら、そのキーを離す（切替で、キーが押されたまま残らないように）。
        // 7.6b（Q5＝A）: DisconnectBT と共有する部品（TriggerButtonKeyReleaser）へ移した。処理は同じ
        private void ReleaseTheTriggerButtonKeys(IOutputContext ctx, int dev)
        {
            _keyReleaser.Release(sa, dev, ctx.OutputHandler);
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            profileSwitcher.RestoreProfile(dev);
        }
    }
}
