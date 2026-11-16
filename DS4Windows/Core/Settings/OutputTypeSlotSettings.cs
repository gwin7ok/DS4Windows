using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの出力の種類に関わる設定（保存される仮想コントローラーの種類、仮想のトリガーボタン、DS4 のトリガーの出力方式）の持ち主（値そのもの）。
    /// <see cref="BackingStore"/> がスロットごとに 1 つ持ち、保存・読み込み（プロファイルの XML）、仮想パッドの接続、設定画面が同じ 1 つのオブジェクトを読み書きする。
    /// 実行中に接続している種類（<c>ActiveOutDevType</c>）や画面の一時の種類（<c>OutDevTypeTemp</c>）は、別の持ち主（出力スロットのサービス）が持つ。
    /// </summary>
    public class OutputTypeSlotSettings
    {
        public OutputTypeSlotSettings(int slot)
        {
            Slot = slot;
            VirtualTriggerButtons = BackingStore.DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS;
            ResetForProfileLoad();
        }

        public int Slot { get; }

        /// <summary>プロファイルに保存される仮想コントローラーの種類（Xbox 360／DS4）。</summary>
        public OutContType ContType { get; set; }

        /// <summary>仮想のトリガーボタン。保存・読み込みの対象ではなく、プロファイルの読み込み前の初期化でも戻さない（従来の配列と同じ）。</summary>
        public bool VirtualTriggerButtons { get; set; }

        /// <summary>DS4 のトリガーの出力方式。</summary>
        public DS4TriggerOutputMode DS4TriggerMode { get; set; }

        /// <summary>プロファイルの読み込み前の既定値へ戻す（種類と DS4 のトリガーの出力方式）。</summary>
        public void ResetForProfileLoad()
        {
            ContType = BackingStore.DEFAULT_OUT_CONT_TYPE;
            DS4TriggerMode = BackingStore.DEFAULT_DS4_TRIGGER_OUTPUT;
        }
    }
}
