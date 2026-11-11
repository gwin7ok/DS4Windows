
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.7b-W1: テスト用の <see cref="InputDeviceCore"/>。実機（HID）を持たずに、デバイスの「前回のレポート」だけを返す。
    /// 旧 <c>MapCustomAction</c> の MultiAction の枝が <c>d.getPreviousStateRef()</c> だけを使うため（押下・解放のエッジの判定）。
    /// 作成は <c>RuntimeHelpers.GetUninitializedObject</c> で行い、コンストラクターは実行しない（<c>MappingCharacterizationScope</c> と同じ方式）。
    /// </summary>
    public sealed class TestDs4Device : InputDeviceCore
    {
        private TestDs4Device() : base(null, null)
        {
        }

        /// <summary>前回のレポートの状態（<see cref="InputDeviceCore.getPreviousStateRef"/> が返す）。</summary>
        public ControllerState Previous { get; set; } = new ControllerState();

        public override ControllerState getPreviousStateRef() => Previous;

        public override event ReportHandler<System.EventArgs> Report { add { } remove { } }

        public override void removeReportHandlers()
        {
        }

        public override void PostInit()
        {
        }

        protected override void RunInputLoop()
        {
        }

        public static TestDs4Device Create()
            => (TestDs4Device)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TestDs4Device));
    }
}
