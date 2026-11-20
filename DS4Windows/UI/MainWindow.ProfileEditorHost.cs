using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.ComponentModel;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Utilities;
using DS4Windows.Translations;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>プロファイル編集画面の表示と、プロファイルの複製・名前の変更。</summary>
    public partial class MainWindow
    {
        internal void EditDeviceProfile(int idx)
        {
            controllerLV.SelectedIndex = idx;
            CompositeDeviceModel? item = conLvViewModel.CurrentItem;

            if (item != null && item.SelectedIndex != -1)
            {
                ProfileEntity entity = profileListHolder.ProfileListCol[item.SelectedIndex];
                ShowProfileEditor(idx, entity);
                mainTabCon.SelectedIndex = 1;
            }
        }

        internal void DuplicateProfile()
        {
            string filename = "";
            if (profilesListBox.SelectedIndex >= 0)
            {
                int idx = profilesListBox.SelectedIndex;
                filename = profileListHolder.ProfileListCol[idx].Name ?? string.Empty;
                dupBox.OldFilename = filename;
                mainWinVM.IsDuplicateBarVisible = true;
                dupBox.Save -= DupBox_Save;
                dupBox.Cancel -= DupBox_Cancel;
                dupBox.Save += DupBox_Save;
                dupBox.Cancel += DupBox_Cancel;
            }
        }

        private void ProfileEditor_Closed(object? sender, EventArgs e)
        {
            profDockPanel.Children.Remove(editor);
            profOptsToolbar.Visibility = Visibility.Visible;
            profilesListBox.Visibility = Visibility.Visible;
            // preserveSizeの操作は不要
            if (editor == null || !editor.Keepsize)
            {
                this.Width = oldSize.Width;
                this.Height = oldSize.Height;
            }
            else
            {
                oldSize = new Size(Width, Height);
                var logical = WindowPlacementHelper.GetLogicalPlacement(this);
                layout.FormWidth = logical.LogicalWidth;
                layout.FormHeight = logical.LogicalHeight;
                layout.FormLocationX = logical.LogicalX;
                layout.FormLocationY = logical.LogicalY;
                AppLogger.LogTrace($"MainWindow.ProfileEditor_Closed: Saved Logical Rect X={layout.FormLocationX} Y={layout.FormLocationY} W={layout.FormWidth} H={layout.FormHeight}");
            }
            editor = null;
            // Restore the tab that was active before opening the profile editor
            try
            {
                if (previousTabIndex >= 0 && previousTabIndex < mainTabCon.Items.Count)
                    mainTabCon.SelectedIndex = previousTabIndex;
                else
                    mainTabCon.SelectedIndex = 0;
            }
            catch
            {
                mainTabCon.SelectedIndex = 0;
            }
            mainWinVM.FullTabsEnabled = true;
            //Task.Run(() => GC.Collect(0, GCCollectionMode.Forced, false));
        }

        /// <summary>
        /// プロファイル編集画面を開く。
        /// 編集は常に作業スロット（Global.TEST_PROFILE_INDEX）に対して行い、保存・適用を押すまで
        /// コントローラーには反映しない。編集スロットは ProfileEditor の内部で決まり、呼び出し元は指定しない。
        /// </summary>
        /// <param name="targetDevice">
        /// Edit／New Profile ボタンを押したコントローラーのスロット番号（ランブルテスト・ライトバーのプレビュー・校正・適用先）。
        /// プロファイル一覧から開いた場合は ProfileEditor.NoTargetDevice
        /// </param>
        /// <param name="entity">編集するプロファイル。null は新規作成</param>
        private void ShowProfileEditor(int targetDevice, ProfileEntity? entity)
        {
            if (editor == null)
            {
                // Remember current tab so we can restore it when editor closes
                try { previousTabIndex = mainTabCon.SelectedIndex; } catch { previousTabIndex = 0; }
                profOptsToolbar.Visibility = Visibility.Collapsed;
                profilesListBox.Visibility = Visibility.Collapsed;
                mainWinVM.FullTabsEnabled = false;

                // 編集画面を開く前のサイズを保存（閉じる時に元に戻すため）
                oldSize.Width = Width;
                oldSize.Height = Height;

                // 【仕様】プロフィール編集画面は開く直前のメインウィンドウと同じサイズ・ポジションで開く
                // サイズ変更処理は不要（以前の最小サイズチェックロジックは無効化）
                // if (this.Width < WindowLayoutDefaults.PROFILE_EDITOR_WIDTH)
                // {
                //     this.Width = WindowLayoutDefaults.PROFILE_EDITOR_WIDTH;
                // }
                //
                // if (this.Height < WindowLayoutDefaults.PROFILE_EDITOR_HEIGHT)
                // {
                //     this.Height = WindowLayoutDefaults.PROFILE_EDITOR_HEIGHT;
                // }

                editor = profileSelection.CreateEditor(targetDevice);
                editor.ProfileSaved += Editor_ProfileSaved;
                editor.ProfilesUpdatedExternally += Editor_ProfilesUpdatedExternally;
                editor.Closed += ProfileEditor_Closed;
                profDockPanel.Children.Add(editor);
                editor.Reload(entity);

                // When the profile editor is opened, emit missing-action logs once
                // per editor-open. We temporarily clear the per-load suppression set
                // so the same missing-action messages will be logged every time the
                // editor is opened (user expectation).
                profileSelection.EmitMissingActionLogsForEditor(entity);
            }

        }

        /// <summary>
        /// プロファイル編集画面で [保存] または [適用] が実行された際のコールバック。
        /// 安全な4段階同期メソッドを経由することで、クラッシュを防ぎつつリスト同期とホットリロードを一括実行します。
        /// </summary>
        private void Editor_ProfileSaved(ProfileEditor sender, string profile)
        {
            // 保存されたプロファイルを対象として、安全にリスト再同期＆該当スロットのホットリロードを実行
            SyncProfileListAndControllers(oldProfile: profile, newProfile: null);
        }

        internal void RenameProfile()
        {
            if (profilesListBox.SelectedIndex >= 0)
            {
                int idx = profilesListBox.SelectedIndex;
                ProfileEntity entity = profileListHolder.ProfileListCol[idx];
                string filename = Path.Combine(mainWinVM.AppDataPath,
                    "Profiles", $"{entity.Name}.xml");

                // Disallow renaming Default profile
                if (entity.Name != "Default" &&
                    File.Exists(filename))
                {
                    RenameProfileWindow renameWin = new RenameProfileWindow(_viewModelFactory.CreateRenameProfileViewModel());
                    renameWin.ChangeProfileName(entity.Name ?? string.Empty);
                    bool? result = renameWin.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        string oldProfileName = entity.Name ?? string.Empty;
                        string? newProfileName = renameWin.RenameProfileVM.ProfileName;
                        entity.RenameProfile(newProfileName ?? string.Empty);
                        // 名前変更を、全スロットの現在のプロファイル名・リンクプロファイルへ反映し、再適用する
                        // （従来はファイルと一覧の名前だけを変えていたため、サービスの再開後に Selected Profile が空欄になった）
                        if (string.Equals(entity.Name, newProfileName, StringComparison.Ordinal) &&
                            !string.Equals(oldProfileName, newProfileName, StringComparison.Ordinal))
                        {
                            SyncProfileListAndControllers(oldProfile: oldProfileName, newProfile: newProfileName);
                        }

                        trayIconVM.PopulateContextMenu();
                    }
                }
            }
        }
    }
}
