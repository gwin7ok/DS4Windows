using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using NonFormTimer = System.Timers.Timer;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4Windows;
using DS4Windows.DI;
using System.ComponentModel;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;
using DS4Windows.Actions;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase8-Step4b W10（Phase8-Step4b-Plan.md §3.4、Q7＝A）: プロファイル編集画面のホバー表示用の表（ボタンごとの画像・位置・割り当て一覧の番号）を組み立てる関数。
    /// <c>ProfileEditor.xaml.cs</c> から移したもの（同じ部分クラス。移動のみで、本体・挙動は変えない）。
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
                App.logHolder?.Logger?.Debug($"[PopulateReverseHoverIndexes] Built reverseHoverIndexes count={reverseHoverIndexes.Count}");
            }
            catch (Exception ex)
            {
                App.logHolder?.Logger?.Debug($"[PopulateReverseHoverIndexes] failed: {ex.Message}");
            }
        }

        private void PopulateHoverIndexes()
        {
            hoverIndexes[crossConBtn] = 0;
            hoverIndexes[circleConBtn] = 1;
            hoverIndexes[squareConBtn] = 2;
            hoverIndexes[triangleConBtn] = 3;
            hoverIndexes[optionsConBtn] = 4;
            hoverIndexes[shareConBtn] = 5;
            hoverIndexes[upConBtn] = 6;
            hoverIndexes[downConBtn] = 7;
            hoverIndexes[leftConBtn] = 8;
            hoverIndexes[rightConBtn] = 9;
            hoverIndexes[guideConBtn] = 10;
            hoverIndexes[muteConBtn] = 11;
            hoverIndexes[l1ConBtn] = 12;
            hoverIndexes[r1ConBtn] = 13;
            hoverIndexes[l2ConBtn] = 14;
            hoverIndexes[r2ConBtn] = 15;
            hoverIndexes[l3ConBtn] = 16;
            hoverIndexes[r3ConBtn] = 17;

            // Phase6 是正（ユーザー承認 2026-10-15）: 6f04ecf6 で誤って欠落した Left Touch の登録を復元（ホバーで名前が出ない不具合）。
            // 同コミットが追加した brpConBtn = 44 は 763 行で 43 に上書きされるため削除。
            hoverIndexes[leftTouchConBtn] = mappingListVM.ControlIndexMap[DS4Controls.TouchLeft]; // 21

            App.logHolder?.Logger?.Debug($"[PopulateHoverIndexes] Populated hoverIndexes count={hoverIndexes.Count}");
            hoverIndexes[rightTouchConBtn] = mappingListVM.ControlIndexMap[DS4Controls.TouchRight]; // 22
            hoverIndexes[multiTouchConBtn] = mappingListVM.ControlIndexMap[DS4Controls.TouchMulti]; // 23
            hoverIndexes[topTouchConBtn] = mappingListVM.ControlIndexMap[DS4Controls.TouchUpper]; // 24

            hoverIndexes[lsuConBtn] = 25;
            hoverIndexes[lsdConBtn] = 26;
            hoverIndexes[lslConBtn] = 27;
            hoverIndexes[lsrConBtn] = 28;

            hoverIndexes[rsuConBtn] = 29;
            hoverIndexes[rsdConBtn] = 30;
            hoverIndexes[rslConBtn] = 31;
            hoverIndexes[rsrConBtn] = 32;

            hoverIndexes[gyroZNBtn] = 33;
            hoverIndexes[gyroZPBtn] = 34;
            hoverIndexes[gyroXNBtn] = 35;
            hoverIndexes[gyroXPBtn] = 36;

            hoverIndexes[swipeUpBtn] = 37;
            hoverIndexes[swipeDownBtn] = 38;
            hoverIndexes[swipeLeftBtn] = 39;
            hoverIndexes[swipeRightBtn] = 40;

            hoverIndexes[fnlConBtn] = 41;
            hoverIndexes[fnrConBtn] = 42;
            hoverIndexes[brpConBtn] = 43;
            hoverIndexes[blpConBtn] = 44;
        }

        private void PopulateHoverLocations()
        {
            hoverLocations[crossConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(crossConBtn), Canvas.GetTop(crossConBtn)),
                size = new Size(crossConBtn.Width, crossConBtn.Height)
            };
            hoverLocations[circleConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(circleConBtn), Canvas.GetTop(circleConBtn)),
                size = new Size(circleConBtn.Width, circleConBtn.Height)
            };

            App.logHolder?.Logger?.Debug($"[PopulateHoverLocations] Populated hoverLocations count={hoverLocations.Count}");
            hoverLocations[squareConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(squareConBtn), Canvas.GetTop(squareConBtn)),
                size = new Size(squareConBtn.Width, squareConBtn.Height)
            };
            hoverLocations[triangleConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(triangleConBtn), Canvas.GetTop(triangleConBtn)),
                size = new Size(triangleConBtn.Width, triangleConBtn.Height)
            };
            hoverLocations[l1ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l1ConBtn), Canvas.GetTop(l1ConBtn)),
                size = new Size(l1ConBtn.Width, l1ConBtn.Height)
            };
            hoverLocations[r1ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r1ConBtn), Canvas.GetTop(r1ConBtn)),
                size = new Size(r1ConBtn.Width, r1ConBtn.Height)
            };
            hoverLocations[l2ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l2ConBtn), Canvas.GetTop(l2ConBtn)),
                size = new Size(l2ConBtn.Width, l2ConBtn.Height)
            };
            hoverLocations[r2ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r2ConBtn), Canvas.GetTop(r2ConBtn)),
                size = new Size(r2ConBtn.Width, r2ConBtn.Height)
            };
            hoverLocations[shareConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(shareConBtn), Canvas.GetTop(shareConBtn)),
                size = new Size(shareConBtn.Width, shareConBtn.Height)
            };
            hoverLocations[optionsConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(optionsConBtn), Canvas.GetTop(optionsConBtn)),
                size = new Size(optionsConBtn.Width, optionsConBtn.Height)
            };
            hoverLocations[guideConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(guideConBtn), Canvas.GetTop(guideConBtn)),
                size = new Size(guideConBtn.Width, guideConBtn.Height)
            };
            hoverLocations[muteConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(muteConBtn), Canvas.GetTop(muteConBtn)),
                size = new Size(muteConBtn.Width, muteConBtn.Height)
            };

            hoverLocations[leftTouchConBtn] = new HoverImageInfo()
            { point = new Point(144, 44), size = new Size(140, 98) };
            hoverLocations[multiTouchConBtn] = new HoverImageInfo()
            { point = new Point(143, 42), size = new Size(158, 100) };
            hoverLocations[rightTouchConBtn] = new HoverImageInfo()
            { point = new Point(156, 47), size = new Size(146, 94) };
            hoverLocations[topTouchConBtn] = new HoverImageInfo()
            { point = new Point(155, 6), size = new Size(153, 114) };


            hoverLocations[l3ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
                size = new Size(l3ConBtn.Width, l3ConBtn.Height)
            };
            hoverLocations[lsuConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
                size = new Size(l3ConBtn.Width, l3ConBtn.Height)
            };
            hoverLocations[lsrConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
                size = new Size(l3ConBtn.Width, l3ConBtn.Height)
            };
            hoverLocations[lsdConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
                size = new Size(l3ConBtn.Width, l3ConBtn.Height)
            };
            hoverLocations[lslConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
                size = new Size(l3ConBtn.Width, l3ConBtn.Height)
            };

            hoverLocations[r3ConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
                size = new Size(r3ConBtn.Width, r3ConBtn.Height)
            };
            hoverLocations[rsuConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
                size = new Size(r3ConBtn.Width, r3ConBtn.Height)
            };
            hoverLocations[rsrConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
                size = new Size(r3ConBtn.Width, r3ConBtn.Height)
            };
            hoverLocations[rsdConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
                size = new Size(r3ConBtn.Width, r3ConBtn.Height)
            };
            hoverLocations[rslConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
                size = new Size(r3ConBtn.Width, r3ConBtn.Height)
            };

            hoverLocations[upConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(upConBtn), Canvas.GetTop(upConBtn)),
                size = new Size(upConBtn.Width, upConBtn.Height)
            };
            hoverLocations[rightConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(rightConBtn), Canvas.GetTop(rightConBtn)),
                size = new Size(rightConBtn.Width, rightConBtn.Height)
            };
            hoverLocations[downConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(downConBtn), Canvas.GetTop(downConBtn)),
                size = new Size(downConBtn.Width, downConBtn.Height)
            };
            hoverLocations[leftConBtn] = new HoverImageInfo()
            {
                point = new Point(Canvas.GetLeft(leftConBtn), Canvas.GetTop(leftConBtn)),
                size = new Size(leftConBtn.Width, leftConBtn.Height)
            };
        }

        private void PopulateHoverImages()
        {
            App.logHolder?.Logger?.Debug("[PopulateHoverImages] loading hover images");
            ImageSourceConverter sourceConverter = new ImageSourceConverter();

            ImageSource temp =
                sourceConverter.ConvertFromString(
                    $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Cross.png") as ImageSource;
            ImageBrush crossHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Circle.png") as ImageSource;
            ImageBrush circleHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Square.png") as ImageSource;
            ImageBrush squareHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Triangle.png") as ImageSource;
            ImageBrush triangleHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_L1.png") as ImageSource;
            ImageBrush l1Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_R1.png") as ImageSource;
            ImageBrush r1Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_L2.png") as ImageSource;
            ImageBrush l2Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_R2.png") as ImageSource;
            ImageBrush r2Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Share.png") as ImageSource;
            ImageBrush shareHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_options.png") as ImageSource;
            ImageBrush optionsHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_PS.png") as ImageSource;
            ImageBrush guideHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_TouchLeft.png") as ImageSource;
            ImageBrush leftTouchHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_TouchMulti.png") as ImageSource;
            ImageBrush multiTouchTouchHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_TouchRight.png") as ImageSource;
            ImageBrush rightTouchHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_TouchUpper.png") as ImageSource;
            ImageBrush topTouchHover = new ImageBrush(temp);


            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_LS.png") as ImageSource;
            ImageBrush l3Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_LS.png") as ImageSource;
            ImageBrush lsuHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_LS.png") as ImageSource;
            ImageBrush lsrHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_LS.png") as ImageSource;
            ImageBrush lsdHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_LS.png") as ImageSource;
            ImageBrush lslHover = new ImageBrush(temp);


            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_RS.png") as ImageSource;
            ImageBrush r3Hover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_RS.png") as ImageSource;
            ImageBrush rsuHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_RS.png") as ImageSource;
            ImageBrush rsrHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_RS.png") as ImageSource;
            ImageBrush rsdHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_RS.png") as ImageSource;
            ImageBrush rslHover = new ImageBrush(temp);


            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Up.png") as ImageSource;
            ImageBrush upHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Right.png") as ImageSource;
            ImageBrush rightHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Down.png") as ImageSource;
            ImageBrush downHover = new ImageBrush(temp);

            temp = sourceConverter.ConvertFromString(
                $"{ResourcePaths.AssemblyResourcePrefix}component/Resources/DS4-Config_Left.png") as ImageSource;
            ImageBrush leftHover = new ImageBrush(temp);

            hoverImages[crossConBtn] = crossHover;
            hoverImages[circleConBtn] = circleHover;
            hoverImages[squareConBtn] = squareHover;
            hoverImages[triangleConBtn] = triangleHover;
            hoverImages[l1ConBtn] = l1Hover;
            hoverImages[r1ConBtn] = r1Hover;
            hoverImages[l2ConBtn] = l2Hover;
            hoverImages[r2ConBtn] = r2Hover;
            hoverImages[shareConBtn] = shareHover;
            hoverImages[optionsConBtn] = optionsHover;
            hoverImages[guideConBtn] = guideHover;
            hoverImages[muteConBtn] = guideHover;

            hoverImages[leftTouchConBtn] = leftTouchHover;
            hoverImages[multiTouchConBtn] = multiTouchTouchHover;
            hoverImages[rightTouchConBtn] = rightTouchHover;
            hoverImages[topTouchConBtn] = topTouchHover;
            hoverImages[l3ConBtn] = l3Hover;
            hoverImages[lsuConBtn] = lsuHover;
            hoverImages[lsrConBtn] = lsrHover;
            hoverImages[lsdConBtn] = lsdHover;
            hoverImages[lslConBtn] = lslHover;
            hoverImages[r3ConBtn] = r3Hover;
            hoverImages[rsuConBtn] = rsuHover;
            hoverImages[rsrConBtn] = rsrHover;
            hoverImages[rsdConBtn] = rsdHover;
            hoverImages[rslConBtn] = rslHover;

            hoverImages[upConBtn] = upHover;
            hoverImages[rightConBtn] = rightHover;
            hoverImages[downConBtn] = downHover;
            hoverImages[leftConBtn] = leftHover;

            hoverImages[fnlConBtn] = guideHover;
            hoverImages[fnrConBtn] = guideHover;
            hoverImages[blpConBtn] = guideHover;
            hoverImages[brpConBtn] = guideHover;

            App.logHolder?.Logger?.Debug($"[PopulateHoverImages] loaded hoverImages count={hoverImages.Count}");
        }
    }
}
