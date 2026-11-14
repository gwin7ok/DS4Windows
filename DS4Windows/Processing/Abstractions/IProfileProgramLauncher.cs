namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// プロファイルに結んだ「起動するプログラム」を起動する部品。起動の判断と実行は、ここ 1 か所だけが持つ
    /// （プロファイルの読み込み［永続化の層］は、起動しない）。
    /// </summary>
    public interface IProfileProgramLauncher
    {
        /// <summary>
        /// スロットのプロファイルに起動するプログラムが設定されていて、まだ動いていなければ、5 秒後に 1 回起動する
        /// （起動の直後に別のプログラムを立ち上げるゲームなどを待つため）。設定がないとき、すでに動いているときは何もしない。
        /// 起動の失敗は握りつぶす。
        /// </summary>
        void LaunchIfNotRunning(int slot);
    }
}
