#nullable enable

using System.Windows;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>アプリの資源（<c>Application.Current</c>）から、画像のファイル名を引く。</summary>
    public sealed class WpfImageResourceLookup : IImageResourceLookup
    {
        public string Find(string key) => (string)Application.Current.FindResource(key);
    }
}
