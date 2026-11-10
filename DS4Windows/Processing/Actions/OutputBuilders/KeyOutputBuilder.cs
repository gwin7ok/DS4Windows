using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>Key の出力。</summary>
    public sealed class KeyOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IManagedActionManager _manager;
        private readonly IKeyButtonControllerRegistry _controllers;
        private readonly ISpecialActionRepository _actionRepository;

        public KeyOutputBuilder(IProfileSettingsService profileSettings, IManagedActionManager manager, IKeyButtonControllerRegistry controllers, ISpecialActionRepository actionRepository = null)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _actionRepository = actionRepository;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.Key;

        public SpecialActionOutputs Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            if (!definition.Key.HasValue) return null;
            // Controls 由来は、ログの index と ActionEntries の数（SA タブの設定の位置）を持たない（従来の Controls の束縛と同じ）
            ISpecialActionRepository logRepository = origin == AssignmentOrigin.Controls ? null : _actionRepository;
            return new SpecialActionOutputs(new IOutputAction[] { new KeyOutputAction(sa, definition.Key.Value, _profileSettings, _manager, _controllers, index, logRepository) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
