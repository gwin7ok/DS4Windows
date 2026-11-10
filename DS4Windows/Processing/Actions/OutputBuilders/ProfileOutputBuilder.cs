using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>Profile（プロファイルの切替）の出力。</summary>
    public sealed class ProfileOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IProfileSwitcher _profileSwitcher;
        private readonly ISpecialActionRepository _actionRepository;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IMapping _mapping;

        public ProfileOutputBuilder(IProfileSwitcher profileSwitcher, IProfileSettingsService profileSettings, ISpecialActionRepository actionRepository = null, IMapping mapping = null)
        {
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _actionRepository = actionRepository;
            _mapping = mapping;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.Profile;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            ControlSpecialItem item = definition.Item;
            // 解除トリガー（uTrigger）のボタンによる復帰は、機能廃止（規約 2.2。ユーザー承認済み）。UI から作れず、旧データ（Actions.xml）にだけ残っていた。
            // uTrigger を持つ Profile も束縛を作り、切替は行う。復帰は、自動（AutomaticUntrigger）のときだけ。uTrigger の値は読み込みも保存もそのまま（ファイルは変えない）。
            UntriggerPolicy untrigger = item != null && item.AutomaticUntrigger ? UntriggerPolicy.Automatic : UntriggerPolicy.None;
            return new SpecialActionOutputs(new IOutputAction[] { new ProfileSwitchAction(sa, device, _profileSwitcher, index, _actionRepository, _profileSettings, _mapping) },
                OutputBindingPolicy.For(origin, definition, blocksWhileExecuting: true, untrigger: untrigger,
                    stopOnRelease: untrigger == UntriggerPolicy.None));   // 復帰を持つ Profile は、離したエッジの Stop を呼ばない。復帰は ProcessUntrigger だけが担う（二重の復帰を防ぐ）
        }
    }
}
