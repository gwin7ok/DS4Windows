namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 後の Step5（SA 側のアナログ出力）: SA のマウス移動（強さの固定値）が、マウスの移動量を借りる窓口。
    /// Controls のマウス移動（<see cref="IAnalogTriggerInput.GetMouseMovement"/>）は、元の入力のアナログ値で量が決まるが、SA の組み合わせトリガーはオン／オフのため、
    /// 「ボタンを押した（最大）ときの量」に強さ（0〜1）を掛けた量を返す。感度（<c>activeButtonSensitivity</c>）と縦方向の倍率は、プロファイルの設定に従う。
    /// </summary>
    public interface IFixedMouseInput
    {
        /// <param name="direction">0＝Up、1＝Down、2＝Left、3＝Right（<see cref="IAnalogTriggerInput.GetMouseMovement"/> と同じ）。</param>
        /// <param name="fraction">強さ（0〜1。1 は、Controls のボタンからのマウス移動と同じ量）。</param>
        double GetFixedMouseMovement(int direction, double fraction);
    }
}
