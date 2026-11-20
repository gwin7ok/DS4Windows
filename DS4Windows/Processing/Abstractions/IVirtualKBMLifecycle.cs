#nullable enable

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// KBM 出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化を担う（送出は <see cref="IVirtualKBM"/> の責務）。
    /// <see cref="IVirtualKBM"/> は出力ハンドラへの委譲（実体は OutputKBMHandlerAdapter.Handler。Phase7-Step8 W3 で Global から移した）であり、ハンドラの生成・差し替えの口を持たないため、
    /// ライフサイクル操作を別インターフェースとして分離した。
    /// </summary>
    public interface IVirtualKBMLifecycle
    {
        /// <summary>現行ハンドラが存在すれば Disconnect し、ハンドラを null にする。ハンドラが無い場合は何もしない。</summary>
        void ReleaseHandler();

        /// <summary>指定の識別子でハンドラを決定する。</summary>
        void DetermineHandler(string identifier);

        /// <summary>ハンドラを既定のフォールバック（SendInput）へ差し替える。</summary>
        void SwitchToFallbackHandler();

        /// <summary>現行ハンドラの Version へ、FakerInput の導入バージョンを設定する。</summary>
        void ApplyFakerInputVersion();

        /// <summary>指定の識別子に対応する KBM マッピングを初期化する。</summary>
        void InitializeMapping(string identifier);
    }
}