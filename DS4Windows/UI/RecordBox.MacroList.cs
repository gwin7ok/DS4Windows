using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>手順の一覧の選択・編集の表示。</summary>
    public partial class RecordBox
    {
        private void MacroSteps_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    int count = recordBoxVM.MacroSteps.Count;
                    if (count > 0)
                    {
                        macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[count - 1]);
                    }
                }));
            }
        }

        private void MacroListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 編集部品の中の ComboBox（マウス移動のモード）の SelectionChanged も、ここまで伝わってくる。
            // それで編集部品を確定・差し替えると、ComboBox の操作の最中に木が壊れてクラッシュするため、ListBox 自身の選択の変更だけを扱う
            if (!ReferenceEquals(e.OriginalSource, macroListBox)) return;

            // 挿入位置の基準は、選択された行のうち、いちばん上の行（選択がなければ -1＝いちばん下に足す）
            recordBoxVM.MacroStepIndex = RecordBoxViewModel.FirstIndexOf(
                macroListBox.SelectedItems.Cast<MacroStepItem>().Select(item => recordBoxVM.MacroSteps.IndexOf(item)));

            if (!recordBoxVM.Recording)
            {
                if (recordBoxVM.MacroStepIndex >= 0)
                {
                    MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.MacroStepIndex];
                    recordBtn.Content = $"Record Before {item.Step.Name}";
                }
                else
                {
                    recordBtn.Content = "Record";
                }

                if (recordBoxVM.EditMacroIndex > -1)
                {
                    UpdateDataRevertTemplate();
                }
            }
        }

        private void MacroListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (recordBoxVM.MacroStepIndex >= 0)
            {
                MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.MacroStepIndex];
                if (item.Step.ActType == DS4Windows.Processing.Macro.MacroStep.StepType.Wait)
                {
                    ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    if (lbitem != null) lbitem.ContentTemplate = this.FindResource("EditTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.Processing.Macro.MacroStep.StepOutput.PointerMove)
                {
                    // マウス移動の座標の編集
                    ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    if (lbitem != null) lbitem.ContentTemplate = this.FindResource("EditPointerMoveTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.Processing.Macro.MacroStep.StepOutput.Rumble &&
                    item.Step.ActType == DS4Windows.Processing.Macro.MacroStep.StepType.ActDown)
                {
                    ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    if (lbitem != null) lbitem.ContentTemplate = this.FindResource("EditRumbleTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.Processing.Macro.MacroStep.StepOutput.Lightbar &&
                    item.Step.ActType == DS4Windows.Processing.Macro.MacroStep.StepType.ActDown)
                {
                    colorDialog = new ColorPickerWindow();
                    colorDialog.Owner = Application.Current.MainWindow;
                    DS4Color tempcolor = item.LightbarColorValue();
                    colorDialog.colorPicker.SelectedColor = MediaColors.ToMedia(tempcolor);
                    recordBoxVM.StartForcedColor(tempcolor);
                    colorDialog.ColorChanged += (sender2, color) =>
                    {
                        recordBoxVM.UpdateForcedColor(MediaColors.ToDS4(color));
                    };
                    colorDialog.ShowDialog();
                    recordBoxVM.EndForcedColor();
                    item.UpdateLightbarValue(MediaColors.ToDS4(colorDialog.colorPicker.SelectedColor.GetValueOrDefault()));

                    FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;
                    TraversalRequest request = new TraversalRequest(focusDirection);
                    UIElement? elementWithFocus = Keyboard.FocusedElement as UIElement;
                    elementWithFocus?.MoveFocus(request);
                }
            }
        }

        private void RevertListItemTemplate()
        {
            if (recordBoxVM.EditMacroIndex >= 0)
            {
                ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
                if (lbitem != null) lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
                recordBoxVM.EditMacroIndex = -1;
            }
        }

        private void UpdateDataRevertTemplate()
        {
            ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
            ContentPresenter? contentPresenter = UtilMethods.FindVisualChild<ContentPresenter>(lbitem);
            if (contentPresenter == null) return;
            DataTemplate oldDataTemplate = contentPresenter.ContentTemplate;

            MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.EditMacroIndex];
            if (item.Step.ActType == DS4Windows.Processing.Macro.MacroStep.StepType.Wait)
            {
                IntegerUpDown? integerUpDown = oldDataTemplate.FindName("waitIUD", contentPresenter) as IntegerUpDown;
                if (integerUpDown != null)
                {
                    BindingExpression bindExp = integerUpDown.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();
                }
            }
            else if (item.Step.OutputType == DS4Windows.Processing.Macro.MacroStep.StepOutput.PointerMove)
            {
                // モードを先に確定する（座標は、そのモードの範囲へ丸められる）
                ComboBox? modeBox = oldDataTemplate.FindName("pointerModeCb", contentPresenter) as ComboBox;
                IntegerUpDown? pointerX = oldDataTemplate.FindName("pointerXUD", contentPresenter) as IntegerUpDown;
                IntegerUpDown? pointerY = oldDataTemplate.FindName("pointerYUD", contentPresenter) as IntegerUpDown;
                modeBox?.GetBindingExpression(ComboBox.SelectedIndexProperty)?.UpdateSource();
                pointerX?.GetBindingExpression(IntegerUpDown.ValueProperty)?.UpdateSource();
                pointerY?.GetBindingExpression(IntegerUpDown.ValueProperty)?.UpdateSource();
            }
            else if (item.Step.OutputType == DS4Windows.Processing.Macro.MacroStep.StepOutput.Rumble)
            {
                IntegerUpDown? heavyRumble = oldDataTemplate.FindName("heavyRumbleUD", contentPresenter) as IntegerUpDown;
                IntegerUpDown? lightRumble = oldDataTemplate.FindName("lightRumbleUD", contentPresenter) as IntegerUpDown;
                if (heavyRumble != null && lightRumble != null)
                {
                    BindingExpression bindExp = heavyRumble.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();

                    bindExp = lightRumble.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();
                }
            }

            if (lbitem != null) lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
            recordBoxVM.EditMacroIndex = -1;
        }
    }
}
