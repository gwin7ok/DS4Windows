namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// UI が、スロット番号で指定した接続中のコントローラーへ出す操作の窓口（振動のテスト、ジャイロ較正のリセット）。
    /// UI が機器の具象型（<c>DS4Device</c>、<c>DualSenseDevice</c>）を知らずに済むようにする。
    /// スロットが範囲外、または機器が未接続のときは、何もせず false を返す（例外にしない）。
    /// </summary>
    public interface IControllerSlotCommands
    {
        /// <summary>そのスロットに機器が接続されているか。</summary>
        bool IsConnected(int slot);

        /// <summary>2 つのモーターの強さを指定して振動させる（0 で停止）。引数の順は機器の <c>setRumble</c> と同じ。</summary>
        bool SetRumble(int slot, byte rightLightFast, byte leftHeavySlow);

        /// <summary>重いモーターだけを変え、軽いモーターは今の強さのままにする。</summary>
        bool SetHeavyRumble(int slot, byte strength);

        /// <summary>軽いモーターだけを変え、重いモーターは今の強さのままにする。</summary>
        bool SetLightRumble(int slot, byte strength);

        /// <summary>
        /// 機器が DualSense のとき、振動のエミュレーション方式（<c>DualSenseDevice.RumbleEmulationMode</c> の値）と
        /// ハプティクスの強さを機器へ反映して true を返す。DualSense でなければ何もせず false を返す。
        /// </summary>
        bool ApplyDualSenseRumbleOptions(int slot, int rumbleEmulationMode, byte hapticPowerLevel);

        /// <summary>ジャイロの連続較正をやり直す。2 台が合体している機器では相手側も同時にやり直す。</summary>
        bool ResetGyroCalibration(int slot);
    }
}
