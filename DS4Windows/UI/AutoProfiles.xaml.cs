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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using DS4Windows.Core.Profiles;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for AutoProfiles.xaml
    /// </summary>
    public partial class AutoProfiles : UserControl
    {
        // Phase6-Step10-2-3: 本コントロールは MainWindow.xaml から生成されるため、引数なしのコンストラクタが必要。
        // 依存は Initialize で受け取る（MainWindow のコンストラクタが、AutoProfileHolder を読む前に 1 回だけ呼ぶ）。
        // 列幅の読み書きは、設定を持つ MainWindow が関数で渡す（View は設定のサービスを参照しない）
        private Func<(int name, int path, int title)> loadColumnWidths;
        private Action<int, int, int> saveColumnWidths;
        private DS4Windows.UI.Abstractions.IShellViewModelFactory viewModelFactory;
        protected String m_Profile;
        public const string steamCommx86Loc = @"C:\Program Files (x86)\Steam\steamapps\common";
        public const string steamCommLoc = @"C:\Program Files\Steam\steamapps\common";
        private string steamgamesdir;
        private AutoProfilesViewModel autoProfVM;
        private AutoProfileHolder autoProfileHolder;
        private ProfileList profileList;
        private bool autoDebug;

        public AutoProfileHolder AutoProfileHolder
        {
            get => autoProfileHolder;
            set => autoProfileHolder = value;
        }
        public AutoProfilesViewModel AutoProfVM { get => autoProfVM; }
        public bool AutoDebug { get => autoDebug; }
        public event EventHandler AutoDebugChanged;

        public AutoProfiles()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 値を受け取り、画面の初期化（保存先ファイルの設定、Steam フォルダの有無、AutoProfileHolder、
        /// 行数の調整）を行う。置換前はコンストラクタで行っていた処理で、順序は同じ。
        /// MainWindow のコンストラクタから、AutoProfileHolder を読む前に 1 回だけ呼ぶ。
        /// </summary>
        public void Initialize(string autoProfilesFile, string steamGamesDir, int slotLimit,
            AutoProfileHolder autoProfileHolder, DS4Windows.UI.Abstractions.IShellViewModelFactory viewModelFactory,
            Func<(int name, int path, int title)> loadColumnWidths, Action<int, int, int> saveColumnWidths)
        {
            if (this.viewModelFactory != null) throw new InvalidOperationException("AutoProfiles is already initialized.");
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            this.loadColumnWidths = loadColumnWidths ?? throw new ArgumentNullException(nameof(loadColumnWidths));
            this.saveColumnWidths = saveColumnWidths ?? throw new ArgumentNullException(nameof(saveColumnWidths));

            m_Profile = autoProfilesFile ?? throw new ArgumentNullException(nameof(autoProfilesFile));

            // Steam のゲームのフォルダが無い（null）ときは、メニューの項目を外す
            steamgamesdir = steamGamesDir;
            if (steamgamesdir == null)
                addProgramsBtn.ContextMenu.Items.Remove(steamMenuItem);

            // 監視に使われている唯一の Holder を共有する。独自に作ると、この画面での編集・保存が
            // バックグラウンドの監視（AutoProfileService.CheckProfiles）に反映されない二重インスタンスになる。
            this.autoProfileHolder = autoProfileHolder ?? throw new ArgumentNullException(nameof(autoProfileHolder));

            int currentRowCount = autoProfilesGrid.RowDefinitions.Count;
            if (currentRowCount > slotLimit)
            {
                for (int i = currentRowCount - 1; i >= slotLimit; i--)
                {
                    autoProfilesGrid.RowDefinitions.RemoveAt(i);
                }
            }
        }

        // 列幅の保存。Window Title の列は、自動の幅（NaN）のあいだは 0 として保存し、復元でも自動のままにする
        private void ProgramListLV_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var gridView = programListLV.View as GridView;
            if (gridView == null || gridView.Columns.Count < 3 || saveColumnWidths == null) return;

            double title = gridView.Columns[2].Width;
            saveColumnWidths((int)gridView.Columns[0].Width, (int)gridView.Columns[1].Width, double.IsNaN(title) ? 0 : (int)title);
        }

        private void RestoreColumnWidths()
        {
            var gridView = programListLV.View as GridView;
            if (gridView == null || gridView.Columns.Count < 3) return;

            var widths = loadColumnWidths();
            gridView.Columns[0].Width = widths.name;
            gridView.Columns[1].Width = widths.path;
            if (widths.title > 0)
            {
                gridView.Columns[2].Width = widths.title;
            }
        }

        public void SetupDataContext(ProfileList profileList)
        {
            RestoreColumnWidths();
            autoProfVM = viewModelFactory.CreateAutoProfilesViewModel(autoProfileHolder, profileList);
            programListLV.DataContext = autoProfVM;
            programListLV.ItemsSource = autoProfVM.ProgramColl;

            revertDefaultProfileOnUnknownCk.DataContext = autoProfVM;

            autoProfVM.SearchFinished += AutoProfVM_SearchFinished;
            autoProfVM.CurrentItemChange += AutoProfVM_CurrentItemChange;

            //autoProfilesGrid.DataContext = autoProfVM;
            outerEditControlsPanel.DataContext = autoProfVM;
            editControlsPanel.DataContext = autoProfVM.SelectedItem;
            this.profileList = profileList;

            // Sort auto profile list by application file name
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(programListLV.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Filename", ListSortDirection.Ascending));
            view.Refresh();

            sidebarScrollViewer.ScrollToTop();
        }

        private void AutoProfVM_CurrentItemChange(AutoProfilesViewModel sender, ProgramItem item)
        {
            if (item != null)
            {
                // 保存されているプロファイル名を、各コントローラーの選択位置へ反映する（ViewModel が決める）
                sender.LoadSelectedIndexes(item);

                editControlsPanel.DataContext = item;
                editControlsPanel.IsEnabled = true;
            }
            else
            {
                editControlsPanel.DataContext = null;
                editControlsPanel.IsEnabled = false;

                cont1AutoProf.SelectedIndex = 0;
                cont2AutoProf.SelectedIndex = 0;
                cont3AutoProf.SelectedIndex = 0;
                cont4AutoProf.SelectedIndex = 0;
                cont5AutoProf.SelectedIndex = 0;
                cont6AutoProf.SelectedIndex = 0;
                cont7AutoProf.SelectedIndex = 0;
                cont8AutoProf.SelectedIndex = 0;
            }
        }

        private void AutoProfVM_SearchFinished(object sender, EventArgs e)
        {
            IsEnabled = true;
        }

        private void SteamMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            steamMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromSteam(steamgamesdir);
        }

        private void BrowseProgsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                //browseProgsMenuItem.Visibility = Visibility.Collapsed;
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramsFromDir(dialog.SelectedPath);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void StartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            startMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromStartMenu();
        }

        private void AppsSearchFinished(object sender, EventArgs e)
        {
            autoProfVM.SearchFinished -= AppsSearchFinished;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void AddProgramsBtn_Click(object sender, RoutedEventArgs e)
        {
            addProgramsBtn.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void HideUncheckedBtn_Click(object sender, RoutedEventArgs e)
        {
            programListLV.ItemsSource = null;
            autoProfVM.RemoveUnchecked();
            steamMenuItem.Visibility = Visibility.Visible;
            startMenuItem.Visibility = Visibility.Visible;
            browseProgsMenuItem.Visibility = Visibility.Visible;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void ShowAutoDebugCk_Click(object sender, RoutedEventArgs e)
        {
            bool state = showAutoDebugCk.IsChecked == true;
            autoDebug = state;
            AutoDebugChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                editControlsPanel.DataContext = null;
                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, false);
                autoProfVM.RemoveAutoProfileEntry(autoProfVM.SelectedItem);
                autoProfVM.AutoProfileHolder.Save(m_Profile);
                autoProfVM.SelectedItem = null;
            }
        }

        private void SaveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                if (autoProfVM.SelectedItem.MatchedAutoProfile == null)
                {
                    autoProfVM.CreateAutoProfileEntry(autoProfVM.SelectedItem);
                }
                else
                {
                    autoProfVM.PersistAutoProfileEntry(autoProfVM.SelectedItem);
                }

                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, autoProfVM.SelectedItem.Turnoff);
                autoProfVM.AutoProfileHolder.Save(m_Profile);
            }
        }

        private void BrowseAddProgMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Program (*.exe)|*.exe";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramExeLocation(dialog.FileName);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void MoveUpDownAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null && sender != null)
            {
                if (autoProfVM.MoveItemUpDown(autoProfVM.SelectedItem, ((sender as MenuItem).Name == "MoveUp") ? -1 : 1))
                    autoProfVM.AutoProfileHolder.Save(m_Profile);
            }
        }
    }
}