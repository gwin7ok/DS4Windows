using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>コントローラーの一覧（色・プロファイルの選択・列の幅）。</summary>
    public partial class MainWindow
    {
        private void ChangeControllerPanel()
        {
            if (conLvViewModel.ControllerCol.Count == 0)
            {
                controllerLV.Visibility = Visibility.Hidden;
                noContLb.Visibility = Visibility.Visible;
            }
            else
            {
                controllerLV.Visibility = Visibility.Visible;
                noContLb.Visibility = Visibility.Hidden;
            }
        }

        private void ControllerCol_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ChangeControllerPanel();
                System.Collections.IList? newitems = e.NewItems;
                if (newitems != null)
                {
                    foreach (CompositeDeviceModel item in newitems)
                    {
                        item.SubscribeSyncChange(DS4Device_SyncChange);
                        item.RequestColorPicker += Item_RequestColorPicker;
                        //item.LightContext.Items.Add(new MenuItem() { Header = "Use Profile Color", IsChecked = !item.UseCustomColor });
                        //item.LightContext.Items.Add(new MenuItem() { Header = "Use Custom Color", IsChecked = item.UseCustomColor });
                    }
                }

                if (mainWinVM.ServiceRunning)
                    trayIconVM.PopulateContextMenu();
            }));
        }

        private void Item_RequestColorPicker(CompositeDeviceModel sender)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = this;
            dialog.colorPicker.SelectedColor = MediaColors.ToMedia(sender.CustomLightColor);
            dialog.ColorChanged += (sender2, color) =>
            {
                sender.UpdateCustomLightColor(MediaColors.ToDS4(color));
            };
            dialog.ShowDialog();
        }

        private void ContStatusImg_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image img) return;
            int tag = Convert.ToInt32(img.Tag);
            conLvViewModel.CurrentIndex = tag;
            CompositeDeviceModel? item = conLvViewModel.CurrentItem;
            //CompositeDeviceModel item = conLvViewModel.ControllerDict[tag];
            if (item != null)
            {
                item.RequestDisconnect();
            }
        }

        private void IdColumnTxtB_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (sender is not TextBlock statusBk) return;
            int idx = Convert.ToInt32(statusBk.Tag);
            if (idx >= 0)
            {
                CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];
                item.RequestUpdatedTooltipID();
            }
        }

        /// <summary>
        /// Change profile based on selection
        /// </summary>
        private void SelectProfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox? box = sender as ComboBox;
            if (box == null) return;

            int idx = Convert.ToInt32(box.Tag);
            if (idx > -1 && conLvViewModel.ControllerDict.ContainsKey(idx))
            {
                CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];
                if (profileSelection.ApplySelection(idx, item))
                {
                    trayIconVM.PopulateContextMenu();
                }
            }
        }

        // ライトバーの色（プロファイルの色／カスタム色）を選ぶメニュー。開くたびに、いまの状態のチェックで作る
        private static ContextMenu BuildLightContext(CompositeDeviceModel item)
        {
            var menu = new ContextMenu();
            var profileItem = new MenuItem() { Header = "Use Profile Color", IsChecked = !item.UseCustomColor };
            profileItem.Click += (s, e) => item.UseProfileLightColor();
            var customItem = new MenuItem() { Header = "Use Custom Color", IsChecked = item.UseCustomColor };
            customItem.Click += (s, e) => item.UseCustomLightColor();
            menu.Items.Add(profileItem);
            menu.Items.Add(customItem);
            return menu;
        }

        private void LightColorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            int idx = Convert.ToInt32(button.Tag);
            CompositeDeviceModel item = conLvViewModel.ControllerDict[idx];
            //(button.ContextMenu.Items[0] as MenuItem).IsChecked = conLvViewModel.ControllerCol[idx].UseCustomColor;
            //(button.ContextMenu.Items[1] as MenuItem).IsChecked = !conLvViewModel.ControllerCol[idx].UseCustomColor;
            button.ContextMenu = BuildLightContext(item);
            button.ContextMenu.IsOpen = true;
        }

        private void SaveControllerColumnWidths()
        {
            var gridView = controllerLV.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 10)
            {
                int[] widths = new int[10];
                for (int i = 0; i < widths.Length; i++) widths[i] = (int)gridView.Columns[i].Width;
                layout.ControllerColumnWidths = widths;
            }
        }

        private void RestoreControllerColumnWidths()
        {
            var gridView = controllerLV.View as GridView;
            if (gridView != null && gridView.Columns.Count >= 10)
            {
                int[] widths = layout.ControllerColumnWidths;
                for (int i = 0; i < widths.Length; i++) gridView.Columns[i].Width = widths[i];
            }
        }
    }
}
