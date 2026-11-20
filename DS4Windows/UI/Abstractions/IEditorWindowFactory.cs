
#nullable enable

using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// プロファイル編集画面などから開く、補助ウィンドウ（ボタン割り当て、マクロ記録、プリセット、スティック校正、
    /// スペシャルアクション編集）を生成する窓口。
    /// 呼び出し側（<c>ProfileEditor</c> など）が <c>new</c> で生成し、必要なサービスを
    /// 連鎖して渡す代わりに、この窓口へ生成を依頼する。必要なサービスは実装（<c>EditorWindowFactory</c>）が必須引数で保持する。
    /// ウィンドウは UI スレッドで生成すること。
    /// </summary>
    public interface IEditorWindowFactory
    {
        /// <param name="deviceNum">設定を読み書きするスロット（プロファイル編集画面では編集用の作業スロット）</param>
        /// <param name="settings">編集対象のボタン設定</param>
        /// <param name="targetDevice">ランブルテスト・ライトバーのプレビュー・マクロ記録で使う実機のスロット番号。-1 は実機なし</param>
        BindingWindow CreateBindingWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            BindingWindow.ExposeMode expose);

        RecordBox CreateRecordBox(int deviceNum, DS4ControlSettings controlSettings, int targetDevice,
            bool showscan = true, bool repeatable = true, bool showPlaybackOptions = true);

        RecordBoxWindow CreateRecordBoxWindow(int deviceNum, DS4ControlSettings settings, int targetDevice,
            bool repeatable = true, bool showPlaybackOptions = true);

        PresetOptionWindow CreatePresetOptionWindow();

        StickCalibrationWindow CreateStickCalibrationWindow(Stick stick, int device,
            ProfileSettingsViewModel profileSettingsViewModel);

        AssignmentEditor CreateAssignmentEditor(int deviceNum, ProfileList profileList, int targetDevice,
            SpecialAction specialAction = null);

        /// <summary>一覧の行（<paramref name="item"/>）のスペシャルアクションを編集する画面。画面が設定の型を直接触らないための入口。</summary>
        AssignmentEditor CreateAssignmentEditorFor(int deviceNum, ProfileList profileList, int targetDevice, SpecialActionItem item);

        /// <summary>Controls の一覧の行（<paramref name="control"/>）の割り当てを編集する画面（Controls のモード）。</summary>
        AssignmentEditor CreateControlActionEditor(int deviceNum, MappedControl control, ProfileList profileList, int targetDevice);

        /// <summary>
        /// 共通の編集画面の、Controls のモード（Controls のボタン 1 つの割り当ての編集）。
        /// <c>ProfileEditor</c> の <c>ShowControlAssignmentEditor</c> が、従来の選択ダイアログ（<c>BindingWindow</c> の Full のモード）の代わりに、これを使う。
        /// </summary>
        AssignmentEditor CreateControlActionEditor(int deviceNum, DS4ControlSettings controlSettings, ProfileList profileList, int targetDevice);

        /// <summary>
        /// プロファイル編集画面（メイン画面に埋め込む）を生成する。
        /// 旧: <c>MainWindow</c> が <c>new ProfileEditor(...)</c> で生成し、<c>ProfileEditor</c> が <c>AppHost</c> から 4 つのサービスを解決していた。
        /// </summary>
        /// <param name="targetDevice">編集の対象にする実機のスロット番号。-1 は実機なし</param>
        ProfileEditor CreateProfileEditor(int targetDevice);
    }
}
