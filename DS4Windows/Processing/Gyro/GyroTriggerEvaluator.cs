#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// ジャイロの有効化トリガーの判定（トリガーの文字列の解析のキャッシュと、トグルの記憶を持つ）。
    /// 結果は <see cref="GyroTriggerResult"/> で返し、呼び出し側が読むフィールドを持たない。モードごとの設定は、デバイスの番号で引く。
    /// </summary>
    internal sealed class GyroTriggerEvaluator
    {
        private readonly int _deviceNum;
        private readonly IProfileSettingsService _profileSettings;
        // ジャイロの出力の種類（Controls／Mouse／MouseJoystick）ごとの、直した結果。入力のスレッドだけが読み書きする
        private readonly ParsedTriggerList?[] _cache = new ParsedTriggerList?[3];
        private bool _currentToggle;
        private bool _previousActivated;

        public GyroTriggerEvaluator(int deviceNum, IProfileSettingsService profileSettings)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _deviceNum = deviceNum;
            _profileSettings = profileSettings;
        }

        /// <summary>トグルの記憶を初期化する（プロファイルの切り替え・接続の入れ替え）。</summary>
        public void Reset()
        {
            _currentToggle = false;
            _previousActivated = false;
        }

        private ParsedTriggerList GetParsed(int cacheIndex, string source)
        {
            ParsedTriggerList? cached = _cache[cacheIndex];
            if (cached == null || !string.Equals(cached.Source, source))
            {
                cached = new ParsedTriggerList(source);
                _cache[cacheIndex] = cached;
            }

            return cached;
        }

        public GyroTriggerResult Evaluate(GyroOutMode mode, ControllerState s, bool toggleEnabled)
        {
            ParsedTriggerList? triggers = null;
            var andCond = false;
            bool reverseRatchet = _profileSettings.ForSlot(_deviceNum).Gyro.TriggerTurns;
            if (mode == GyroOutMode.Controls)
            {
                GyroControlsInfo controlsMapInfo = _profileSettings.ForSlot(_deviceNum).Gyro.Controls;
                triggers = GetParsed(0, controlsMapInfo.triggers);
                andCond = controlsMapInfo.triggerCond;
            }
            else if (mode == GyroOutMode.Mouse)
            {
                triggers = GetParsed(1, _profileSettings.ForSlot(_deviceNum).Gyro.MouseTriggers!);   // プロファイルの読み込み前の既定値で必ず設定される
                andCond = _profileSettings.ForSlot(_deviceNum).Gyro.MouseTriggerCondAnd;
            }
            else if (mode == GyroOutMode.MouseJoystick)
            {
                reverseRatchet = _profileSettings.ForSlot(_deviceNum).Gyro.MouseStickTriggerTurns;
                triggers = GetParsed(2, _profileSettings.ForSlot(_deviceNum).Gyro.MouseStickTriggers!);   // 同上
                andCond = _profileSettings.ForSlot(_deviceNum).Gyro.MouseStickTriggerCondAnd;
            }

            bool activated = andCond;
            if (triggers != null && !triggers.IsEmpty)
            {
                for (int index = 0, arlen = triggers.Values.Length; index < arlen; index++)
                {
                    bool pressed = triggers.Valid[index] && TriggerButtonReader.IsPressed(s, triggers.Values[index]);
                    if (andCond && !pressed)
                    {
                        activated = false;
                        break;
                    }
                    else if (!andCond && pressed)
                    {
                        activated = true;
                        break;
                    }
                }
            }

            if (toggleEnabled)
            {
                if (activated && activated != _previousActivated)
                {
                    _currentToggle = !_currentToggle;
                }

                _previousActivated = activated;
                activated = _currentToggle;
            }
            else
            {
                _previousActivated = activated;
            }

            return new GyroTriggerResult(activated, reverseRatchet);
        }
    }
}
