using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// マクロの記録画面が使う窓口。記録中のフラグと、コントローラー 1（スロット 0）の入力の押下の判定を提供する。
    /// </summary>
    public interface IMacroRecordingState
    {
        /// <summary>記録中は、入力のマッピングを飛ばす（記録画面が書く）。</summary>
        bool IsRecording { get; set; }

        /// <summary>スロット 0 に機器が接続されていて、入力を読めるか。</summary>
        bool CanReadInput { get; }

        /// <summary>スロット 0 の入力で、そのコントロールが押されているか。機器がなければ false。</summary>
        bool IsPressed(DS4Controls control);
    }
}
