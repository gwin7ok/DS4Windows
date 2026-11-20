#nullable enable

using System.Windows.Input;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 仮想キーコードの表示名（WPF の <see cref="Key"/> の名前）。割り当ての説明文の表示で使う。
    /// WPF のキーの型への依存は、ViewModel ではなく、ここ 1 か所に置く（ViewModel は WPF の型を参照しない）。
    /// </summary>
    public static class VirtualKeyNames
    {
        public static string Get(int virtualKey) => KeyInterop.KeyFromVirtualKey(virtualKey).ToString();
    }
}
