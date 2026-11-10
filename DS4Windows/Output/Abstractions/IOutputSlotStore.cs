
using DS4Windows.Output;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 出力スロット割り当て（OutputSlots.xml）の永続化を抽象化するインターフェース。
    /// </summary>
    public interface IOutputSlotStore
    {
        bool Load(OutputSlotManager slotManager);
        bool Save(OutputSlotManager slotManager);
    }
}
