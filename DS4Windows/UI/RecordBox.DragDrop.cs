using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>手順の一覧のドラッグ・ドロップ。</summary>
    public partial class RecordBox
    {
        private static bool IsOnScrollBar(DependencyObject? source)
        {
            for (DependencyObject? current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is System.Windows.Controls.Primitives.ScrollBar) return true;
            }

            return false;
        }

        private void MacroListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            pendingCollapseItem = null;
            if (recordBoxVM.Recording || IsOnScrollBar(e.OriginalSource as DependencyObject)) return;

            ListBoxItem? item = ItemUnder(e.OriginalSource as DependencyObject);
            if (item == null)
            {
                // コマンドのない行（空白）のクリック: 選択を外す（Record／Insert は、いちばん下に足す）
                if (recordBoxVM.EditMacroIndex > -1) UpdateDataRevertTemplate();
                macroListBox.UnselectAll();
                macroListBox.Focus();
                return;
            }

            dragStartPoint = e.GetPosition(macroListBox);
            bool plain = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None;
            if (plain && item.IsSelected && macroListBox.SelectedItems.Count > 1)
            {
                // 複数選択の行を押した: そのままドラッグできるよう、選択をまだ 1 行にしない（ドラッグせずに離したら、その 1 行だけにする）
                pendingCollapseItem = item;
                macroListBox.Focus();
                e.Handled = true;
            }
        }

        private void MacroListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || dragStartPoint == null || recordBoxVM.Recording || recordBoxVM.EditMacroIndex > -1) return;

            Vector moved = e.GetPosition(macroListBox) - dragStartPoint.Value;
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            dragStartPoint = null;
            pendingCollapseItem = null;
            int[] indexes = macroListBox.SelectedItems.Cast<MacroStepItem>().Select(item => recordBoxVM.MacroSteps.IndexOf(item))
                .Where(i => i >= 0).OrderBy(i => i).ToArray();
            if (indexes.Length == 0) return;

            DragDrop.DoDragDrop(macroListBox, new DataObject(DragFormat, indexes), DragDropEffects.Move);
        }

        private void MacroListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            if (pendingCollapseItem != null)
            {
                // ドラッグせずに離した: 押した行だけの選択にする
                ListBoxItem item = pendingCollapseItem;
                pendingCollapseItem = null;
                macroListBox.SelectedItem = item.DataContext;
            }
        }

        // ドロップ先: 行の上半分なら、その行の前。下半分なら、その行の次の前。行のない所なら、いちばん下
        private int DropTargetIndex(DragEventArgs e)
        {
            ListBoxItem? item = ItemUnder(e.OriginalSource as DependencyObject);
            if (item == null) return recordBoxVM.MacroSteps.Count;

            int index = macroListBox.ItemContainerGenerator.IndexFromContainer(item);
            if (index < 0) return recordBoxVM.MacroSteps.Count;

            return e.GetPosition(item).Y > item.ActualHeight / 2 ? index + 1 : index;
        }

        private void MacroListBox_Drop(object sender, DragEventArgs e)
        {
            if (recordBoxVM.Recording || !e.Data.GetDataPresent(DragFormat)) return;

            if (recordBoxVM.EditMacroIndex > -1) UpdateDataRevertTemplate();
            int[] sources = (int[])e.Data.GetData(DragFormat);
            int[]? moved = recordBoxVM.MoveSteps(sources, DropTargetIndex(e));
            if (moved != null)
            {
                // 移した行を、そのまま選択しておく
                macroListBox.UnselectAll();
                foreach (int index in moved)
                {
                    macroListBox.SelectedItems.Add(recordBoxVM.MacroSteps[index]);
                }

                macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[moved[0]]);
            }

            e.Handled = true;
        }
    }
}
