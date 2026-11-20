using DS4Windows.UI.Controls;
/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for BindingWindow.xaml
    /// </summary>
    public partial class BindingWindow : Window
    {
        private Dictionary<Button, BindAssociation> associatedBindings =
            new Dictionary<Button, BindAssociation>();
        private Dictionary<int, Button> keyBtnMap = new Dictionary<int, Button>();
        private Dictionary<DS4Windows.Core.Model.X360Controls, Button> conBtnMap =
            new Dictionary<DS4Windows.Core.Model.X360Controls, Button>();
        private Dictionary<DS4Windows.Core.Model.X360Controls, Button> mouseBtnMap =
            new Dictionary<DS4Windows.Core.Model.X360Controls, Button>();
        private BindingWindowViewModel bindingVM;
        // 振動テストボタンの動作（画面に依存しない判断）
        private readonly ActionRumbleTestController rumbleTest;
        // Phase6-Step10-2-4b: Global.defaultButtonMapping／InverseRumbleMotors の置換先（必須引数）
        // Phase6-Step10-2-4a: RecordBox の生成を窓口へ依頼する（必須引数）
        private readonly DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory;
        // コントローラーの図のボタンは、ControllerVisualizerControl が配置表から生成する。元の名前のまま持つ
        private Button guideBtn, aBtn, bBtn, xBtn, yBtn, lbBtn, rbBtn, ltBtn, rtBtn, backBtn, startBtn, lsbBtn, lsuBtn, lsrBtn, lsdBtn, lslBtn, rsbBtn, rsuBtn, rsrBtn, rsdBtn, rslBtn, dpadUBtn, dpadRBtn, dpadDBtn, dpadLBtn, touchpadClickBtn;

        private Button highlightBtn;
        private ExposeMode expose;

        public enum ExposeMode : uint
        {
            // SA の Press Key 用（旧 ForPressToggle）。Default／Unbound を出さない（SA の Press Key に「Default」はない）。
            // Phase10-Step6 I5c: Extras は、SA に保存先（SpecialAction.outputExtras）ができたため、Control と同じく出す。
            //   2 つのモードの違いは Default／Unbound の表示だけ（Phase10-Step3-Plan.md §3.1 の最終形）
            SpecialAction,
            // Controls の割り当てエディターの Press Key 用（Phase10-Step3 W1）。Default／Unbound と Extras を出す。
            // どのモードでも、Record A Macro・Toggle・Scan Code は出さない（割り当てエディターで設定する）
            Control,
        }

        /// <param name="deviceNum">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="targetDevice">
        /// Phase6-Step7b: ランブルテスト・ライトバーのプレビュー・マクロ記録で使う実機のスロット番号。-1 は実機なし。
        /// 渡し忘れをコンパイルで検出するため必須引数とする（Phase6-Step7b-Plan.md 決定2）
        /// </param>
        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）

        public BindingWindow(int deviceNum, int targetDevice,
            DS4Windows.UI.Abstractions.IEditorWindowFactory editorWindowFactory,
            ActionRumbleTestController rumbleTest,
            BindingWindowViewModel viewModel,
            ExposeMode expose)
        {
            ArgumentNullException.ThrowIfNull(editorWindowFactory);
            this.editorWindowFactory = editorWindowFactory;
            InitCommands();
            InitializeComponent();
            InitVisualizerButtons();


            ArgumentNullException.ThrowIfNull(rumbleTest);
            this.rumbleTest = rumbleTest;
            this.expose = expose;
            ArgumentNullException.ThrowIfNull(viewModel);
            bindingVM = viewModel;
            DataContext = bindingVM;
            bindingVM.CloseRequested += (s, e) => Close();

            if (bindingVM.Control != DS4Windows.Core.Model.DS4Controls.None)
            {
                Title = $"Select action for {DS4Windows.Core.Model.DS4ControlNames.ds4inputNames[bindingVM.Control]}";
            }
            else
            {
                Title = "Select action";
            }

            guideBtn.Content = "";
            highlightImg.Visibility = Visibility.Hidden;
            highlightLb.Visibility = Visibility.Hidden;

            // コントローラー／ボタンの割り当ての初期化（SA のモードも Control のモードも同じ）
            InitInfoMaps();

            InitPickerFace(bindingVM.OutputType);

            bindingVM.ActionBinding = bindingVM.CurrentOutBind;
            if (expose == ExposeMode.Control)
            {
                // Phase10-Step3 W1: Default／Unbound（modePanel）と Extras（extrasSidePanel）は出す。
                // Record A Macro・Toggle・Scan Code（topOptsPanel）・マクロの表示は、どのモードでも、割り当てエディターで設定するため出さない
                recordMacroBtn.Visibility = Visibility.Collapsed;
                topOptsPanel.Visibility = Visibility.Collapsed;
                macroOnLb.Visibility = Visibility.Collapsed;
            }
            else if (expose == ExposeMode.SpecialAction)
            {
                // For Press/Toggle selection: show keyboard, mouse and controller areas,
                // but hide auxiliary UI panels (mode panel and record macro).
                // Phase10-Step6 I5c: Extras（extrasSidePanel）は、Controls のモードと同じく出す（SA に保存先ができたため。2 つのモードの違いは Default／Unbound［modePanel］の表示だけ）
                modePanel.Visibility = Visibility.Collapsed;
                recordMacroBtn.Visibility = Visibility.Collapsed;
                // Phase10-Step1 W4: Toggle（Switch Mode）と Scan Code は、Press Key タブで設定する。ダイアログは、値を通過させるだけ
                topOptsPanel.Visibility = Visibility.Collapsed;
            }

            ChangeForCurrentAction();
        }

        private void OutConBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            //string name = button.Tag.ToString();
            string name = GetControlString(button);
            highlightLb.Content = name;

            double left = Canvas.GetLeft(button);
            double top = Canvas.GetTop(button);

            Canvas.SetLeft(highlightImg, left + (button.Width / 2.0) - (highlightImg.Height / 2.0));
            Canvas.SetTop(highlightImg, top + (button.Height / 2.0) - (highlightImg.Height / 2.0));

            Canvas.SetLeft(highlightLb, left + (button.Width / 2.0) - (highlightLb.ActualWidth / 2.0));
            Canvas.SetTop(highlightLb, top - 30);

            highlightImg.Visibility = Visibility.Visible;
            highlightLb.Visibility = Visibility.Visible;
        }

        private string GetControlString(Button button)
        {
            // 種類がない（None）ときは、既定の種類の表記
            var names = DS4Windows.Core.Model.OutContTypeInfo.ButtonNames(bindingVM.OutputType)
                ?? DS4Windows.Core.Model.OutContTypeInfo.ButtonNames(DS4Windows.Core.Model.OutContTypeInfo.DefaultType);
            return names[associatedBindings[button].control];
        }

        private void OutConBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            highlightImg.Visibility = Visibility.Hidden;
            highlightLb.Visibility = Visibility.Hidden;
        }

        /// <summary>ユーザーが「Default」を選んだか（× で閉じたときは false）。判断は ViewModel が持つ。</summary>
        public bool DefaultPicked => bindingVM.DefaultPicked;

        private void ChangeForCurrentAction()
        {
            OutBinding bind = bindingVM.ActionBinding;
            topOptsPanel.DataContext = bind;

            if (expose == ExposeMode.Control)
            {
                extrasGB.DataContext = bind;
                modePanel.DataContext = bind;
                macroOnLb.DataContext = bind;
            }
            else if (expose == ExposeMode.SpecialAction)
            {
                // Phase10-Step6 I5c: SA のモードも Extras を出す。結びつけるのは Extras だけ（Default／Unbound と、マクロの表示は出さない）
                extrasGB.DataContext = bind;
            }

            FindCurrentHighlightButton();
        }

        /// <summary>選択中の出力の色。テーマの OutputSelectedColor（ライト: LimeGreen、ダーク: 白い文字が読める緑）。無ければ LimeGreen。</summary>
        private Brush SelectedBrush()
        {
            return TryFindResource("OutputSelectedBrush") as Brush ?? new SolidColorBrush(Colors.LimeGreen);
        }

        private void FindCurrentHighlightButton()
        {
            if (highlightBtn != null)
            {
                // テーマの既定の配色へ戻す（以前は SystemColors.ControlBrush を固定で入れていたため、ダークテーマで明るい灰色になった）
                highlightBtn.ClearValue(Button.BackgroundProperty);
            }

            OutBinding binding = bindingVM.ActionBinding;
            if (binding.outputType == OutBinding.OutType.Default)
            {
                DS4Windows.Core.Model.X360Controls defaultBind = bindingVM.DefaultMappingFor(binding.input);
                if (!OutBinding.IsMouseRange(defaultBind))
                {
                    if (conBtnMap.TryGetValue(defaultBind, out Button tempBtn))
                    {
                        OutConBtn_MouseEnter(tempBtn, null);
                        //tempBtn.Background = new SolidColorBrush(Colors.LimeGreen);
                    }
                }
                else
                {
                    if (mouseBtnMap.TryGetValue(defaultBind, out Button tempBtn))
                    {
                        tempBtn.Background = SelectedBrush();
                        highlightBtn = tempBtn;
                    }
                }
            }
            else if (binding.outputType == OutBinding.OutType.Button)
            {
                if (!binding.IsMouse())
                {
                    if (conBtnMap.TryGetValue(binding.control, out Button tempBtn))
                    {
                        OutConBtn_MouseEnter(tempBtn, null);
                        //tempBtn.Background = new SolidColorBrush(Colors.LimeGreen);
                    }
                }
                else
                {
                    if (mouseBtnMap.TryGetValue(binding.control, out Button tempBtn))
                    {
                        tempBtn.Background = SelectedBrush();
                        highlightBtn = tempBtn;
                    }
                }
            }
            else if (binding.outputType == OutBinding.OutType.Key)
            {
                if (keyBtnMap.TryGetValue(binding.outkey, out Button tempBtn))
                {
                    tempBtn.Background = SelectedBrush();
                    highlightBtn = tempBtn;
                }
            }
        }

        /// <summary>
        /// ボタンと割り当ての対応表を作る。XAML に置いたボタンは、<c>Command</c> のバインディングと <c>CommandParameter</c> から読み、
        /// コントローラーの図が生成するボタンは、<see cref="BindingWindowVisualizerButtons"/> の表から、コマンドを結ぶ。
        /// 対応表は、ホバーの表示と、選択中のボタンの強調に使う。
        /// </summary>
        private void InitInfoMaps()
        {
            foreach (Button button in LogicalButtons(this))
            {
                string path = BindingOperations.GetBinding(button, System.Windows.Controls.Primitives.ButtonBase.CommandProperty)?.Path?.Path;
                if (path == null || !(button.CommandParameter is string parameter)) continue;

                if (path.EndsWith(nameof(BindingWindowViewModel.SelectKeyCommand), StringComparison.Ordinal))
                {
                    associatedBindings.Add(button,
                        new BindAssociation() { outputType = BindAssociation.OutType.Key, outkey = BindingWindowViewModel.ParseKeyCode(parameter) });
                }
                else if (path.EndsWith(nameof(BindingWindowViewModel.SelectButtonCommand), StringComparison.Ordinal))
                {
                    associatedBindings.Add(button, ButtonAssociation(parameter));
                }
            }

            foreach ((string id, string control) in BindingWindowVisualizerButtons.Table)
            {
                Button button = conVisualizer[id];
                button.Command = bindingVM.SelectButtonCommand;
                button.CommandParameter = control;
                associatedBindings.Add(button, ButtonAssociation(control));
            }

            foreach (KeyValuePair<Button, BindAssociation> pair in associatedBindings)
            {
                Button button = pair.Key;
                BindAssociation binding = pair.Value;
                if (binding.outputType == BindAssociation.OutType.Button)
                {
                    if (!binding.IsMouse())
                    {
                        conBtnMap.Add(binding.control, button);
                    }
                    else
                    {
                        mouseBtnMap.Add(binding.control, button);
                    }
                }
                else if (binding.outputType == BindAssociation.OutType.Key)
                {
                    if (!keyBtnMap.ContainsKey(binding.outkey))
                    {
                        keyBtnMap.Add(binding.outkey, button);
                    }
                }
            }
        }

        private static BindAssociation ButtonAssociation(string controlName)
            => new BindAssociation()
            {
                outputType = BindAssociation.OutType.Button,
                control = (DS4Windows.Core.Model.X360Controls)Enum.Parse(typeof(DS4Windows.Core.Model.X360Controls), controlName),
            };

        private static IEnumerable<Button> LogicalButtons(DependencyObject root)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is Button button) yield return button;
                if (child is DependencyObject node)
                {
                    foreach (Button nested in LogicalButtons(node)) yield return nested;
                }
            }
        }

        private void InitPickerFace(DS4Windows.Core.Model.OutContType type)
        {
            var face = ControllerLayouts.ForOutputType(type);
            // 背景の画像のキーがないのは、XAML の既定の図（Xbox 360）のとき。差し替えない
            if (face.BackgroundImageKey == null) return;

            ImageSourceConverter sourceConverter = new ImageSourceConverter();
            ImageSource temp = sourceConverter.
                ConvertFromString($"{ResourcePaths.AssemblyResourcePrefix}component/Resources/{App.Current.FindResource(face.BackgroundImageKey)}") as ImageSource;
            conImageBrush.ImageSource = temp;

            // ボタンの位置・大きさ・タッチパッド クリックの表示は、配置表が持つ
            conVisualizer.Layout = face.Layout;
        }

        /// <summary>配置表から生成されたボタンを元の名前のフィールドに持ち、ホバーの処理を結び付ける</summary>
        private void InitVisualizerButtons()
        {
            Button Get(string id) => conVisualizer[id];
            guideBtn = Get("guideBtn");
            aBtn = Get("aBtn");
            bBtn = Get("bBtn");
            xBtn = Get("xBtn");
            yBtn = Get("yBtn");
            lbBtn = Get("lbBtn");
            rbBtn = Get("rbBtn");
            ltBtn = Get("ltBtn");
            rtBtn = Get("rtBtn");
            backBtn = Get("backBtn");
            startBtn = Get("startBtn");
            lsbBtn = Get("lsbBtn");
            lsuBtn = Get("lsuBtn");
            lsrBtn = Get("lsrBtn");
            lsdBtn = Get("lsdBtn");
            lslBtn = Get("lslBtn");
            rsbBtn = Get("rsbBtn");
            rsuBtn = Get("rsuBtn");
            rsrBtn = Get("rsrBtn");
            rsdBtn = Get("rsdBtn");
            rslBtn = Get("rslBtn");
            dpadUBtn = Get("dpadUBtn");
            dpadRBtn = Get("dpadRBtn");
            dpadDBtn = Get("dpadDBtn");
            dpadLBtn = Get("dpadLBtn");
            touchpadClickBtn = Get("touchpadClickBtn");

            foreach (Button button in conVisualizer.Buttons.Values)
            {
                button.MouseEnter += OutConBtn_MouseEnter;
                button.MouseLeave += OutConBtn_MouseLeave;
            }
        }

        private void TestRumbleBtn()
        {
            // 振動させるのは実機（targetDevice）。モーターの左右反転は、編集中の設定（編集スロット DeviceNum）の値を読む。
            // 判断は ActionRumbleTestController が行い、ここはボタンの文字と動作中の印だけを変える
            if (!bindingVM.HasTargetDevice) return;

            var outcome = rumbleTest.Toggle(bindingVM.TargetDevice, bindingVM.RumbleActive,
                bindingVM.ActionBinding.HeavyRumble, bindingVM.ActionBinding.LightRumble,
                bindingVM.InverseRumbleMotors);
            if (outcome == RumbleTestOutcome.None) return;

            bool started = outcome == RumbleTestOutcome.Started;
            bindingVM.RumbleActive = started;
            testRumbleBtn.Content = started ? Properties.Resources.StopText : Properties.Resources.TestText;
        }

        private void ExtrasColorChoosebtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            OutBinding actBind = bindingVM.ActionBinding;
            DS4Color tempcolor = actBind.ExtrasColorValue;
            dialog.colorPicker.SelectedColor = MediaColors.ToMedia(tempcolor);
            bindingVM.StartForcedColor(tempcolor);
            dialog.ColorChanged += (sender2, color) =>
            {
                bindingVM.UpdateForcedColor(MediaColors.ToDS4(color));
            };
            dialog.ShowDialog();
            bindingVM.EndForcedColor();
            actBind.UpdateExtrasColor(MediaColors.ToDS4(dialog.colorPicker.SelectedColor.GetValueOrDefault()));
        }

        private void RecordMacroBtn()
        {
            RecordBox box = editorWindowFactory.CreateRecordBox(bindingVM.DeviceNum, bindingVM.Settings,
                bindingVM.TargetDevice);
            box.Visibility = Visibility.Visible;
            mapBindingPanel.Visibility = Visibility.Collapsed;
            extrasSidePanel.Visibility = Visibility.Collapsed;
            fullPanel.Children.Add(box);
            box.Cancel += (sender2, args) =>
            {
                box.Visibility = Visibility.Collapsed;
                fullPanel.Children.Remove(box);
                box = null;
                mapBindingPanel.Visibility = Visibility.Visible;
                extrasSidePanel.Visibility = Visibility.Visible;
            };

            box.Save += (sender2, args) =>
            {
                box.Visibility = Visibility.Collapsed;
                fullPanel.Children.Remove(box);
                box = null;
                //mapBindingPanel.Visibility = Visibility.Visible;
                bindingVM.PrepareSaveMacro(bindingVM.ActionBinding);
                Close();
            };
        }

        private void UnregisterDataContext()
        {
            topOptsPanel.DataContext = null;
            extrasGB.DataContext = null;
            modePanel.DataContext = null;
            macroOnLb.DataContext = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bindingVM.WriteBinds();

            UnregisterDataContext();
        }
    }
}