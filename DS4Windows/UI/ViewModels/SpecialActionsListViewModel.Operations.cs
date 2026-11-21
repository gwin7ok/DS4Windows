using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>スペシャルアクションの一覧の操作（削除・複製・書き出し・並べ替え・差し替え）。</summary>
    public partial class SpecialActionsListViewModel
    {
        public void SortActions(string propertyName, ListSortDirection direction)
        {
            List<SpecialActionItem> items = _sorter.Sort(actionCol, propertyName, direction);
            actionCol.Clear();
            foreach (var item in items)
            {
                actionCol.Add(item);
            }

            OnPropertyChanged(nameof(CurrentSortColumn));
            OnPropertyChanged(nameof(CurrentSortDirection));
            OnPropertyChanged(nameof(CurrentSortAscending));
            RefreshHeaderTexts();
        }

        /// <summary>
        /// 一覧からアクションを除去する。Actions.xml に存在するアクションは、Actions.xml からも削除して保存する
        /// （Phase6-Step10-3b-3: <see cref="ISpecialActionRepository.RemoveActionAndSave"/>。従来はメモリ上のリストから外すだけだった）。
        /// Actions.xml に無い名前（グレーの項目）は、一覧から外すだけ。プロファイルの XML からの参照の削除は呼び出し側が行う。
        /// </summary>
        public void RemoveAction(SpecialActionItem item)
        {
            if (item == null) return;

            if (!item.IsMissing)
            {
                specialActionRepo.RemoveActionAndSave(item.ActionName);
            }

            actionCol.Remove(item);

            // インデックス再割り振り
            for (int i = 0; i < actionCol.Count; i++)
            {
                actionCol[i].Index = i;
            }

            SyncProfileActionsString();
        }

        /// <summary>
        /// R-1: アクションを複製する（<see cref="ISpecialActionRepository.DuplicateAction"/>。Actions.xml へ保存される）。複製は、一覧の末尾に、
        /// <b>無効（チェックなし）</b>で追加する（同じトリガーの 2 つが、同時に有効になって二重に動くのを避ける。使うときは、チェックを入れる）。
        /// Actions.xml に無い項目（グレーの項目）は複製できない。
        /// </summary>
        /// <returns>追加した項目。複製できなかったときは null</returns>
        public SpecialActionItem? DuplicateAction(SpecialActionItem item)
        {
            if (item == null || item.IsMissing) return null;

            string? newName = specialActionRepo.DuplicateAction(item.ActionName);
            if (newName == null) return null;

            SpecialAction action = specialActionRepo.GetActionOrPlaceholder(newName);
            SpecialActionItem newItem = CreateActionItem(action);
            newItem.Active = false;
            actionCol.Add(newItem);
            return newItem;
        }

        /// <summary>Actions.xml を、選んだ保存先へコピーする（エクスポート）。</summary>
        /// <returns>コピーしたら true。取り消しなら false</returns>
        public bool PickAndExportActions()
        {
            string? file = dialogs.PickSaveFile(new FileDialogOptions
            {
                Filter = "DS4Windows Special Actions (*.xml)|*.xml",
                DefaultExtension = ".xml",
                FileName = "Actions",
                CheckFileExists = false,
            });
            if (file == null) return false;

            File.Copy(ActionsFilePath, file, true);
            return true;
        }

        /// <summary>
        /// 選んだ項目を、確認のうえ取り除く（Actions.xml も更新）。プロファイルの XML からの参照の削除は呼び出し側が行う。
        /// </summary>
        /// <returns>取り除いた項目の名前。選択なし・確認で「いいえ」のときは null</returns>
        public string? RemoveSelectedWithConfirm()
        {
            SpecialActionItem? item = specialActionIndex >= 0 ? currentSAItem : null;
            if (item == null) return null;

            if (!dialogs.Confirm(AppLogger.UserText("SpecialAction_RemoveConfirm", item.ActionName), "DS4Windows", DialogIcon.Question))
            {
                return null;
            }

            string removedName = item.ActionName;
            RemoveAction(item);
            return removedName;
        }

        /// <summary>選んだ項目を複製する。複製できない項目（Actions.xml に無い）のときは、通知の窓を出して false を返す。</summary>
        public bool DuplicateSelectedWithNotice()
        {
            SpecialActionItem? item = specialActionIndex >= 0 ? currentSAItem : null;
            if (item == null) return false;

            if (DuplicateAction(item) == null)
            {
                dialogs.ShowMessage(AppLogger.UserText("SpecialAction_DuplicateNotSaved", item.ActionName), "DS4Windows", DialogIcon.Information);
                return false;
            }

            return true;
        }

        /// <summary>割り当て編集画面で新しく保存したアクションを、有効の状態で一覧の末尾へ加える。</summary>
        public void AddSavedAction(string actionName)
        {
            SpecialActionItem newItem = CreateActionItem(GetActionOrPlaceholder(actionName));
            newItem.Active = true;
            newItem.Index = actionCol.Count;
            actionCol.Add(newItem);
        }

        /// <summary>
        /// 編集したアクションを、元の項目の位置へ差し替える（有効の状態は引き継ぐ）。
        /// 一覧が変わっていて元の項目が見つからないときは <paramref name="fallbackIndex"/> を使う。
        /// </summary>
        public void ReplaceEditedAction(SpecialActionItem oldItem, int fallbackIndex, string newName)
        {
            int idx = actionCol.IndexOf(oldItem);
            if (idx < 0) idx = fallbackIndex;

            SpecialActionItem newItem = CreateActionItem(GetActionOrPlaceholder(newName));
            newItem.Active = oldItem.Active;
            newItem.Index = idx;

            if (idx >= 0 && idx < actionCol.Count)
            {
                actionCol.RemoveAt(idx);
                actionCol.Insert(idx, newItem);
            }
            else
            {
                actionCol.Add(newItem);
            }
        }
    }
}
