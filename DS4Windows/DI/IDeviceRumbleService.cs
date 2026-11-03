namespace DS4Windows.DI
{
    /// <summary>
    /// Phase13-Step3: ブースト（<c>GetRumbleBoost</c>）と反転（<c>InverseRumbleMotors</c>）を適用した振動の設定
    /// （旧 <c>ControlService.SetDevRumble</c>）。入力レポートの処理など、デバイスの有無を確かめ済みの呼び出し側が使う。
    /// </summary>
    public interface IDeviceRumbleService
    {
        void SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum);
    }
}
