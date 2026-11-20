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
    /// <summary>スペシャルアクションの実行・マクロの直接再生・抑制の判定。</summary>
    public partial class Mapping
    {
        /// <summary>
        /// DI/IMacroPlayer 用のマクロ実行エントリーポイント
        /// </summary>
        /// <summary>
        /// DI/IMacroPlayer 用のマクロ実行エントリーポイント
        /// </summary>
        public void PlayMacroDirect(int device, SpecialAction action)
        {
            // 4 スロット固定だと、拡張スロット数が 8 のときデバイス 4〜7 のマクロが再生されなかった。
            if (device < 0 || device >= ControllerLimits.MaxControllerCount || action == null) return;
            _macroPlayback.Play(device, String.Empty, action.macro, null, action.keyType, action, null);
        }

        /// <summary>
        /// DI/IMacroPlayer 用のマクロ停止・キー解放エントリーポイント
        /// </summary>
        public void EndMacroDirect(int device)
        {
            if (device < 0 || device >= ControllerLimits.MaxControllerCount) return;
            // TODO(技術的負債・Phase9 候補): IMacroPlayer.Stop の実体。旧 EndMacro（control == None のときは何もしない空処理）を削除した（Phase8-Fix-MacroRepeatUnify D5）ため、
            // 現状も何もしない（マクロの停止は、RepeatMacro のループが BeingTriggered を見て止まる）。インターフェース（IMapping の窓口）を保つために、空実装で残している。
            // 離したときにマクロを中断してキーを解放する必要が出たら、ここで実装する。
        }

        /// <summary>
        /// Method to check the currently active bool state for an input control.
        /// Slower version that does not take advantage of a generated ControllerStateFieldMapping instance.
        /// Meant to be used outside of the Mapping class
        /// </summary>
        /// <param name="device">Input slot number for InputDeviceCore</param>
        /// <param name="control">Current control mapped</param>
        /// <param name="cState">Current input state</param>
        /// <param name="eState">Exposed input state helper</param>
        /// <param name="tp">Mouse object</param>
        /// <returns>Is the specified input considered active</returns>
        public bool GetBoolMappingExternal(int device, DS4Controls control,
            ControllerState cState, ControllerStateExposed eState, TouchpadMouse tp)
        {
            // 判定の本体（約 105 行）は ControlActivityReader へ移した。設定は、Mapping が保持する profileSettings を渡す
            return _controlActivityReader.IsActive(profileSettings, device, control, cState, eState, tp);
        }
    }
}
