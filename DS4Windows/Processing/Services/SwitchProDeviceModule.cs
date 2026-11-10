using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Services
{
    /// <summary>Phase9 9-E E-6c: Switch Pro の部品。設定の有効／無効と、追加ボタン（Capture）。</summary>
    public sealed class SwitchProDeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) => InputDeviceTypeInfo.IsFamily(type, InputDeviceFamily.SwitchPro);

        public override bool IsEnabled(ControlServiceDeviceOptions options) => options.SwitchProDeviceOpts.Enabled;

        public override List<DS4Controls> GetKnownExtraButtons(DS4Device device)
        {
            List<DS4Controls> result = new List<DS4Controls>();
            result.AddRange(new DS4Controls[] { DS4Controls.Capture });
            return result;
        }
    }
}
