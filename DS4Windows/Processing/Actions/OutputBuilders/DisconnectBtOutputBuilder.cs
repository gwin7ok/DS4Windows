#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Actions.OutputBuilders
{
    /// <summary>DisconnectBT（Bluetooth の切断）の出力。</summary>
    public sealed class DisconnectBtOutputBuilder : ISpecialActionOutputBuilder
    {
        private readonly IControllerDisconnectPort _disconnectPort;
        private readonly IProfileSettingsService _profileSettings;

        public DisconnectBtOutputBuilder(IControllerDisconnectPort disconnectPort, IProfileSettingsService profileSettings)
        {
            ArgumentNullException.ThrowIfNull(disconnectPort);
            _disconnectPort = disconnectPort;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
        }

        public SpecialAction.ActionTypeId Kind => SpecialAction.ActionTypeId.DisconnectBT;

        public SpecialActionOutputs? Create(int device, AssignmentDefinition definition, SpecialAction source, int index)
        {
            AssignmentOrigin origin = definition.Origin;
            SpecialAction sa = source;
            // 切断の要求が成功するまで、押している間は再試行する。Fresh-press ガードは既定のまま。実行中のブロックなし
            return new SpecialActionOutputs(new IOutputAction[] { new DisconnectBTAction(sa, device, _disconnectPort, _profileSettings) },
                OutputBindingPolicy.For(origin, definition));
        }
    }
}
