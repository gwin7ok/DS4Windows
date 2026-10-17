using System.Threading;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step7 W2（Q2＝A）: 合成キー・マウス入力の状態の保持クラス（旧: <c>Mapping</c> の静的フィールド <c>globalState</c>／<c>deviceState</c>／<c>syncStateLock</c>）。
    /// <c>Mapping</c>（<c>Commit</c> ほか）、<c>SyntheticDispatcher</c>、<c>ActionsLoadedRuntimeReset</c> が使う。
    /// 配列の参照は再代入しない（要素の差し替えのみ。<c>GlobalState</c> は読み込み後のリセットで差し替える）。要素の読み書きにロックはない（従来どおり）。
    /// </summary>
    public class SyntheticInputState
    {
        // TODO(Phase7-Step8、Q3＝A): 暫定の静的な窓口。Mapping（静的）、Mouse、SyntheticDispatcher がコンテナの外から同じ instance を得るために置く。
        // Mapping の instance 化で、Mapping が本クラスをコンストラクターで受け取り、SyntheticDispatcher も注入になった時点で削除する。
        // DI にも同じ instance を Singleton として登録している（ServiceRegistration）。
        public static SyntheticInputState Shared { get; } = new SyntheticInputState();

        public ReaderWriterLockSlim SyncStateLock { get; } = new ReaderWriterLockSlim();

        public SyntheticState GlobalState { get; set; } = new SyntheticState();

        public SyntheticState[] DeviceState { get; }

        public SyntheticInputState()
        {
            DeviceState = new SyntheticState[Global.MAX_DS4_CONTROLLER_COUNT];
            for (int i = 0; i < DeviceState.Length; i++) DeviceState[i] = new SyntheticState();
        }

        /// <summary>全体と全デバイスの合成状態を新品へ戻す（Actions.xml の読み込み後のリセット用）。</summary>
        public void ResetAll()
        {
            GlobalState = new SyntheticState();
            for (int d = 0; d < DeviceState.Length; d++)
            {
                DeviceState[d] = new SyntheticState();
            }
        }
    }
}
