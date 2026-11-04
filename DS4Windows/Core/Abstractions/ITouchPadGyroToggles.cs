namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase14b-Step7（R-02）: プロファイルの読込（<c>BackingStore.SetGyro*</c>、<c>ResetMouseProperties</c>）が、スロットのタッチパッドのマウス
    /// （第2層の <c>DS4Windows.Processing.Mouse</c>）に対して行う操作だけを切り出した契約。<c>Mouse</c> が実装する。
    /// <c>IProfileLoadTarget.GetTouchPad</c> が、<c>Mouse</c> の代わりにこの型を返す（同じインスタンス）。横断基盤（Core）から第2層の具象を参照しないため。
    /// </summary>
    public interface ITouchPadGyroToggles
    {
        /// <summary>ジャイロのマウスのデッドゾーン（プロファイルの <c>GyroMouseDeadZone</c>）。</summary>
        int CursorGyroDead { get; set; }

        /// <summary>ジャイロ → コントロールのトグル。</summary>
        bool ToggleGyroControls { get; set; }

        /// <summary>ジャイロのマウスのトグル。</summary>
        bool ToggleGyroMouse { get; set; }

        /// <summary>ジャイロのマウススティックのトグル。</summary>
        bool ToggleGyroStick { get; set; }

        /// <summary>ジャイロの各トグルの状態を初期化する。</summary>
        void ResetToggleGyroModes();
    }
}
