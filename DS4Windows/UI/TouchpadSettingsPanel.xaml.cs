using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.Controls;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「タッチパッド」タブの中身。DataContext は <see cref="TouchpadSettingsViewModel"/>。
    /// 割り当て編集画面を開く操作は、編集画面の全体に関わるため、イベントで編集画面へ知らせる。
    /// 「ボタンの有効化方式」の部品は編集画面が持つため、出力モードに合わせた置き場所の選択だけをここで行う。
    /// </summary>
    public partial class TouchpadSettingsPanel : UserControl
    {
        private ContentControl activeTouchButtonDisplayControl;

        public TouchpadSettingsPanel()
        {
            InitializeComponent();
        }

        /// <summary>スワイプ（上下左右）のボタンが押された。引数は対象のコントロール。</summary>
        public event EventHandler<DS4Controls> SwipeControlClicked;

        // 同時ホバー表示の対応表（編集画面のホバー表）が使う
        public Button SwipeUpButton => swipeUpBtn;
        public Button SwipeDownButton => swipeDownBtn;
        public Button SwipeLeftButton => swipeLeftBtn;
        public Button SwipeRightButton => swipeRightBtn;

        /// <summary>反転を無効にするトリガーのメニューのチェックと表示を、保存されている値へ合わせる（プロファイルの読み込み・プリセットの適用のあと）。</summary>
        public void PopulateDisInvertMenu(TouchpadSettingsViewModel touchpad)
        {
            touchpad.PopulateTouchDisInver(touchDisInvertBtn.ContextMenu);
        }

        /// <summary>スワイプの 4 方向の割り当ての表示を、割り当ての一覧の各コントロールへ結ぶ。</summary>
        public void AssignSwipeAssociation(MappingListViewModel mappingList)
        {
            swipeUpLb.DataContext = mappingList.ControlMap[DS4Controls.SwipeUp];
            swipeDownLb.DataContext = mappingList.ControlMap[DS4Controls.SwipeDown];
            swipeLeftLb.DataContext = mappingList.ControlMap[DS4Controls.SwipeLeft];
            swipeRightLb.DataContext = mappingList.ControlMap[DS4Controls.SwipeRight];
        }

        /// <summary>
        /// 「ボタンの有効化方式」の部品を、出力モード（<see cref="TouchpadSettingsViewModel.TouchpadOutputIndex"/>）に応じた
        /// 有効なタブの置き場所へ置く。置く場所を持つ ContentControl は、モードごとのタブの中にある。
        /// </summary>
        public void PlaceTouchButtonControl(TouchButtonUserControl touchButtonUC, int outputIndex)
        {
            if (activeTouchButtonDisplayControl != null)
            {
                activeTouchButtonDisplayControl.Content = null;
                activeTouchButtonDisplayControl = null;
            }

            switch (outputIndex)
            {
                case 1:
                    touchContentControl2.Content = touchButtonUC;
                    activeTouchButtonDisplayControl = touchContentControl2;
                    break;
                case 2:
                    touchContentControl4.Content = touchButtonUC;
                    activeTouchButtonDisplayControl = touchContentControl4;
                    break;
                case 3:
                    break;

                case 0:
                default:
                    touchContentControl1.Content = touchButtonUC;
                    activeTouchButtonDisplayControl = touchContentControl1;
                    break;
            }
        }

        private void SwipeControlsButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SwipeControlClicked?.Invoke(this, (DS4Controls)Convert.ToInt32(btn.Tag));
        }

        private void TouchDisInvertBtn_Click(object sender, RoutedEventArgs e)
        {
            touchDisInvertBtn.ContextMenu.IsOpen = true;
        }

        private void TouchDisInvertMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TouchpadSettingsViewModel)DataContext).UpdateTouchDisInvert(touchDisInvertBtn.ContextMenu);
        }
    }
}
