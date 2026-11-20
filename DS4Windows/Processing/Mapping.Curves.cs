using DS4Windows.Processing.Touchpad;
using DS4Windows.Processing.Gyro;
using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using IVirtualKBM = DS4Windows.Processing.Abstractions.IVirtualKBM;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing
{
    /// <summary>スティック・トリガーのカーブとデッドゾーンの適用。</summary>
    public partial class Mapping
    {
        private static int ClampInt(int min, int value, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        // 補正の段（回転・ドリフト・デッドゾーン・出力カーブ・ジャイロ）は CurveAndDeadzoneStage が行う
        public ControllerState SetCurveAndDeadzone(int device, ControllerState cState, ControllerState dState,
                DS4Windows.Core.Abstractions.IProfileSettingsService settings)
        {
            return _curveStage.Apply(device, cState, dState, settings);
        }
    }
}
