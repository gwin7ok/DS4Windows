namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// スロットごとのタッチパッドの <see cref="Mouse"/>（カーソル・ホイール・タップの出力）を作る。
    /// 仮想キーボード・マウス、スティックの動き、合成入力の状態、時刻の供給を、作る側（<c>DeviceConnector</c>）が持たずに済むよう、ここにまとめる。
    /// </summary>
    public interface IMouseFactory
    {
        Mouse Create(int deviceIndex, DS4Windows.Input.InputDeviceCore device);
    }
}
