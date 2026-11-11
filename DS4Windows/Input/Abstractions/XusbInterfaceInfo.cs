namespace DS4Windows.Input.Abstractions
{
    /// <summary>XUSB のデバイス インターフェース（Xbox 系の機器 1 台）の情報。</summary>
    public sealed class XusbInterfaceInfo
    {
        public XusbInterfaceInfo(string devicePath, string instanceId, int vendorId, int productId, int playerIndex)
        {
            DevicePath = devicePath;
            InstanceId = instanceId;
            VendorId = vendorId;
            ProductId = productId;
            PlayerIndex = playerIndex;
        }

        /// <summary>デバイス インターフェースのパス（接続の識別に使う）。</summary>
        public string DevicePath { get; }

        /// <summary>機器のインスタンス ID（親の連鎖をたどる起点。シリアルを含むことがある）。</summary>
        public string InstanceId { get; }

        public int VendorId { get; }
        public int ProductId { get; }

        /// <summary>XInput の番号（0〜3）。読めなかったときは -1。</summary>
        public int PlayerIndex { get; }
    }
}
