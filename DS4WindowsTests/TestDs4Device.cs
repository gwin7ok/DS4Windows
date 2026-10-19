using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.7b-W1: テスト用の <see cref="DS4Device"/>。実機（HID）を持たずに、デバイスの「前回のレポート」だけを返す。
    /// 旧 <c>MapCustomAction</c> の MultiAction の枝が <c>d.getPreviousStateRef()</c> だけを使うため（押下・解放のエッジの判定）。
    /// 作成は <c>RuntimeHelpers.GetUninitializedObject</c> で行い、コンストラクターは実行しない（<c>MappingCharacterizationScope</c> と同じ方式）。
    /// </summary>
    public sealed class TestDs4Device : DS4Device
    {
        private TestDs4Device() : base(null, null)
        {
        }

        /// <summary>前回のレポートの状態（<see cref="DS4Device.getPreviousStateRef"/> が返す）。</summary>
        public DS4State Previous { get; set; } = new DS4State();

        public override DS4State getPreviousStateRef() => Previous;

        /// <summary>
        /// Phase8-Step7 7.8e-W1: デバイスの生の前回・現在の状態（<see cref="DS4Device.GetRawPreviousStateRef"/>／<see cref="DS4Device.GetRawCurrentStateRef"/> が返す）を設定する。
        /// 旧経路の Lightbar マクロは、MapCustom の引数の状態ではなく、この生の状態の変化で判定する。<see cref="Previous"/> も同じ前回の状態にそろえる。
        /// </summary>
        public void SetRawStates(DS4State previous, DS4State current)
        {
            pState = previous;
            cState = current;
            Previous = previous;
        }

        public static TestDs4Device Create()
            => (TestDs4Device)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TestDs4Device));
    }
}
