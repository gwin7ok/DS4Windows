using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 画面の配置（ウィンドウの位置・大きさ、エディターの幅、一覧の列の幅）。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class LayoutSettings
    {
        public int profileEditorLeftWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        public int profileEditorRightWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        public int specialActionNameColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        public int specialActionTriggerColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public int specialActionDetailColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;
        public int controllerIndexColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
        public int controllerIdColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;
        public int controllerStatusColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
        public int controllerExclusiveColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public int controllerBatteryColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
        public int controllerSelectProfileColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public int controllerEditColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
        public int controllerLinkedProfileColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public int controllerLinkProfIdColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public int controllerCustomColorColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;
        public int outputSlotNumColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_NUM_COL_WIDTH;
        public int outputSlotInputNumColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_NUM_COL_WIDTH;
        public int outputSlotInputIdColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_ID_COL_WIDTH;
        public int outputSlotCurrentTypeColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH;
        public int outputSlotReservationColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_RESERVATION_COL_WIDTH;
        public int autoProfileNameColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_NAME_COL_WIDTH;
        public int autoProfilePathColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_PATH_COL_WIDTH;
        public int autoProfileTitleColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_TITLE_COL_WIDTH;
        public int logTimeColWidth { get; set; } = BackingStore.DEFAULT_LOG_TIME_COL_WIDTH;
        public int logDataColWidth { get; set; } = BackingStore.DEFAULT_LOG_DATA_COL_WIDTH;
        public int formWidth { get; set; } = BackingStore.DEFAULT_FORM_WIDTH;
        public int formHeight { get; set; } = BackingStore.DEFAULT_FORM_HEIGHT;
        public int formLocationX { get; set; } = BackingStore.DEFAULT_FORM_LOCATION_X;
        public int formLocationY { get; set; } = BackingStore.DEFAULT_FORM_LOCATION_Y;
    }
}
