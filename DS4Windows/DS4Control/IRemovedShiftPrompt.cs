namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step7 7.8d: 廃止された「シフト」機能の設定が残っていることを、ユーザーに警告し、削除か残すかを選ばせる窓口。
    /// 実装は画面（<c>ProfileEditor</c> のダイアログ）。テストでは偽物に差し替える。
    /// </summary>
    public interface IRemovedShiftPrompt
    {
        /// <param name="profileName">対象のプロファイルの名前（警告の文面に使う）。</param>
        RemovedShiftChoice Ask(string profileName);
    }
}
