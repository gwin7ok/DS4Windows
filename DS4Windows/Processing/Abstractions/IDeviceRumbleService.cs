#nullable enable

using DS4Windows.Core.Abstractions;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// ブースト（<c>GetRumbleBoost</c>）と反転（<c>InverseRumbleMotors</c>）を適用した振動の設定。
    /// 入力レポートの処理など、デバイスの有無を確かめ済みの呼び出し側が使う。
    /// </summary>
    public interface IDeviceRumbleService
    {
        void SetDevRumble(IRumbleTarget device, byte heavyMotor, byte lightMotor, int deviceNum);
    }
}
