using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W3（Q2＝A）: マクロの実行時状態の保持クラス（旧: <c>Mapping</c> の静的フィールド
    /// <c>macrodone</c>／<c>macroDispatchInFlight</c>／<c>macroDispatchLock</c>／<c>macroControl</c>／<c>macroCount</c>／<c>macroTaskQueue</c>）。
    /// <c>Mapping</c>（<c>PlayMacro</c> ほか）と <c>ActionsLoadedRuntimeReset</c> が使う。
    /// 配列の参照は再代入しない（要素の読み書きのみ）。要素の読み書きにロックはない（従来どおり。<c>DispatchInFlight</c> だけは
    /// <c>DispatchLock</c> で保護される）。マクロは別スレッドの <c>Task</c> から更新されるため、構造を変えずに移しただけである。
    /// </summary>
    public class MacroRuntimeState
    {
        /// <summary>DS4 のコントロールを添字にする配列の長さ（旧: <c>Mapping.DS4_CONTROL_MACRO_ARRAY_LEN</c>）。</summary>
        public const int ControlArrayLength = 52;

        /// <summary>マクロが送出するボタン／スティック／トリガーの出力フラグの数（旧: <c>macroControl</c> の長さ）。</summary>
        public const int MacroControlLength = 26;

        /// <summary>コントロールごとの「マクロの実行を済ませた」フラグ（旧: <c>macrodone</c>）。</summary>
        public bool[] MacroDone { get; } = new bool[ControlArrayLength];

        // TODO(Phase7-Step9、K3): PlayMacro の二重実行防止の暫定対策（旧: macroDispatchInFlight／macroDispatchLock。
        // Mapping.cs に書かれていた申し送りのとおり、トリガー判定層とマクロ実行層の分離とあわせて再設計する）。
        public bool[] DispatchInFlight { get; } = new bool[ControlArrayLength];

        public object DispatchLock { get; } = new object();

        /// <summary>マクロが出力中のボタン等のフラグ（旧: <c>macroControl</c>）。</summary>
        public bool[] MacroControl { get; } = new bool[MacroControlLength];

        /// <summary>出力中のマクロのキー数（旧: <c>macroCount</c>）。0 より大きい間、<c>MapCustom</c> が <c>MacroControl</c> を出力へ重ねる。</summary>
        public uint MacroCount { get; set; }

        /// <summary>デバイスごと、特殊アクション名ごとの、マクロの実行 Task の連鎖（旧: <c>macroTaskQueue</c>）。</summary>
        public Dictionary<string, Task>[] TaskQueue { get; }

        public MacroRuntimeState()
        {
            TaskQueue = new Dictionary<string, Task>[Global.MAX_DS4_CONTROLLER_COUNT];
            for (int i = 0; i < TaskQueue.Length; i++) TaskQueue[i] = new Dictionary<string, Task>();
        }

        /// <summary>「マクロの実行を済ませた」フラグをすべて解除する（Actions.xml の読み込み後のリセット用）。</summary>
        public void ClearMacroDone()
        {
            for (int i = 0; i < MacroDone.Length; i++) MacroDone[i] = false;
        }
    }
}
