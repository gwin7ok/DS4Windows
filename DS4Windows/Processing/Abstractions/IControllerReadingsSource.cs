using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 読み取り表示（ControllerReadings タブ）とスティックの較正画面が、コントローラーの状態を読むための窓口。
    /// 表示は約 15fps で呼ぶため、状態は呼び出し側が持つ配列へコピーし、割り当てを増やさない。
    /// </summary>
    public interface IControllerReadingsSource
    {
        /// <summary>
        /// 表示中だけ true にする、処理遅延の計測の有効化。
        /// </summary>
        bool IsMeasuringProcessingDelay { get; set; }

        /// <summary>
        /// スロットの機器が生きていれば、入力の状態とマッピング後の状態を <paramref name="input"/>／<paramref name="mapped"/> へコピーして true を返す。
        /// 機器がない、生きていない、または読み取りの待機が 10ms で終わらなかったときは false を返し、何もコピーしない。
        /// <paramref name="profileSlot"/> が <paramref name="slot"/> と異なるときは、そのスロットのプロファイルの曲線とデッドゾーンをコピーに適用する。
        /// </summary>
        bool TryRead(int slot, int profileSlot, ControllerState input, ControllerState mapped, out ControllerReadingInfo info);

        /// <summary>スロットの現在の入力（範囲外は例外）。</summary>
        ControllerState GetCurrentState(int slot);
    }
}
