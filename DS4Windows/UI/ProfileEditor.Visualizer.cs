using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DS4Windows.UI
{
    /// <summary>コントローラーの図（ホバー・選択・読み取りの更新）。</summary>
    public partial class ProfileEditor
    {
        /// <summary>配置表から生成されたボタンを元の名前のフィールドに持ち、クリック・ホバー・右クリックの処理を結び付ける</summary>
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(crossConBtn), nameof(circleConBtn), nameof(squareConBtn), nameof(triangleConBtn), nameof(l1ConBtn), nameof(r1ConBtn), nameof(l2ConBtn), nameof(r2ConBtn), nameof(shareConBtn), nameof(optionsConBtn), nameof(guideConBtn), nameof(muteConBtn), nameof(fnlConBtn), nameof(fnrConBtn), nameof(blpConBtn), nameof(brpConBtn), nameof(leftTouchConBtn), nameof(multiTouchConBtn), nameof(rightTouchConBtn), nameof(topTouchConBtn), nameof(l3ConBtn), nameof(lsuConBtn), nameof(lsrConBtn), nameof(lsdConBtn), nameof(lslConBtn), nameof(r3ConBtn), nameof(rsuConBtn), nameof(rsrConBtn), nameof(rsdConBtn), nameof(rslConBtn), nameof(upConBtn), nameof(rightConBtn), nameof(downConBtn), nameof(leftConBtn))]
        private void InitVisualizerButtons()
        {
            crossConBtn = conCanvas["crossConBtn"];
            circleConBtn = conCanvas["circleConBtn"];
            squareConBtn = conCanvas["squareConBtn"];
            triangleConBtn = conCanvas["triangleConBtn"];
            l1ConBtn = conCanvas["l1ConBtn"];
            r1ConBtn = conCanvas["r1ConBtn"];
            l2ConBtn = conCanvas["l2ConBtn"];
            r2ConBtn = conCanvas["r2ConBtn"];
            shareConBtn = conCanvas["shareConBtn"];
            optionsConBtn = conCanvas["optionsConBtn"];
            guideConBtn = conCanvas["guideConBtn"];
            muteConBtn = conCanvas["muteConBtn"];
            fnlConBtn = conCanvas["fnlConBtn"];
            fnrConBtn = conCanvas["fnrConBtn"];
            blpConBtn = conCanvas["blpConBtn"];
            brpConBtn = conCanvas["brpConBtn"];
            leftTouchConBtn = conCanvas["leftTouchConBtn"];
            multiTouchConBtn = conCanvas["multiTouchConBtn"];
            rightTouchConBtn = conCanvas["rightTouchConBtn"];
            topTouchConBtn = conCanvas["topTouchConBtn"];
            l3ConBtn = conCanvas["l3ConBtn"];
            lsuConBtn = conCanvas["lsuConBtn"];
            lsrConBtn = conCanvas["lsrConBtn"];
            lsdConBtn = conCanvas["lsdConBtn"];
            lslConBtn = conCanvas["lslConBtn"];
            r3ConBtn = conCanvas["r3ConBtn"];
            rsuConBtn = conCanvas["rsuConBtn"];
            rsrConBtn = conCanvas["rsrConBtn"];
            rsdConBtn = conCanvas["rsdConBtn"];
            rslConBtn = conCanvas["rslConBtn"];
            upConBtn = conCanvas["upConBtn"];
            rightConBtn = conCanvas["rightConBtn"];
            downConBtn = conCanvas["downConBtn"];
            leftConBtn = conCanvas["leftConBtn"];

            conButtons = new[] { crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l1ConBtn, r1ConBtn, l2ConBtn, r2ConBtn, shareConBtn, optionsConBtn, guideConBtn, muteConBtn, leftTouchConBtn, multiTouchConBtn, rightTouchConBtn, topTouchConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn, fnlConBtn, fnrConBtn, blpConBtn, brpConBtn };

            // 大きさ 0 で隠れているボタンには、処理を結び付けない
            foreach (Button button in new[] { crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l1ConBtn, r1ConBtn, l2ConBtn, r2ConBtn, shareConBtn, optionsConBtn, guideConBtn, leftTouchConBtn, multiTouchConBtn, rightTouchConBtn, topTouchConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn })
            {
                button.Click += HoverConBtn_Click;
                button.MouseEnter += ContBtn_MouseEnter;
                button.MouseLeave += ContBtn_MouseLeave;
            }

            // プリセットのメニューを出すボタン（ABXY、スティック、十字キー）
            foreach (Button button in new[] { crossConBtn, circleConBtn, squareConBtn, triangleConBtn, l3ConBtn, lsuConBtn, lsrConBtn, lsdConBtn, lslConBtn, r3ConBtn, rsuConBtn, rsrConBtn, rsdConBtn, rslConBtn, upConBtn, rightConBtn, downConBtn, leftConBtn })
            {
                button.MouseRightButtonUp += ConBtn_MouseRightButtonUp;
            }
        }

        private Button[] conButtons = System.Array.Empty<Button>();

        private Button[] ConButtons => conButtons;

        private void RemoveHoverBtnText()
        {
            // 図のボタンには文字を出さない（割り当ての表示は、図の下のラベルに出す）。全ボタンの内容を空にそろえる
            foreach (Button button in ConButtons) button.Content = "";
        }

        private void HoverConBtn_Click(object sender, RoutedEventArgs e)
        {
            // 500ms の遅延の前にクリックされたときも、クリックしたボタンの割り当てを開く（選択を先に確定する）
            CancelHoverSelect();
            if (sender is Button clicked) SelectHoveredControl(clicked);

            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            ShowControlAssignmentEditor(mpControl, updateHighlight: true);
        }

        private void StartHoverSelect(Button control)
        {
            if (hoverSelectTimer == null)
            {
                hoverSelectTimer = new System.Timers.Timer(HoverSelectDelayMs) { AutoReset = false };
                hoverSelectTimer.Elapsed += (s, e) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Stop 済み（キャンセル）のあとに届いた Elapsed は、保留の選択がないので何もしない
                    Button? pending = hoverSelectPending;
                    hoverSelectPending = null;
                    if (pending != null) SelectHoveredControl(pending);
                }));
            }

            hoverSelectTimer.Stop();
            hoverSelectPending = control;
            hoverSelectTimer.Start();
        }

        private void HighlightHoverImage(Button control)
        {
            if (hoverImages.TryGetValue(control, out ImageBrush? tempBrush))
            {
                picBoxHover.Source = tempBrush.ImageSource;
                //picBoxHover.Width = tempBrush.ImageSource.Width * .8;
                //picBoxHover.Height = tempBrush.ImageSource.Height * .8;
                //control.Background = tempBrush;
                //control.Background = new SolidColorBrush(Colors.Green);
                //control.Width = tempBrush.ImageSource.Width;
                //control.Height = tempBrush.ImageSource.Height;
            }

            if (hoverLocations.TryGetValue(control, out HoverImageInfo? tempInfo))
            {
                Canvas.SetLeft(picBoxHover, tempInfo.point.X);
                Canvas.SetTop(picBoxHover, tempInfo.point.Y);
                picBoxHover.Width = tempInfo.size.Width;
                picBoxHover.Height = tempInfo.size.Height;
                picBoxHover.Stretch = Stretch.Fill;
                picBoxHover.Visibility = Visibility.Visible;
            }
        }

        private void SelectHoveredControl(Button control)
        {
            if (hoverIndexes.TryGetValue(control, out int tempIndex))
            {
                mappingListVM.SelectedIndex = tempIndex;
                mappingListBox.ScrollIntoView(mappingListBox.SelectedItem);
                MappedControl mapped = mappingListVM.Mappings[tempIndex];
                UpdateHighlightLabel(mapped);
            }
        }

        private void ContBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelHoverSelect();
            //Button control = sender as Button;
            //control.Background = new SolidColorBrush(Colors.Transparent);
            Canvas.SetLeft(picBoxHover, 0);
            Canvas.SetTop(picBoxHover, 0);
            picBoxHover.Visibility = Visibility.Hidden;
        }

        private void InputDS4(object? sender, System.Timers.ElapsedEventArgs e)
        {
            inputTimer?.Stop();

            bool activeWin = false;
            int tempDeviceNum = 0;
            Dispatcher.Invoke(() =>
            {
                activeWin = Application.Current.MainWindow.IsActive;
                tempDeviceNum = profileSettingsVM.FuncDevNum;
            });

            if (activeWin && profileSettingsVM.General.UseControllerReadout)
            {
                int index = -1;
                // 281行目で解決済みのcontrolServiceフィールドを使わず、
                // ここだけProgram.rootHubを直接呼んでいた置換漏れを修正。
                switch (backend.GetActiveInputControl(tempDeviceNum))
                {
                    case DS4Controls.None: break;
                    case DS4Controls.Cross: index = 0; break;
                    case DS4Controls.Circle: index = 1; break;
                    case DS4Controls.Square: index = 2; break;
                    case DS4Controls.Triangle: index = 3; break;
                    case DS4Controls.Options: index = 4; break;
                    case DS4Controls.Share: index = 5; break;
                    case DS4Controls.DpadUp: index = 6; break;
                    case DS4Controls.DpadDown: index = 7; break;
                    case DS4Controls.DpadLeft: index = 8; break;
                    case DS4Controls.DpadRight: index = 9; break;
                    case DS4Controls.PS: index = 10; break;
                    case DS4Controls.Mute: index = 11; break;
                    case DS4Controls.L1: index = 12; break;
                    case DS4Controls.R1: index = 13; break;
                    case DS4Controls.L2: index = 14; break;
                    case DS4Controls.R2: index = 15; break;
                    case DS4Controls.L3: index = 16; break;
                    case DS4Controls.R3: index = 17; break;
                    case DS4Controls.TouchLeft: index = 18; break;
                    case DS4Controls.TouchRight: index = 19; break;
                    case DS4Controls.TouchMulti: index = 20; break;
                    case DS4Controls.TouchUpper: index = 21; break;
                    case DS4Controls.LYNeg: index = 22; break;
                    case DS4Controls.LYPos: index = 23; break;
                    case DS4Controls.LXNeg: index = 24; break;
                    case DS4Controls.LXPos: index = 25; break;
                    case DS4Controls.RYNeg: index = 26; break;
                    case DS4Controls.RYPos: index = 27; break;
                    case DS4Controls.RXNeg: index = 28; break;
                    case DS4Controls.RXPos: index = 29; break;
                    case DS4Controls.FnL: index = 30; break;
                    case DS4Controls.FnR: index = 31; break;
                    case DS4Controls.BLP: index = 32; break;
                    case DS4Controls.BRP: index = 33; break;
                    default: break;
                }

                if (index >= 0 && leftPaneEditor == null)
                {
                    Dispatcher.BeginInvoke((System.Action)(() =>
                    {
                        if (leftPaneEditor != null) return;
                        mappingListVM.SelectedIndex = index;
                        ShowControlBindingWindow();
                    }));
                }
            }

            if (profileSettingsVM.General.UseControllerReadout)
            {
                inputTimer?.Start();
            }
        }
        private void ProfileEditor_Closed(object? sender, EventArgs e)
        {
            // 画面サイズ保持チェックが有効な場合のみレイアウト保存
            if (keepsize)
            {
                SaveSplitterAndColumnWidths();
            }
            profileSettingsVM.General.UseControllerReadout = false;
            inputTimer?.Stop();
            conReadingsUserCon.EnableControl(false);
            // Rebuild cached profile info for this device. CalculateProfileActionDicts
            // no longer emits missing-action logs directly; callers should invoke
            // EmitMissingActionLogsForDevice when they want messages emitted.
            try
            {
                profileSettingsVM.RefreshExtraProfileInfo();
            }
            catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[ProfileEditor:1377] ignored {caught.GetType().Name}: {caught.Message}"); }
            UnregisterEvents();
        }

        private void ConBtn_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Button? btn = sender as Button;
            MappedControl mpControl = mappingListVM.Mappings[mappingListVM.SelectedIndex];
            profileSettingsVM.PresetMenuUtil.SetHighlightControl(mpControl.Control);
            if (conCanvas.FindResource("presetMenu") is not ContextMenu cm) return;
            if (cm.Items[0] is MenuItem temp) temp.Header = profileSettingsVM.PresetMenuUtil.PresetInputLabel;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
        }
    }
}
