using DS4Windows.Core.Abstractions;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットの一般的な設定（ボタンによるマウス移動、プロファイルと一緒に起動するプログラム、無操作での切断、Bluetooth のポーリング間隔など）の窓口。
    /// <see cref="IProfileSettingsService"/> のスロット番号の配列を、スロット番号を渡さずに読み書きできるようにする
    /// （値は設定サービスの配列にあり、保存・読込の経路は変わらない）。
    /// </summary>
    public class GeneralSlotSettings
    {
        private readonly IProfileSettingsService _settings;
        private readonly int _slot;

        public GeneralSlotSettings(IProfileSettingsService settings, int slot)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _slot = slot;
        }

        /// <summary>ボタンによるマウス移動の設定（感度、縦の倍率、オフセット、加速。中身を直接書き換える）。</summary>
        public ButtonMouseInfo ButtonMouse => _settings.ButtonMouseInfos[_slot];

        /// <summary>コントローラーの入力を画面に表示する（スロットごとではなく、全体の設定）。</summary>
        public bool UseControllerReadout
        {
            get => _settings.DS4Mapping;
            set => _settings.DS4Mapping = value;
        }

        public bool EnableTouchToggle
        {
            get => _settings.EnableTouchToggle[_slot];
            set => _settings.EnableTouchToggle[_slot] = value;
        }

        public bool EnableOutputDataToDS4
        {
            get => _settings.EnableOutputDataToDS4[_slot];
            set => _settings.EnableOutputDataToDS4[_slot] = value;
        }

        /// <summary>プロファイルと一緒に起動するプログラムのパス（なければ空）。</summary>
        public string LaunchProgram
        {
            get => _settings.LaunchProgram[_slot];
            set => _settings.LaunchProgram[_slot] = value;
        }

        /// <summary>無操作で切断するまでの時間（秒。0 なら無効）。</summary>
        public int IdleDisconnectTimeoutSeconds
        {
            get => _settings.IdleDisconnectTimeout[_slot];
            set => _settings.IdleDisconnectTimeout[_slot] = value;
        }

        public int BTPollRate
        {
            get => _settings.BTPollRate[_slot];
            set => _settings.BTPollRate[_slot] = value;
        }
    }
}
