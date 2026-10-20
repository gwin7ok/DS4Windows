using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W3（Q2＝A）: マクロの実行時状態の保持クラス（旧: <c>Mapping</c> の静的フィールド
    /// <c>macroControl</c>／<c>macroCount</c>／<c>macroTaskQueue</c> など）。<c>Mapping</c>（<c>PlayMacro</c> ほか）が使う。
    /// 配列の参照は再代入しない（要素の読み書きのみ）。要素の読み書きにロックはない（従来どおり）。マクロは別スレッドの <c>Task</c> から更新されるため、構造を変えずに移しただけである。
    /// Phase8-Fix-MacroRepeatUnify（D5）: control 単位の <c>MacroDone</c>／<c>DispatchInFlight</c>（本番では到達しなかった暫定対策）は削除した。
    /// </summary>
    public class MacroRuntimeState
    {
        /// <summary>マクロが送出するボタン／スティック／トリガーの出力フラグの数（旧: <c>macroControl</c> の長さ）。</summary>
        public const int MacroControlLength = 26;

        // Phase8-Fix-MacroRepeatUnify（D5）: 旧 MacroDone（macrodone）／DispatchInFlight／TryBeginDispatch／EndDispatch（control 単位の二重実行防止の暫定対策。K3）は削除した。
        // PlayMacro の呼び出しは常に control == None で、本番では到達しない経路だったため。二重実行の防止は ActionInstanceState.IsMacroRunning が担う。

        /// <summary>マクロが出力中のボタン等のフラグ（旧: <c>macroControl</c>）。</summary>
        public bool[] MacroControl { get; } = new bool[MacroControlLength];

        /// <summary>出力中のマクロのキー数（旧: <c>macroCount</c>）。0 より大きい間、<c>MapCustom</c> が <c>MacroControl</c> を出力へ重ねる。</summary>
        public uint MacroCount { get; set; }

        /// <summary>デバイスごと、特殊アクション名ごとの、マクロの実行 Task の連鎖（旧: <c>macroTaskQueue</c>）。</summary>
        public Dictionary<string, Task>[] TaskQueue { get; }

        public MacroRuntimeState()
        {
            TaskQueue = new Dictionary<string, Task>[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < TaskQueue.Length; i++) TaskQueue[i] = new Dictionary<string, Task>();
        }
    }
}
