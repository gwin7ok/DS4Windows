using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DS4Windows.UI.Controls
{
    /// <summary>
    /// コントローラーの図の上に、<see cref="ControllerLayout"/> の配置表どおりにボタンを並べる Canvas。
    /// 背景の画像は、置く側が <see cref="Panel.Background"/> に与える（テーマで画像が変わるため）。
    /// 配置を切り替えても、同じ <see cref="HitboxRegion.Id"/> のボタンは同じインスタンスのまま位置と大きさだけ変わる
    /// （置く側がボタンに結び付けた処理・割り当てを保つため）。
    /// <see cref="ScaleToFit"/> が true で表示領域が基準の大きさと違うときは、図（<c>Stretch=Uniform</c> の背景画像）と同じ縮尺・中央寄せで座標を拡縮する。
    /// 既定（false）と、基準の大きさと同じときは、座標は表の値そのまま。
    /// </summary>
    public class ControllerVisualizerControl : Canvas
    {
        public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
            nameof(Layout), typeof(ControllerLayout), typeof(ControllerVisualizerControl),
            new PropertyMetadata(null, (d, e) => ((ControllerVisualizerControl)d).ApplyLayout()));

        public static readonly DependencyProperty ButtonStyleProperty = DependencyProperty.Register(
            nameof(ButtonStyle), typeof(Style), typeof(ControllerVisualizerControl),
            new PropertyMetadata(null, (d, e) => ((ControllerVisualizerControl)d).ApplyButtonStyle()));

        /// <summary>
        /// true なら、表示領域に合わせて座標を拡縮・中央寄せする。false（既定）なら、座標は表の値そのまま
        /// （図を固定の大きさで置く画面が、領域の余りで位置をずらさないため）。
        /// </summary>
        public static readonly DependencyProperty ScaleToFitProperty = DependencyProperty.Register(
            nameof(ScaleToFit), typeof(bool), typeof(ControllerVisualizerControl),
            new PropertyMetadata(false, (d, e) => ((ControllerVisualizerControl)d).ApplyLayout()));

        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();

        public ControllerLayout Layout
        {
            get => (ControllerLayout)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public bool ScaleToFit
        {
            get => (bool)GetValue(ScaleToFitProperty);
            set => SetValue(ScaleToFitProperty, value);
        }

        /// <summary>生成するボタンのスタイル（透明で、ホバー時の枠なしなど）。</summary>
        public Style ButtonStyle
        {
            get => (Style)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        /// <summary>これまでに生成したボタン。キーは <see cref="HitboxRegion.Id"/>。</summary>
        public IReadOnlyDictionary<string, Button> Buttons => _buttons;

        public Button this[string id] => _buttons[id];

        /// <summary>配置表の座標を、表示領域（<paramref name="width"/>×<paramref name="height"/>）に写す縮尺と余白。</summary>
        public static (double Scale, double OffsetX, double OffsetY) ComputeFit(ControllerLayout layout, double width, double height)
        {
            if (!(width > 0) || !(height > 0))
            {
                return (1.0, 0.0, 0.0);
            }

            double scale = Math.Min(width / layout.ReferenceWidth, height / layout.ReferenceHeight);
            return (scale, (width - layout.ReferenceWidth * scale) / 2.0, (height - layout.ReferenceHeight * scale) / 2.0);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Size result = base.ArrangeOverride(arrangeSize);
            if (Layout != null)
            {
                Place(Layout, arrangeSize.Width, arrangeSize.Height);
            }

            return result;
        }

        private void ApplyLayout()
        {
            ControllerLayout layout = Layout;
            if (layout == null)
            {
                foreach (Button button in _buttons.Values)
                {
                    button.Visibility = Visibility.Collapsed;
                }

                return;
            }

            var inLayout = new HashSet<string>();
            foreach (HitboxRegion region in layout.Regions)
            {
                inLayout.Add(region.Id);
                if (!_buttons.TryGetValue(region.Id, out Button button))
                {
                    button = new Button { Tag = region.Tag };
                    if (ButtonStyle != null)
                    {
                        button.Style = ButtonStyle;
                    }

                    // 図の上に重ねる他の要素（ホバーの強調表示など。ヒットテストを奪う）より常に前に置く
                    Panel.SetZIndex(button, 1);
                    _buttons.Add(region.Id, button);
                    Children.Add(button);
                }

                button.Tag = region.Tag;
            }

            foreach (KeyValuePair<string, Button> pair in _buttons)
            {
                if (!inLayout.Contains(pair.Key))
                {
                    pair.Value.Visibility = Visibility.Collapsed;
                }
            }

            Place(layout, ActualWidth, ActualHeight);
            InvalidateArrange();
        }

        private void Place(ControllerLayout layout, double width, double height)
        {
            (double scale, double offsetX, double offsetY) = ScaleToFit ? ComputeFit(layout, width, height) : (1.0, 0.0, 0.0);
            foreach (HitboxRegion region in layout.Regions)
            {
                Button button = _buttons[region.Id];
                button.Width = region.Width * scale;
                button.Height = region.Height * scale;
                SetLeft(button, offsetX + region.Left * scale);
                SetTop(button, offsetY + region.Top * scale);
                button.Visibility = region.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplyButtonStyle()
        {
            foreach (Button button in _buttons.Values)
            {
                button.Style = ButtonStyle;
            }
        }
    }
}
