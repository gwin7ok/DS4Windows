namespace DS4Windows
{
    /// <summary>
    /// 名前空間 DS4Windows の既存コードが、修飾なしの <c>AppHost.GetService&lt;T&gt;()</c> と書いている箇所のための互換ラッパー。
    /// 正本は <c>DS4WinWPF.AppHost</c>。
    /// TODO(Phase7): 唯一の呼び出し元は Mapping.cs:86（<c>VirtualKBM</c>）。Mapping の instance 化（コンストラクタ注入）で呼び出しがなくなった時点で、このクラスごと削除する。
    /// Phase6-Step12 Wave 5 で、呼び出し元が 0 件だった Host／CreateHost／GetService(Type)／Dispose と、未使用の private フィールドを削除した。
    /// </summary>
    public static class AppHost
    {
        public static T GetService<T>() where T : class => DS4WinWPF.AppHost.GetService<T>();
    }
}
