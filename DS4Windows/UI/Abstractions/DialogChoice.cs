namespace DS4Windows.UI.Abstractions
{
    /// <summary>3 つのボタンの確認の窓（<see cref="IDialogService.ChooseOneOfThree"/>）の答え。</summary>
    public enum DialogChoice
    {
        /// <summary>窓を閉じた（どのボタンも押さなかった）。</summary>
        Cancelled,
        First,
        Second,
        Third,
    }
}
