#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 接続中の機器の状態（UDP のモーションサーバーが「パッドの情報」として返す内容）。
    /// 第 3 層が第 1 層の具象を参照せずに読むための狭い契約。実装するのは <c>InputDeviceCore</c> だけ。
    /// </summary>
    public interface IPadStatusSource
    {
        string getMacAddress();

        bool isValidSerial();

        bool IsSynced();

        bool IsAlive();

        ConnectionType getConnectionType();

        bool IsDS4Idle();

        int getBattery();

        bool isCharging();
    }
}
