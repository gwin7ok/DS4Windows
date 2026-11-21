#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Touchpad;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>ジャイロの出力モード Mouse: トリガーの判定で、カーソルの移動か、端数の初期化かを選ぶ。感度が 0 以下なら何もしない。</summary>
    internal sealed class GyroMouseOutput : IGyroModeOutput
    {
        private readonly int _deviceNum;
        private readonly MouseCursor _cursor;
        private readonly IProfileSettingsService _profileSettings;

        public GyroMouseOutput(int deviceNum, MouseCursor cursor, IProfileSettingsService profileSettings)
        {
            ArgumentNullException.ThrowIfNull(cursor);
            ArgumentNullException.ThrowIfNull(profileSettings);
            _deviceNum = deviceNum;
            _cursor = cursor;
            _profileSettings = profileSettings;
        }

        public bool IsEnabled => _profileSettings.ForSlot(_deviceNum).Gyro.Sensitivity > 0;

        public void Process(SixAxisEventArgs arg, ControllerState state, bool shouldOutput)
        {
            if (shouldOutput)
                _cursor.SixaxisMoved(arg);
            else
                _cursor.MouseRemainderReset(arg);
        }
    }
}
