using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W3（Q2＝A）: マクロの実行時状態の保持クラス（旧: <c>Mapping</c> の静的フィールド
    /// <c>macroControl</c>／<c>macroCount</c>／<c>macroTaskQueue</c> など）。マクロの再生（<c>MacroStepExecutor</c>／<c>MacroPlayback</c>）が使う。
    /// 配列の参照は再代入しない（要素の読み書きのみ）。要素の読み書きにロックはない（従来どおり）。マクロは別スレッドの <c>Task</c> から更新されるため、構造を変えずに移しただけである。
    /// Phase8-Fix-MacroRepeatUnify（D5）: control 単位の <c>MacroDone</c>／<c>DispatchInFlight</c>（本番では到達しなかった暫定対策）は削除した。
    /// Phase9 9-C（ユーザー判断 Q2）: <c>MacroControl</c>／<c>MacroCount</c> は、デバイスごとに持つ（旧: 全コントローラーで 1 つを共有していたため、
    /// コントローラー 1 のマクロが押したボタンが、全コントローラーの出力に重なっていた）。1 台で使う場合の挙動は変わらない。
    /// </summary>
    public class MacroRuntimeState
    {
        /// <summary>
        /// マクロが送出するボタン／スティック／トリガーの出力フラグの数（旧: <c>macroControl</c> の長さ 26）。
        /// Phase9 9-C（ユーザー承認）: マクロ値の範囲は 261〜<c>DS4ControlSettings.MAX_MACRO_VALUE</c>（288）で、フラグの添字は 0〜27。
        /// 旧実装は配列が 26 個（添字 0〜25）しかなく、287（Touch Started）・288（Touch Ended）で <c>IndexOutOfRangeException</c> になっていたため、28 個に広げた
        /// （添字 26・27 には対応する出力がなく、<see cref="ApplyHeldControls"/> は何も重ねない。例外にならないだけ）。
        /// </summary>
        public const int MacroControlLength = DS4ControlSettings.MAX_MACRO_VALUE - 261 + 1;

        // Phase8-Fix-MacroRepeatUnify（D5）: 旧 MacroDone（macrodone）／DispatchInFlight／TryBeginDispatch／EndDispatch（control 単位の二重実行防止の暫定対策。K3）は削除した。
        // PlayMacro の呼び出しは常に control == None で、本番では到達しない経路だったため。二重実行の防止は ActionInstanceState.IsMacroRunning が担う。

        /// <summary>マクロが出力中のボタン等のフラグ（旧: <c>macroControl</c>）。<c>[デバイス][フラグ]</c>。</summary>
        public bool[][] MacroControl { get; }

        /// <summary>デバイスごとの、出力中のマクロのキー数（旧: <c>macroCount</c>）。0 より大きい間、<c>MapCustom</c> が <c>MacroControl</c> を出力へ重ねる。</summary>
        public uint[] MacroCount { get; }

        /// <summary>デバイスごと、特殊アクション名ごとの、マクロの実行 Task の連鎖（旧: <c>macroTaskQueue</c>）。</summary>
        public Dictionary<string, Task>[] TaskQueue { get; }

        public MacroRuntimeState()
        {
            MacroControl = new bool[ControllerLimits.MaxControllerCount][];
            MacroCount = new uint[ControllerLimits.MaxControllerCount];
            TaskQueue = new Dictionary<string, Task>[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < TaskQueue.Length; i++)
            {
                MacroControl[i] = new bool[MacroControlLength];
                TaskQueue[i] = new Dictionary<string, Task>();
            }
        }

        /// <summary>
        /// そのデバイスのマクロが押しているゲームパッドのボタン等（<see cref="MacroControl"/>）を、出力 <paramref name="mapped"/> に重ねる（旧 <c>Mapping.MapCustom</c> の末尾のブロック）。
        /// 個数 <see cref="MacroCount"/> が 0 のときは何もしない。毎フレーム呼ばれる（割り当て 0）。
        /// </summary>
        public void ApplyHeldControls(int device, DS4State mapped)
        {
            if (MacroCount[device] > 0)
            {
                bool[] macroControl = MacroControl[device];
                if (macroControl[00]) mapped.Cross = true;
                if (macroControl[01]) mapped.Circle = true;
                if (macroControl[02]) mapped.Square = true;
                if (macroControl[03]) mapped.Triangle = true;
                if (macroControl[04]) mapped.Options = true;
                if (macroControl[05]) mapped.Share = true;
                if (macroControl[06]) mapped.DpadUp = true;
                if (macroControl[07]) mapped.DpadDown = true;
                if (macroControl[08]) mapped.DpadLeft = true;
                if (macroControl[09]) mapped.DpadRight = true;
                if (macroControl[10]) mapped.PS = true;
                if (macroControl[11]) mapped.L1 = true;
                if (macroControl[12]) mapped.R1 = true;
                if (macroControl[13]) mapped.L2 = 255;
                if (macroControl[14]) mapped.R2 = 255;
                if (macroControl[15]) mapped.L3 = true;
                if (macroControl[16]) mapped.R3 = true;
                if (macroControl[17]) mapped.LX = 255;
                if (macroControl[18]) mapped.LX = 0;
                if (macroControl[19]) mapped.LY = 255;
                if (macroControl[20]) mapped.LY = 0;
                if (macroControl[21]) mapped.RX = 255;
                if (macroControl[22]) mapped.RX = 0;
                if (macroControl[23]) mapped.RY = 255;
                if (macroControl[24]) mapped.RY = 0;
                if (macroControl[25]) mapped.OutputTouchButton = true;
            }
        }
    }
}
