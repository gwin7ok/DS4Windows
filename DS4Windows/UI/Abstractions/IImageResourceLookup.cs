#nullable enable

namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 画像の資源名の引き当て。画像の名前はテーマで差し替わるため、画面（アプリの資源）から引く。
    /// ViewModel が <c>Application</c> の資源を直接参照せず、WPF なしでテストできるようにするための契約。
    /// </summary>
    public interface IImageResourceLookup
    {
        /// <summary>資源のキーに対応する画像のファイル名。</summary>
        string Find(string key);
    }
}
