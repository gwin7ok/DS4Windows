using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// メイン画面（<see cref="MainWindow"/>）が読み書きする設定の窓口。画面が設定サービスの型を直接参照しない（V2）ようにする。
    /// 列幅は、画面の列の並びと同じ順の配列で受け渡し、設定の項目名との対応はここに 1 か所で持つ。
    /// </summary>
    public sealed class MainWindowSettings
    {
        private readonly IAppSettingsService settings;

        public MainWindowSettings(IAppSettingsService settings)
            => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public bool Save() => settings.Save();

        public string LogMinLevel => settings.LogMinLevel;
        public bool StartMinimized => settings.StartMinimized;
        public bool MinimizeToTaskbar => settings.MinimizeToTaskbar;
        public bool CloseMinimizes => settings.CloseMinimizes;
        public bool FirstRun => settings.FirstRun;
        public bool RunHotPlug => settings.RunHotPlug;
        public bool SwipeProfiles => settings.SwipeProfiles;
        public ulong LastVersionCheckedNum => settings.LastVersionCheckedNum;

        /// <summary>Steam のゲームのフォルダ。カスタムのフォルダが有効で存在するときだけ、その値（それ以外は null）。</summary>
        public string CustomSteamFolderIfValid
            => settings.UseCustomSteamFolder && System.IO.Directory.Exists(settings.CustomSteamFolder) ? settings.CustomSteamFolder : null;

        public int FormLocationX { get => settings.FormLocationX; set => settings.FormLocationX = value; }
        public int FormLocationY { get => settings.FormLocationY; set => settings.FormLocationY = value; }
        public int FormWidth { get => settings.FormWidth; set => settings.FormWidth = value; }
        public int FormHeight { get => settings.FormHeight; set => settings.FormHeight = value; }

        /// <summary>機器の登録の設定画面の ViewModel（設定の機器オプションを渡して作る）。</summary>
        public ControllerRegDeviceOptsViewModel CreateDeviceOptionsViewModel(IShellViewModelFactory factory)
            => factory.CreateControllerRegDeviceOptsViewModel(settings.DeviceOptions);

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
