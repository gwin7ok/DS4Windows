namespace DS4Windows.Core.Settings
{
    /// <summary>設定の選択肢 1 つ: 画面に出す名前と、保存される値（列挙型の名前）。</summary>
    public sealed class SettingChoice
    {
        public SettingChoice(string display, string value)
        {
            Display = display;
            Value = value;
        }

        public string Display { get; }

        public string Value { get; }
    }
}
