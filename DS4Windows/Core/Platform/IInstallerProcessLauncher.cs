#nullable enable
using System;

namespace DS4Windows.Core.Platform
{
    /// <summary>導入プログラムの起動（テストで偽物に差し替える）。</summary>
    public interface IInstallerProcessLauncher
    {
        /// <summary>導入プログラムを起動し、終了したかを返す関数を返す。起動できなかったときは null。</summary>
        Func<bool>? Start(string filePath);
    }
}
