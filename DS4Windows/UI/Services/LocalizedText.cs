using WPFLocalizeExtension.Engine;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// コードから、<c>Strings.resx</c> の文言を現在の表示言語で引く。XAML の <c>lex:Loc</c> と同じ辞書を使う。
    /// 見つからないとき（またはローカライズの辞書を使えない環境）は、キーそのものを返す。
    /// </summary>
    public static class LocalizedText
    {
        public static string Get(string key)
        {
            try
            {
                return LocalizeDictionary.Instance.GetLocalizedObject("DS4Windows", "Strings", key, LocalizeDictionary.Instance.Culture) as string ?? key;
            }
            catch
            {
                return key;
            }
        }
    }
}
