using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI
{
    /// <summary>ウィンドウの大きさ・分割線・列の幅の保存と復元。</summary>
    public partial class ProfileEditor
    {
        // ウィンドウサイズ初期化ボタンのクリックイベント
        // すべての初期値は WindowLayoutDefaults から取得される（BackingStore経由）
        private void ResetWindowSize()
        {
            // 初期値にリセット（左右領域・列幅）
            // これらは WindowLayoutDefaults.PROFILE_EDITOR_* の値を参照している
            appSettingsService.ProfileEditorLeftWidth = BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
            appSettingsService.ProfileEditorRightWidth = BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
            appSettingsService.SpecialActionNameColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
            appSettingsService.SpecialActionTriggerColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
            appSettingsService.SpecialActionDetailColWidth = BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

            // ウィンドウ自体のサイズも初期値にリセット（ポジションは変更しない）
            // これらは WindowLayoutDefaults.MAIN_WINDOW_* の値を参照している
            appSettingsService.FormWidth = BackingStore.DEFAULT_FORM_WIDTH;
            appSettingsService.FormHeight = BackingStore.DEFAULT_FORM_HEIGHT;

            var win = Window.GetWindow(this);
            if (win != null)
            {
                // WindowLayoutDefaultsの値は論理ピクセルの初期値
                // WPFのWidthプロパティも論理ピクセルなので、そのまま代入
                win.Width = BackingStore.DEFAULT_FORM_WIDTH;
                win.Height = BackingStore.DEFAULT_FORM_HEIGHT;
            }

            // UIにも即時反映
            RestoreSplitterAndColumnWidths();
        }

        private void SaveSplitterAndColumnWidths()
        {
            var grid = this.Content as Grid ?? this.FindName("baseGrid") as Grid;
            if (grid == null || grid.ColumnDefinitions.Count < 3) return;

            double leftWidth = grid.ColumnDefinitions[0].Width.Value;
            double rightWidth = grid.ColumnDefinitions[2].Width.Value;

            // Special Actions ListViewのGridViewColumn幅取得
            var specialActionsLV = this.FindName("specialActionsLV") as System.Windows.Controls.ListView;
            if (specialActionsLV?.View is GridView gridView && gridView.Columns.Count >= 4)
            {
                try
                {
                    // IAppSettingsService 経由で保存
                    appSettingsService.ProfileEditorLeftWidth = (int)leftWidth;
                    appSettingsService.ProfileEditorRightWidth = (int)rightWidth;
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    appSettingsService.SpecialActionNameColWidth = (int)gridView.Columns[1].Width;
                    appSettingsService.SpecialActionTriggerColWidth = (int)gridView.Columns[2].Width;
                    appSettingsService.SpecialActionDetailColWidth = (int)gridView.Columns[3].Width;
                    // 必要なら他の列も追加

                    AppLogger.Log(AppLogLevel.Debug, $"[SaveSplitterAndColumnWidths] Saved left={appSettingsService.ProfileEditorLeftWidth} right={appSettingsService.ProfileEditorRightWidth} nameCol={appSettingsService.SpecialActionNameColWidth} trigCol={appSettingsService.SpecialActionTriggerColWidth} detailCol={appSettingsService.SpecialActionDetailColWidth}");
                }
                catch (Exception ex)
                {
                    AppLogger.Log(AppLogLevel.Debug, $"[SaveSplitterAndColumnWidths] failed: {ex.Message}");
                }
            }
        }

        private void RestoreSplitterAndColumnWidths()
        {
            try
            {
                var grid = this.Content as Grid ?? this.FindName("baseGrid") as Grid;
                if (grid != null && grid.ColumnDefinitions.Count >= 3)
                {
                    double left = appSettingsService.ProfileEditorLeftWidth;
                    double right = appSettingsService.ProfileEditorRightWidth;
                    if (IsValidWidth(left))
                        grid.ColumnDefinitions[0].Width = new GridLength(left);
                    if (IsValidWidth(right))
                        grid.ColumnDefinitions[2].Width = new GridLength(right);
                }
                var specialActionsLV = this.FindName("specialActionsLV") as System.Windows.Controls.ListView;
                if (specialActionsLV?.View is GridView gridView && gridView.Columns.Count >= 4)
                {
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    // Active 列はプロファイルに永続化しない初期幅を毎回適用する（BackingStore の定数を使用）
                    gridView.Columns[0].Width = BackingStore.DEFAULT_SPECIAL_ACTION_ACTIVE_COL_WIDTH;

                    double nameW = appSettingsService.SpecialActionNameColWidth;
                    double trigW = appSettingsService.SpecialActionTriggerColWidth;
                    double detailW = appSettingsService.SpecialActionDetailColWidth;
                    // columns: 0=Active,1=Name,2=Trigger,3=Detail
                    if (IsValidWidth(nameW))
                        gridView.Columns[1].Width = nameW;
                    if (IsValidWidth(trigW))
                        gridView.Columns[2].Width = trigW;
                    if (IsValidWidth(detailW))
                        gridView.Columns[3].Width = detailW;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogLevel.Debug, $"[RestoreSplitterAndColumnWidths] failed: {ex.Message}");
            }
        }
    }
}
