using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Touchpad;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>ジャイロの有効化トリガーの解析と判定。</summary>
    public partial class GyroOutputProcessor
    {
        /// <summary>
        /// トリガーの設定の文字列（「4,5」のような、コンマ区切りのボタンの番号）を、数の配列へ直したもの。
        /// 判定は毎フレーム行われるため、同じ文字列のあいだは直した結果を使い回し、Split と TryParse の割り当てを避ける。
        /// 数でない要素は、判定では「押されていない」として扱う（Valid が偽）。
        /// </summary>
        private sealed class ParsedTriggerList
        {
            public readonly string Source;
            public readonly bool IsEmpty;
            public readonly int[] Values;
            public readonly bool[] Valid;

            public ParsedTriggerList(string source)
            {
                Source = source;
                string[] parts = (source ?? string.Empty).Split(',');
                IsEmpty = string.IsNullOrEmpty(parts[0]);
                Values = new int[parts.Length];
                Valid = new bool[parts.Length];
                for (int k = 0; k < parts.Length; k++)
                {
                    Valid[k] = int.TryParse(parts[k], out Values[k]);
                }
            }
        }

        private ParsedTriggerList GetParsedTriggers(int cacheIndex, string source)
        {
            ParsedTriggerList cached = gyroTriggerCache[cacheIndex];
            if (cached == null || !string.Equals(cached.Source, source))
            {
                cached = new ParsedTriggerList(source);
                gyroTriggerCache[cacheIndex] = cached;
            }

            return cached;
        }

        public bool IsTriggerActive(GyroOutMode mode)
        {
            ParsedTriggerList? triggers = null;
            var andCond = false;
            useReverseRatchet = _profileSettings.ForSlot(deviceNum).Gyro.TriggerTurns;
            if (mode == GyroOutMode.Controls)
            {
                GyroControlsInfo controlsMapInfo = _profileSettings.ForSlot(deviceNum).Gyro.Controls;
                triggers = GetParsedTriggers(0, controlsMapInfo.triggers);
                andCond = controlsMapInfo.triggerCond;
            }
            else if (mode == GyroOutMode.Mouse)
            {
                triggers = GetParsedTriggers(1, _profileSettings.ForSlot(deviceNum).Gyro.MouseTriggers!);   // プロファイルの読み込み前の既定値で必ず設定される
                andCond = _profileSettings.ForSlot(deviceNum).Gyro.MouseTriggerCondAnd;
            }
            else if (mode == GyroOutMode.MouseJoystick)
            {
                useReverseRatchet = _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggerTurns;
                triggers = GetParsedTriggers(2, _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggers!);   // 同上
                andCond = _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggerCondAnd;
            }
            triggeractivated = andCond;
            if (triggers != null && !triggers.IsEmpty)
            {
                for (int index = 0, arlen = triggers.Values.Length; index < arlen; index++)
                {
                    bool pressed = triggers.Valid[index] && TriggerButtonReader.IsPressed(s, triggers.Values[index]);
                    if (andCond && !pressed)
                    {
                        triggeractivated = false;
                        break;
                    }
                    else if (!andCond && pressed)
                    {
                        triggeractivated = true;
                        break;
                    }
                }
            }
            if (toggleGyroControls)
            {
                if (triggeractivated && triggeractivated != previousTriggerActivated)
                {
                    currentToggleGyroControls = !currentToggleGyroControls;
                }

                previousTriggerActivated = triggeractivated;
                triggeractivated = currentToggleGyroControls;
            }
            else
            {
                previousTriggerActivated = triggeractivated;
            }

            return triggeractivated;
        }
    }
}
