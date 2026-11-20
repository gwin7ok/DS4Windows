namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 起動時にだけ必要な取り付け（CRC の表、トリガーの 2 段階の設定が変わったときの状態のリセット、
    /// デバイス登録簿・適用プロファイルの記憶・仮想パッドの失敗の処理の 3 つのフック）と、終了時の後始末の窓口。
    /// 取り付けは、生成の副作用ではなく <see cref="Initialize"/> の明示の呼び出しで行う（呼び出しは App の起動と、テストの <c>TestHost</c> だけ）。
    /// </summary>
    public interface IRuntimeBootstrapper
    {
        /// <summary>取り付けを行う。何度呼んでも 1 回しか行わない（フックの二重の購読を防ぐ）。</summary>
        void Initialize();

        /// <summary>仮想パッドのスロットを止めて保存し、イベントスレッドを終える（停止の後に呼ぶ）。</summary>
        void Shutdown();
    }
}
