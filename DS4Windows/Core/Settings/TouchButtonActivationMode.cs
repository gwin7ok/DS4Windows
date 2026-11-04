namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// タッチパッドのボタン（左・右・マルチ・上）を、いつ押したとみなすか（プロファイルの <c>TouchpadButtonMode</c>）。
    /// 保存は値の名前（<c>Click</c> など）で行う。値と順序、基底型（<c>ushort</c>）は変えないこと。
    /// Phase14b-Step2（R-04）: 旧 <c>DS4Windows.Processing.Mouse</c> のネスト型から、設定値の型としてここへ移した（規約 §3.4 R5）。
    /// </summary>
    public enum TouchButtonActivationMode : ushort
    {
        Click,
        Touch,
        Release,
    }
}
