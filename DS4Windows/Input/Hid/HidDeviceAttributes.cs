using DS4Windows.Core.Platform;

namespace DS4Windows.Input.Hid
{
    public class HidDeviceAttributes
    {
        private int vendorId;
        private int productId;
        private int version;
        private string vendorHexId;
        private string productHexId;

        /// <summary>OS の構造体を介さずに作る（HID を持たない通信路の機器、テスト用）。</summary>
        public HidDeviceAttributes(int vendorId, int productId, int version)
        {
            this.vendorId = vendorId;
            this.productId = productId;
            this.version = version;

            vendorHexId = "0x" + vendorId.ToString("X4");
            productHexId = "0x" + productId.ToString("X4");
        }

        internal HidDeviceAttributes(NativeMethods.HIDD_ATTRIBUTES attributes)
        {
            vendorId = attributes.VendorID;
            productId = attributes.ProductID;
            version = attributes.VersionNumber;

            vendorHexId = "0x" + attributes.VendorID.ToString("X4");
            productHexId = "0x" + attributes.ProductID.ToString("X4");
        }

        public int VendorId { get => vendorId; private set => vendorId = value; }
        public int ProductId { get => productId; private set => productId = value; }
        public int Version { get => version; private set => version = value; }
        public string VendorHexId { get => vendorHexId; set => vendorHexId = value; }
        public string ProductHexId { get => productHexId; set => productHexId = value; }
    }
}
