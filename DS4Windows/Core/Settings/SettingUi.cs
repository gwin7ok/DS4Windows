using System.Collections.Generic;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 設定項目の画面での見せ方: 表示名・説明のリソースキー（<c>Strings.resx</c>）、新しい接続からの反映か、列挙型の選択肢。
    /// 画面は、この定義から項目（チェックボックス、選択肢の一覧）を作る。
    /// </summary>
    public sealed class SettingUi
    {
        public SettingUi(string labelKey, string tooltipKey, bool appliesOnNewConnection, IReadOnlyList<SettingChoice> choices)
        {
            LabelKey = labelKey;
            TooltipKey = tooltipKey;
            AppliesOnNewConnection = appliesOnNewConnection;
            Choices = choices;
        }

        public string LabelKey { get; }

        public string TooltipKey { get; }

        /// <summary>変更が、新しいデバイスの接続から効くか（画面に注意書きを出す）。</summary>
        public bool AppliesOnNewConnection { get; }

        /// <summary>列挙型の選択肢（真偽値の項目は null）。</summary>
        public IReadOnlyList<SettingChoice> Choices { get; }
    }
}
