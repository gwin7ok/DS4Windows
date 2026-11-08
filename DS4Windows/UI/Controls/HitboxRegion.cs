namespace DS4Windows.UI.Controls
{
    /// <summary>
    /// コントローラーの図の上に置く、ボタン 1 つ分の領域。位置と大きさは、配置の基準の大きさ
    /// （<see cref="ControllerLayout.ReferenceWidth"/>×<see cref="ControllerLayout.ReferenceHeight"/>）での画素の値。
    /// </summary>
    public sealed class HitboxRegion
    {
        public HitboxRegion(string id, string tag, double left, double top, double width, double height, bool isVisible = true)
        {
            Id = id;
            Tag = tag;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            IsVisible = isVisible;
        }

        /// <summary>配置をまたいで同じボタンを指す識別子（Xbox 360 と DualShock 4 の表で同じ値）。</summary>
        public string Id { get; }

        /// <summary>ボタンの Tag に入れる表示用の名前。</summary>
        public string Tag { get; }

        public double Left { get; }

        public double Top { get; }

        public double Width { get; }

        public double Height { get; }

        /// <summary>この配置で表示するか（例: タッチパッド クリックは DualShock 4 のときだけ）。</summary>
        public bool IsVisible { get; }
    }
}
