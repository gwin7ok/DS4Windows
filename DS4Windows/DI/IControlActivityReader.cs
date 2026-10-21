namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: 指定のコントロールが「今押されている（有効）」かを、入力の状態から判定する（マクロ記録画面が使う）。
    /// 以前は <c>Mapping.GetBoolMappingExternal</c>（と、その補助 <c>IsUsingGyroForControls</c>）だった。状態は持たない。
    /// 窓口 <see cref="IMapping.GetBoolMappingExternal"/> は変えず、<c>Mapping</c> がこの部品へ委譲する。
    /// </summary>
    public interface IControlActivityReader
    {
        /// <param name="settings">ジャイロの出力モードと感度（SX／SZ）を読む設定。</param>
        bool IsActive(IProfileSettingsService settings, int device, DS4Controls control,
            DS4State cState, DS4StateExposed eState, Mouse tp);
    }
}
