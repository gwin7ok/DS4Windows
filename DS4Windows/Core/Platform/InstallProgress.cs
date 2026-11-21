namespace DS4Windows.Core.Platform
{
    /// <summary>導入の段階の通知。<paramref name="Percent"/> は <see cref="InstallStage.Downloading"/> のときだけ意味を持つ（0〜1）。</summary>
    public sealed record InstallProgress(InstallStage Stage, double Percent = 0);
}
