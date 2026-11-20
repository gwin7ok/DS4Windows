using System;
using System.Collections.Generic;

namespace DS4Windows.UI.Controls
{
    /// <summary>コントローラー 1 種類分の、ボタンの配置表と、座標の基準になる大きさ。</summary>
    public sealed class ControllerLayout
    {
        public ControllerLayout(string name, double referenceWidth, double referenceHeight, IReadOnlyList<HitboxRegion> regions)
        {
            ArgumentNullException.ThrowIfNull(name);
            Name = name;
            ReferenceWidth = referenceWidth;
            ReferenceHeight = referenceHeight;
            ArgumentNullException.ThrowIfNull(regions);
            Regions = regions;
        }

        public string Name { get; }

        /// <summary>各領域の座標が前提にしている図の幅。</summary>
        public double ReferenceWidth { get; }

        /// <summary>各領域の座標が前提にしている図の高さ。</summary>
        public double ReferenceHeight { get; }

        public IReadOnlyList<HitboxRegion> Regions { get; }
    }
}
