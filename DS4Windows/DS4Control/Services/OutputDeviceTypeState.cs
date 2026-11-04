using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase13-Step7 7-b: <see cref="IOutputDeviceTypeState"/> の実装（DI の Singleton。依存なし）。
    /// 配列の生成は、旧 <c>OutputSlotService</c> の <c>CreateFilled</c> をそのまま移したもの（長さ <c>TestProfileItemCount</c>、初期値も同じ）。
    /// Singleton なので、プロセス内で 1 つの配列になる（従来の静的配列と同じ）。
    /// </summary>
    public sealed class OutputDeviceTypeState : IOutputDeviceTypeState
    {
        public OutContType[] OutDevTypeTemp { get; } = CreateFilled(OutContType.X360);

        public OutContType[] ActiveOutDevType { get; } = CreateFilled(OutContType.None);

        private static OutContType[] CreateFilled(OutContType value)
        {
            var array = new OutContType[ControllerLimits.TestProfileItemCount];
            for (int i = 0; i < array.Length; i++) array[i] = value;
            return array;
        }
    }
}
