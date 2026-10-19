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

        public static TestDs4Device Create()
            => (TestDs4Device)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TestDs4Device));
    }
}
