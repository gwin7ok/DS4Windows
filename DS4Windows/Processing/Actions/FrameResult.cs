namespace DS4Windows.Actions
{
    /// <summary>Phase8-Step4 W1: 1 つの束縛の処理後に、フレームの走査を続けるか（Profile の切替・DisconnectBT の実行後は打ち切る）。</summary>
    public enum FrameResult
    {
        Continue = 0,
        EndFrame = 1,
    }
}
