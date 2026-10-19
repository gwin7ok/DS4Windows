namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2。Q9＝A の考え方）: Controls タブの Button（パッド）の出力が受け取る「解釈済みの値」。
    /// どの元（<see cref="Source"/>）を、どの出力先（<see cref="Output"/>。X360 のコントロールを逆引きした DS4Controls）へ、どの種類（<see cref="Kind"/>）で出すかの解釈は変換層（ビルダー）が行う。
    /// </summary>
    public readonly struct PadBindingValues
    {
        public PadBindingValues(DS4Controls source, DS4Controls output, PadTargetKind kind)
        {
            Source = source;
            Output = output;
            Kind = kind;
        }

        /// <summary>割り当てたボタン（値や押下を読む元）。</summary>
        public DS4Controls Source { get; }

        /// <summary>出力先（X360 のコントロールを逆引きした DS4Controls）。</summary>
        public DS4Controls Output { get; }

        public PadTargetKind Kind { get; }
    }
}
