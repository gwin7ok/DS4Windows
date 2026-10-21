namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step4c W1（Phase10-Step4c-Plan.md §2 項目 2）: Fresh-press ガード（<see cref="BindingPolicy.RespectsFreshPressGuard"/>）を見るかの規則を、1 か所にまとめた表。
    /// 以前は、<see cref="SpecialActionOutputFactory"/>（SA の種別ごと）と <c>ControlBindingBuilder</c>（Controls の区分ごとの直書き）に散っていた。挙動は変えない（値は従来のまま）。
    /// Fresh-press ガードは、プロファイル適用直後の押しっぱなしで連鎖して発火しないための安全弁（規約 2.2）。見る束縛は、トリガーが未成立になるまで新規に成立とみなさない。
    /// 統合の Step で、方針の表（種別 × 由来）の 1 列になる（Phase10-Integration-Target-Model.md §3）。
    /// </summary>
    public static class FreshPressGuardTable
    {
        /// <summary>
        /// SA の種別ごとの規則。Button の Press と MultiAction は見ない（Button の Toggle は見る）。それ以外は見る。
        /// </summary>
        /// <param name="toggle">Button が Toggle（Switch Mode）か。Button 以外では無視する。</param>
        public static bool ForSpecialAction(SpecialAction.ActionTypeId type, bool toggle = false)
        {
            switch (type)
            {
                case SpecialAction.ActionTypeId.Button:
                    return toggle;   // 設計書 §6: Press は見ない／Toggle は見る（Stage3 の ButtonSpecialActionOutput と同じ規則）
                case SpecialAction.ActionTypeId.MultiAction:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Controls の区分ごとの規則。Macro と Special（Disconnect BT ほか。SA 専用の種別は SA の規則と同じく見る）は見る。
        /// Key、Pad、Mouse、AnalogMouse、Extras、素通し（Pad の区分）は見ない（Controls の Key などは、割り当てたボタンを毎フレーム置き換えるため）。
        /// </summary>
        public static bool ForControls(ControlBindingKind kind)
        {
            switch (kind)
            {
                case ControlBindingKind.Macro:
                case ControlBindingKind.Special:
                    return true;
                default:
                    return false;
            }
        }
    }
}
