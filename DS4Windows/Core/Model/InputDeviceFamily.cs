namespace DS4Windows.Core.Model
{
    /// <summary>
    /// 入力デバイスの系統。<see cref="InputDeviceType"/> の細かい種類（Joy-Con の左・右・グリップなど）を、設定・接続フック・生成を共有する単位にまとめたもの。
    /// 系統の対応は <see cref="InputDeviceTypeInfo"/> の表が持つ。
    /// </summary>
    public enum InputDeviceFamily
    {
        DS4,
        DualSense,
        SwitchPro,
        JoyCon,
        DS3,
    }
}
