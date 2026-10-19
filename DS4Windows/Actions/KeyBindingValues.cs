namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step7-8a-Plan.md W1。Q9＝A）: Key の出力が受け取る「解釈済みの値」。
    /// 設定（SA タブの <c>SpecialAction.details</c>／<c>keyType</c>、のちに Controls タブの割り当て）から何を出すかを決める解釈は変換層（ビルダー）が行い、
    /// 出力（<see cref="KeyOutputAction"/>）は、この値を実行するだけにする。
    /// </summary>
    public readonly struct KeyBindingValues
    {
        public KeyBindingValues(ushort logicalKey, bool scanCode, bool toggle, uint aliasKey = 0, bool suppressSpecialActionLogs = false)
        {
            LogicalKey = logicalKey;
            ScanCode = scanCode;
            Toggle = toggle;
            AliasKey = aliasKey;
            SuppressSpecialActionLogs = suppressSpecialActionLogs;
        }

        /// <summary>
        /// Phase8-Step7 7.8b: 別名キー（Controls タブの <c>actionAlias</c>）。0 以外なら、ネイティブキーはこのキーから求める
        /// （<c>GetRealEventKey(AliasKey)</c>。0 なら論理キーから）。SA タブは 0。
        /// </summary>
        public uint AliasKey { get; }

        /// <summary>
        /// Phase8-Step7 7.8b（Q4＝A）: 真なら、SA タブ形式の Debug／Trace ログ（<c>SpecialAction KEY entry</c> など）を出さない。
        /// 旧経路の Controls タブの Key は、これらを出していなかったため（Controls タブは真）。
        /// </summary>
        public bool SuppressSpecialActionLogs { get; }

        /// <summary>論理キー（<c>details</c> が数値として読めなければ 0）。</summary>
        public ushort LogicalKey { get; }

        /// <summary>ScanCode の指定（<c>keyType</c> の <see cref="DS4KeyType.ScanCode"/>）。</summary>
        public bool ScanCode { get; }

        /// <summary>Toggle の指定（<c>keyType</c> の <see cref="DS4KeyType.Toggle"/>。ログの出し分け用。Press／Toggle の実際の動作は <c>KeyButtonActionController</c> が決める）。</summary>
        public bool Toggle { get; }
    }
}
