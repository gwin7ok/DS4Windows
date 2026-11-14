using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// 外部プロセス起動の抽象化（分類① SpecialAction起動、分類② 権限昇格、分類⑥ 多重起動チェック共通）
    /// §2.1修正版: 新経路（DI経由）とフォールバック（直接 Process.Start）を共存させる
    /// </summary>
    public interface IProcessLauncher
    {
        /// <summary>
        /// 指定されたパスのプロセスを起動する（分類①・⑥共通、引数なしの単純起動）
        /// </summary>
        void Launch(string filePath);

        /// <summary>
        /// 引数・ウィンドウ非表示オプション付きでプロセスを起動する（分類①: specActionLaunchProc 完全互換）。
        /// hidden=true の場合、呼び出し側が .bat/.cmd 判定・COMSPEC ラップ・Arguments を確定済みの状態で渡す。
        /// </summary>
        void Launch(string fileName, string arguments, bool useShellExecute, bool hidden);

        /// <summary>
        /// 実行ファイルのあるフォルダーを作業フォルダーにして起動する（プロファイルの「起動するプログラム」用）。失敗は例外のまま返す。
        /// 既定の実装は <see cref="Launch(string)"/>。作業フォルダーを持てる実装だけが上書きする（既存の差し替え部品を壊さないため）。
        /// </summary>
        void LaunchWithWorkingDirectory(string filePath) => Launch(filePath);
    }
}
