using DS4Windows.Core.Model;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 読み取り表示とスティックの較正画面が、コントローラーの状態を読むための、画面側の窓口。
    /// 画面は業務のサービスの型を直接参照しない（V2）ため、第 2 層のポートを <c>UI/Services</c> のアダプターで包んで渡す。
    /// </summary>
    public interface IControllerReadingsFeed
    {
        bool IsMeasuringProcessingDelay { get; set; }

        bool TryRead(int slot, int profileSlot, ControllerState input, ControllerState mapped, out ControllerReadingInfo info);

        ControllerState GetCurrentState(int slot);
    }
}
