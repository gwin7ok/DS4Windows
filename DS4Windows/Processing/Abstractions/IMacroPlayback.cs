using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: マクロの再生の予約と進行（同期キュー、単発、押している間の繰り返し、後始末）。
    /// 以前は <c>Mapping.PlayMacro</c>／<c>PlayMacroTask</c>／<c>RunMacroOnce</c>（private）だった。入口の <c>IMapping.PlayMacroDirect</c> などは変えず、<c>Mapping</c> がここへ委譲する。
    /// </summary>
    public interface IMacroPlayback
    {
        /// <summary>
        /// マクロをバックグラウンドで再生する。手順は <paramref name="macroStr"/>（"/" 区切りの文字列）、<paramref name="macroLst"/>、<paramref name="macroArr"/> のいずれか 1 つで渡す。
        /// 特殊アクションが同期（または「押している間繰り返す」）なら、そのトリガーごとのキューで順番に実行する。
        /// </summary>
        void Play(int device, string macroStr, List<int> macroLst, int[] macroArr, DS4KeyType keyType,
            SpecialAction action = null, ActionInstanceState actionDoneState = null);

        /// <summary>切断時の後始末: そのデバイスの同期キューを空にする。</summary>
        void ClearDevice(int device);
    }
}
