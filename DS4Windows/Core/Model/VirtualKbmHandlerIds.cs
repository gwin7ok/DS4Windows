namespace DS4Windows.Core.Model
{
    /// <summary>
    /// 仮想キーボード・マウスの出力方式の識別子（起動引数 <c>-virtualkbm</c> と、出力の実装の識別に使う）。
    /// 識別子の値と有効かどうかの判定を、起動引数の解析（<c>Core</c>）が出力の実装（第 3 層）を参照せずに済むよう、ここに置く。
    /// 出力の実装（<c>SendInputHandler</c>、<c>FakerInputHandler</c>）の識別子は、この値を使う。
    /// </summary>
    public static class VirtualKbmHandlerIds
    {
        /// <summary>導入されているドライバーから自動で選ぶ。</summary>
        public const string Default = "default";

        public const string SendInput = "sendinput";

        public const string FakerInput = "fakerinput";

        /// <summary>起動引数で指定できる識別子か（<see cref="Default"/> は指定できない。省略時の既定として扱う）。</summary>
        public static bool IsValid(string identifier)
        {
            switch (identifier)
            {
                case SendInput:
                case FakerInput:
                    return true;
                default:
                    return false;
            }
        }
    }
}
