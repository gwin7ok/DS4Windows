namespace DS4Windows.UI.Abstractions
{
    /// <summary>ファイルの選択の窓の指定（<see cref="IDialogService.PickOpenFile"/>、<see cref="IDialogService.PickSaveFile"/>）。</summary>
    public sealed class FileDialogOptions
    {
        public string Title { get; set; }

        /// <summary>例: <c>"Profile (*.xml)|*.xml"</c>。</summary>
        public string Filter { get; set; }

        public string InitialDirectory { get; set; }

        public string FileName { get; set; }

        public string DefaultExtension { get; set; }

        /// <summary>開くときだけ。存在するファイルしか選べないようにする。</summary>
        public bool CheckFileExists { get; set; } = true;
    }
}
