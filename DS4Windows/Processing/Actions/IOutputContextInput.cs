namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.2-W2（Q3＝A）: エンジンが渡す <see cref="IOutputContext"/> が、このフレームの入力も持つときの口。
    /// Key の解放のログ（離されたトリガーのボタンの名前）のためだけに使う。入力が無い文脈（従来の呼び出し）は、この型を実装しないか、null を返す。
    /// </summary>
    public interface IOutputContextInput
    {
        ITriggerInput Input { get; }
    }
}
