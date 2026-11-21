#nullable enable
using System;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// 外部コンポーネント（ViGEmBus・HidHide・FakerInput）の導入プログラム 1 つぶんの定義。
    /// 3 つの導入は、この定義の違い（ファイル名・URL・導入の確認）だけで、同じ流れ（<see cref="ExternalComponentInstaller"/>）に載る。
    /// </summary>
    /// <param name="FileName">一時フォルダーに置く導入プログラムのファイル名。</param>
    /// <param name="DownloadUrl">ダウンロード元。</param>
    /// <param name="IsInstalled">導入プログラムの終了後に、導入できたかを調べる（必要な再読み込みもここで行う）。</param>
    public sealed record ExternalComponentSpec(string FileName, string DownloadUrl, Func<bool> IsInstalled);
}
