using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services
{
    /// <summary>Xbox 系コントローラーの部品。設定の有効／無効だけを持つ（追加ボタンはなし）。</summary>
    public sealed class XboxDeviceModule : InputDeviceModuleBase
    {
        protected override InputDeviceFamily? Family => InputDeviceFamily.Xbox;
    }
}
