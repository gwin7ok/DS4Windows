using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.UI.ViewModels;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DS4Windows.UI
{
    /// <summary>スペシャルアクションの一覧の操作。</summary>
    public partial class ProfileEditor
    {
        private void SortSpecialActionsList(string columnName, bool asc)
        {
            ListSortDirection sortDirection = asc ? ListSortDirection.Ascending : ListSortDirection.Descending;
            specialActionsVM.SortActions(columnName, sortDirection);

            var view = (CollectionView)CollectionViewSource.GetDefaultView(specialActionsVM.ActionCol);
            view?.Refresh();
            AppLogger.LogDebug($"[SpecialActions] Sorted: column={columnName}, asc={asc}, count={specialActionsVM.ActionCol.Count}");
        }

        // SA の行をダブルクリックすると、「アクションを編集」と同じ編集画面を開く（Controls タブの一覧のダブルクリックと同じ）。
        // ヘッダー、空きの領域、「有効」のチェックボックスのダブルクリックは対象外
        private void SpecialActionsLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is System.Windows.Controls.ListViewItem) && !(source is System.Windows.Controls.Primitives.ButtonBase))
            {
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            if (!(source is System.Windows.Controls.ListViewItem)) return;

            EditAction();
        }

        private void EditAction()
        {
            if (leftPaneEditor != null) return;
            if (specialActionsVM.SpecialActionIndex >= 0)
            {
                SpecialActionItem item = specialActionsVM.CurrentSpecialActionItem!;   // 選択があるときは、必ず項目がある
                int currentIndex = item.Index;
                //int viewIndex = specialActionsVM.SpecialActionIndex;
                //int currentIndex = specialActionsVM.ActionCol[viewIndex].Index;
                //SpecialActionItem item = specialActionsVM.ActionCol[currentIndex];
                ProfileList profList = (Application.Current.MainWindow as MainWindow)!.ProfileListHolder;   // 画面が開いているあいだ、メイン画面は必ずある
                AssignmentEditor actEditor = editorWindowFactory.CreateAssignmentEditorFor(deviceNum, profList, targetDevice, item);
                OpenLeftPaneEditor(actEditor);
                actEditor.Cancel += (sender2, args) =>
                {
                    CloseLeftPaneEditor();
                };
                actEditor.Saved += (sender2, actionName) =>
                {
                    string? oldName = item.OriginalName;
                    string newName = actionName;

                    // Update current profile's references from oldName -> newName
                    try
                    {
                        backend.RenameActionInEditedProfile(deviceNum, oldName, newName);
                        profileSettingsVM.RefreshExtraProfileInfo();
                    }
                    catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[ProfileEditor:1192] ignored {caught.GetType().Name}: {caught.Message}"); }

                    // 一覧の差し替えは UI のスレッドで行う
                    Application.Current.Dispatcher.Invoke(() => specialActionsVM.ReplaceEditedAction(item, currentIndex, newName));

                    CloseLeftPaneEditor();
                    // After editing an existing special action, re-apply the
                    // current sort so any name/type changes are reflected
                    // according to the current sort column and direction.
                    SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);

                    // 名前が変わったときは、編集中以外も含むすべてのプロファイルの XML の参照を
                    // 新名へ置換し、書き換えたプロファイルを使っているスロットへ再適用する（MainWindow の 1 か所の経路）。
                    // 編集中のプロファイルのメモリ上の参照は、上で置換済み。
                    if (!string.IsNullOrEmpty(oldName) && !string.Equals(oldName, newName, StringComparison.Ordinal))
                    {
                        NotifyProfilesUpdated(backend.ReplaceActionReference(oldName, newName));
                    }
                    else
                    {
                        // 名前が同じで内容だけ変わった場合は、プロファイルの XML の書き換えは不要。ただし、接続中のコントローラーは、
                        // プロファイルの適用時に取り込んだ古い内容のアクションを保持している（Actions.xml の読み直しでは更新されない）。
                        // そのアクションを使っているプロファイルを、保存時と同じ再適用の経路で再適用して、新しい内容を反映する。
                        NotifyProfilesUpdated(backend.GetAppliedProfilesUsingAction(newName));
                    }
                };
            }
        }

        // R-1: 選んだ SA を複製する。複製は無効（チェックなし）で一覧に加わる。保存（Actions.xml）は、複製の時点で行われる
        private void DuplicateAction()
        {
            if (leftPaneEditor != null) return;
            if (specialActionsVM.SpecialActionIndex < 0) return;

            if (!specialActionsVM.DuplicateSelectedWithNotice()) return;

            SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
        }

        private void RemoveAction()
        {
            if (specialActionsVM.SpecialActionIndex >= 0)
            {
                // 確認のうえ、Actions.xml と、すべてのプロファイルの XML から取り除く
                string? removedName = specialActionsVM.RemoveSelectedWithConfirm();
                if (removedName == null) return;

                NotifyProfilesUpdated(backend.RemoveActionReference(removedName));
            }
        }

        private void CommitEnabledActions()
        {
            // ViewModel 側の ProfileActions 文字列を即時再同期
            if (specialActionsVM != null)
            {
                specialActionsVM.CommitEnabledActionsToProfile();
            }
        }
        private void Ds4LightbarColorBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            highlightControlDisplayLb.Content = "Click the lightbar for color picker";
        }

        // 列ヘッダークリックイベント
        private void SortByColumn(string? col)
        {
            if (col != null)
            {
                string currentCol = specialActionsVM?.CurrentSortColumn ?? "Name";
                bool currentAsc = specialActionsVM?.CurrentSortAscending ?? true;
                bool asc = col != currentCol ? true : !currentAsc;
                SortSpecialActionsList(col, asc);
            }
        }
    }
}
