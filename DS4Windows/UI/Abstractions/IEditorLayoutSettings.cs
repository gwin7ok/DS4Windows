#nullable enable

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// プロファイル編集画面のレイアウト（左右の幅、Special Actions の列幅、ウィンドウの大きさ）と表示言語の保存値。
    /// 画面が設定のサービス（<c>IAppSettingsService</c>）を直接参照しないための、画面の側の契約。
    /// </summary>
    public interface IEditorLayoutSettings
    {
        /// <summary>UI の言語（カルチャ名）。</summary>
        string UseLang { get; }

        int ProfileEditorLeftWidth { get; set; }
        int ProfileEditorRightWidth { get; set; }
        int SpecialActionNameColWidth { get; set; }
        int SpecialActionTriggerColWidth { get; set; }
        int SpecialActionDetailColWidth { get; set; }

        int FormWidth { get; set; }
        int FormHeight { get; set; }
    }
}
