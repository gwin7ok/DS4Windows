using System;
using System.Collections.Generic;
using System.Windows;
using DS4Windows.Core.Logging;
using System.Windows.Controls;
using System.Windows.Media;
using DS4Windows.Core.Model;
using Button = System.Windows.Controls.Button;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面のホバー表示用の表（ボタンごとの画像・位置・割り当て一覧の番号）を組み立てる関数。
    /// <c>ProfileEditor.xaml.cs</c> から移したもの（同じ部分クラス）。ボタンごとの違いは、下の表（配列）に 1 行ずつ書く。
    /// 表そのものの保持用のフィールド（<c>hoverImages</c>／<c>hoverLocations</c>／<c>hoverIndexes</c>／<c>reverseHoverIndexes</c>）と呼び出しは、<c>ProfileEditor.xaml.cs</c> に残る。
    /// </summary>
    public partial class ProfileEditor
    {
        private void PopulateReverseHoverIndexes()
        {
            try
            {
                foreach (KeyValuePair<Button, int> pair in hoverIndexes)
                {
                    reverseHoverIndexes.Add(pair.Value, pair.Key);
                }
                AppLogger.Log(AppLogLevel.Debug, $"[PopulateReverseHoverIndexes] Built reverseHoverIndexes count={reverseHoverIndexes.Count}");
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogLevel.Debug, $"[PopulateReverseHoverIndexes] failed: {ex.Message}");
            }
        }

        /// <summary>
        /// ホバーの表の 1 行: ボタン、割り当て一覧の番号、ホバー画像の名前、ホバー画像を置く位置と大きさの元になるボタン
        /// （<see cref="Rect"/> を持つときは固定の位置。どちらも無いときは、ボタン自身の位置と大きさ）。
        /// </summary>
        private readonly struct HoverRow
        {
            public HoverRow(Button button, int index, string? image, Button? shapeFrom = null, Rect? fixedRect = null)
            {
                Button = button;
                Index = index;
                Image = image;
                ShapeFrom = shapeFrom;
                FixedRect = fixedRect;
            }

            public Button Button { get; }

            public int Index { get; }

            public string? Image { get; }

            public Button? ShapeFrom { get; }

            public Rect? FixedRect { get; }
        }

        /// <summary>
        /// ホバーの全ボタンの表。番号は割り当て一覧（<c>mappingListVM</c>）の順。左タッチ以降のタッチの 4 つは、一覧の側の番号を使う。
        /// </summary>
        private HoverRow[] BuildHoverRows()
        {
            int Touch(DS4Controls control) => mappingListVM.ControlIndexMap[control];
            return new[]
            {
                new HoverRow(crossConBtn, 0, "Cross"),
                new HoverRow(circleConBtn, 1, "Circle"),
                new HoverRow(squareConBtn, 2, "Square"),
                new HoverRow(triangleConBtn, 3, "Triangle"),
                new HoverRow(optionsConBtn, 4, "options"),
                new HoverRow(shareConBtn, 5, "Share"),
                new HoverRow(upConBtn, 6, "Up"),
                new HoverRow(downConBtn, 7, "Down"),
                new HoverRow(leftConBtn, 8, "Left"),
                new HoverRow(rightConBtn, 9, "Right"),
                new HoverRow(guideConBtn, 10, "PS"),
                new HoverRow(muteConBtn, 11, "PS"),
                new HoverRow(l1ConBtn, 12, "L1"),
                new HoverRow(r1ConBtn, 13, "R1"),
                new HoverRow(l2ConBtn, 14, "L2"),
                new HoverRow(r2ConBtn, 15, "R2"),
                new HoverRow(l3ConBtn, 16, "LS"),
                new HoverRow(r3ConBtn, 17, "RS"),
                // 左・右・多点・上のタッチは、一覧の側の番号と、固定の位置・大きさを使う
                new HoverRow(leftTouchConBtn, Touch(DS4Controls.TouchLeft), "TouchLeft", fixedRect: new Rect(144, 44, 140, 98)),
                new HoverRow(rightTouchConBtn, Touch(DS4Controls.TouchRight), "TouchRight", fixedRect: new Rect(156, 47, 146, 94)),
                new HoverRow(multiTouchConBtn, Touch(DS4Controls.TouchMulti), "TouchMulti", fixedRect: new Rect(143, 42, 158, 100)),
                new HoverRow(topTouchConBtn, Touch(DS4Controls.TouchUpper), "TouchUpper", fixedRect: new Rect(155, 6, 153, 114)),
                // スティックの 4 方向は、押し込み（L3・R3）と同じ位置・大きさ・画像
                new HoverRow(lsuConBtn, 25, "LS", shapeFrom: l3ConBtn),
                new HoverRow(lsdConBtn, 26, "LS", shapeFrom: l3ConBtn),
                new HoverRow(lslConBtn, 27, "LS", shapeFrom: l3ConBtn),
                new HoverRow(lsrConBtn, 28, "LS", shapeFrom: l3ConBtn),
                new HoverRow(rsuConBtn, 29, "RS", shapeFrom: r3ConBtn),
                new HoverRow(rsdConBtn, 30, "RS", shapeFrom: r3ConBtn),
                new HoverRow(rslConBtn, 31, "RS", shapeFrom: r3ConBtn),
                new HoverRow(rsrConBtn, 32, "RS", shapeFrom: r3ConBtn),
                // 画像・位置の表は持たない（番号だけ）: ジャイロ・タッチパッドのスワイプのパネルのボタン
                new HoverRow(gyroPanel.TiltZNegButton, 33, null),
                new HoverRow(gyroPanel.TiltZPosButton, 34, null),
                new HoverRow(gyroPanel.TiltXNegButton, 35, null),
                new HoverRow(gyroPanel.TiltXPosButton, 36, null),
                new HoverRow(touchpadPanel.SwipeUpButton, 37, null),
                new HoverRow(touchpadPanel.SwipeDownButton, 38, null),
                new HoverRow(touchpadPanel.SwipeLeftButton, 39, null),
                new HoverRow(touchpadPanel.SwipeRightButton, 40, null),
                // 追加のボタンは PS の画像。位置の表は持たない
                new HoverRow(fnlConBtn, 41, "PS"),
                new HoverRow(fnrConBtn, 42, "PS"),
                new HoverRow(brpConBtn, 43, "PS"),
                new HoverRow(blpConBtn, 44, "PS"),
            };
        }

        private void PopulateHoverIndexes()
        {
            foreach (HoverRow row in BuildHoverRows())
            {
                hoverIndexes[row.Button] = row.Index;
            }

            AppLogger.Log(AppLogLevel.Debug, $"[PopulateHoverIndexes] Populated hoverIndexes count={hoverIndexes.Count}");
        }

        /// <summary>位置の表は、画像を持つ行のうち、追加のボタン（fnl・fnr・blp・brp）を除く（元から持たなかった）。</summary>
        private void PopulateHoverLocations()
        {
            foreach (HoverRow row in BuildHoverRows())
            {
                if (row.Image == null) continue;
                if (row.Button == fnlConBtn || row.Button == fnrConBtn || row.Button == blpConBtn || row.Button == brpConBtn)
                {
                    continue;
                }

                if (row.FixedRect is Rect fixedRect)
                {
                    hoverLocations[row.Button] = new HoverImageInfo() { point = fixedRect.Location, size = fixedRect.Size };
                    continue;
                }

                Button shape = row.ShapeFrom ?? row.Button;
                hoverLocations[row.Button] = new HoverImageInfo()
                {
                    point = new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape)),
                    size = new Size(shape.Width, shape.Height)
                };
            }

            AppLogger.Log(AppLogLevel.Debug, $"[PopulateHoverLocations] Populated hoverLocations count={hoverLocations.Count}");
        }

        private void PopulateHoverImages()
        {
            AppLogger.Log(AppLogLevel.Debug, "[PopulateHoverImages] loading hover images");
            ImageSourceConverter sourceConverter = new ImageSourceConverter();
            // 同じ画像の行（LS・RS・PS）は、1 つのブラシを共有する
            var brushes = new Dictionary<string, ImageBrush>();
            foreach (HoverRow row in BuildHoverRows())
            {
                if (row.Image == null) continue;
                if (!brushes.TryGetValue(row.Image, out ImageBrush? brush))
                {
                    ImageSource? source = sourceConverter.ConvertFromString(
                        $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_{row.Image}.png") as ImageSource;
                    brush = new ImageBrush(source);
                    brushes[row.Image] = brush;
                }

                hoverImages[row.Button] = brush;
            }
        }
    }
}
