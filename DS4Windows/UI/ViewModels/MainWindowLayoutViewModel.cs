using System;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// メイン画面の配置（ウィンドウの位置・大きさ、各一覧の列幅）の ViewModel。設定の項目名との対応は、ここに 1 か所で持つ。
    /// 列幅は、画面の列の並びと同じ順の配列で受け渡す。
    /// </summary>
    public sealed class MainWindowLayoutViewModel
    {
        private readonly IAppSettingsService settings;

        public MainWindowLayoutViewModel(IAppSettingsService settings)
            => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public bool Save() => settings.Save();

        public int FormLocationX { get => settings.FormLocationX; set => settings.FormLocationX = value; }
        public int FormLocationY { get => settings.FormLocationY; set => settings.FormLocationY = value; }
        public int FormWidth { get => settings.FormWidth; set => settings.FormWidth = value; }
        public int FormHeight { get => settings.FormHeight; set => settings.FormHeight = value; }

        /// <summary>自動プロファイルの一覧の列幅（名前、パス、タイトル）。</summary>
        public (int Name, int Path, int Title) AutoProfileColumnWidths
        {
            get => (settings.AutoProfileNameColWidth, settings.AutoProfilePathColWidth, settings.AutoProfileTitleColWidth);
            set
            {
                settings.AutoProfileNameColWidth = value.Name;
                settings.AutoProfilePathColWidth = value.Path;
                settings.AutoProfileTitleColWidth = value.Title;
            }
        }

        /// <summary>出力スロットの一覧の列幅（番号、入力番号、入力 ID、現在の種類、予約）。</summary>
        public int[] OutputSlotColumnWidths
        {
            get => new[]
            {
                settings.OutputSlotNumColWidth, settings.OutputSlotInputNumColWidth, settings.OutputSlotInputIdColWidth,
                settings.OutputSlotCurrentTypeColWidth, settings.OutputSlotReservationColWidth,
            };
            set
            {
                settings.OutputSlotNumColWidth = value[0];
                settings.OutputSlotInputNumColWidth = value[1];
                settings.OutputSlotInputIdColWidth = value[2];
                settings.OutputSlotCurrentTypeColWidth = value[3];
                settings.OutputSlotReservationColWidth = value[4];
            }
        }

        /// <summary>ログの一覧の列幅（時刻、内容）。</summary>
        public int[] LogColumnWidths
        {
            get => new[] { settings.LogTimeColWidth, settings.LogDataColWidth };
            set
            {
                settings.LogTimeColWidth = value[0];
                settings.LogDataColWidth = value[1];
            }
        }

        /// <summary>コントローラー一覧の列幅（番号、ID、状態、排他、電池、プロファイル選択、編集、リンクのプロファイル、リンクの ID、カスタム色）。</summary>
        public int[] ControllerColumnWidths
        {
            get => new[]
            {
                settings.ControllerIndexColWidth, settings.ControllerIdColWidth, settings.ControllerStatusColWidth, settings.ControllerExclusiveColWidth,
                settings.ControllerBatteryColWidth, settings.ControllerSelectProfileColWidth, settings.ControllerEditColWidth,
                settings.ControllerLinkedProfileColWidth, settings.ControllerLinkProfIdColWidth, settings.ControllerCustomColorColWidth,
            };
            set
            {
                settings.ControllerIndexColWidth = value[0];
                settings.ControllerIdColWidth = value[1];
                settings.ControllerStatusColWidth = value[2];
                settings.ControllerExclusiveColWidth = value[3];
                settings.ControllerBatteryColWidth = value[4];
                settings.ControllerSelectProfileColWidth = value[5];
                settings.ControllerEditColWidth = value[6];
                settings.ControllerLinkedProfileColWidth = value[7];
                settings.ControllerLinkProfIdColWidth = value[8];
                settings.ControllerCustomColorColWidth = value[9];
            }
        }
    }
}
