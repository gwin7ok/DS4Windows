namespace DS4Windows.Core.Model
{
    /// <summary>
    /// DualSense の振動のエミュレーションの方式。設定値（保存・画面・出力）が参照するため、第1層の DualSenseDevice の外（Core/Model）に置く。
    /// 保存形式は値の名前（Accurate／Legacy／Disabled／Passthru）で、変えない。
    /// </summary>
    public enum RumbleEmulationMode
    {
        Accurate,
        Legacy,
        Disabled,
        Passthru,
    }
}
