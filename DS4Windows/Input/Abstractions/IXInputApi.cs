namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// XInput（<c>xinput1_4.dll</c>）の窓口。番号（0〜3）で、状態の読み取りと振動の設定を行う。
    /// 実装は <c>XInputNativeApi</c>。テストでは、台本どおりに返す偽物に替えられる。
    /// </summary>
    public interface IXInputApi
    {
        /// <summary>番号の状態を読む。接続されていなければ false。</summary>
        bool TryGetState(int index, out XInputSnapshot state);

        /// <summary>番号の電池の情報を読む（ゲームパッド本体）。接続されていなければ false。</summary>
        bool TryGetBattery(int index, out XInputBattery battery);

        /// <summary>番号の振動を設定する（0〜65535）。接続されていなければ false。</summary>
        bool TrySetVibration(int index, ushort leftMotor, ushort rightMotor);

        /// <summary>番号のワイヤレスのコントローラーの電源を切る（切断させる）。要求が通れば true。</summary>
        bool TryPowerOff(int index);
    }
}
