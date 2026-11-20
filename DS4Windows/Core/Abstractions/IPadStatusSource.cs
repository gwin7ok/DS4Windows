using DS4Windows.Core.Model;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 接続中の機器の状態（UDP のモーションサーバーが「パッドの情報」として返す内容）。
    /// 第 3 層が第 1 層の具象を参照せずに読むための狭い契約。実装するのは <c>InputDeviceCore</c> だけ。
    /// </summary>
    public interface IPadStatusSource
    {
        string GetMacAddress();

        bool IsValidSerial();

        bool IsSynced();

        bool IsAlive();

        ConnectionType GetConnectionType();

        bool IsDS4Idle();

        int GetBattery();

        bool IsCharging();
    }
}
