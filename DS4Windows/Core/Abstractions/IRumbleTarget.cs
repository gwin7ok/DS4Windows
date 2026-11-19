#nullable enable

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// 振動を送れる入力機器。第 3 層（仮想パッドの戻り・振動の転送）が、第 1 層の具象（<c>InputDeviceCore</c>）を参照せずに振動を渡すための狭い契約。
    /// 実装するのは <c>InputDeviceCore</c> だけ。
    /// </summary>
    public interface IRumbleTarget
    {
        /// <summary>右（軽い・速い）と左（重い・遅い）のモーターの強さを設定する。</summary>
        void setRumble(byte rightLightFastMotor, byte leftHeavySlowMotor);
    }
}
