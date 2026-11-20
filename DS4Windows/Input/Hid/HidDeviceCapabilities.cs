using DS4Windows.Core.Platform;

namespace DS4Windows.Input.Hid
{
    public class HidDeviceCapabilities
    {
        private ushort usagePage;
        private ushort usage;
        private short inputReportByteLength;
        private short outputReportByteLength;
        private short featureReportByteLength;
        private short[] reserved;
        private short numberLinkCollectionNodes;
        private short numberInputButtonCaps;
        private short numberInputValueCaps;
        private short numberInputDataIndices;
        private short numberOutputButtonCaps;
        private short numberOutputValueCaps;
        private short numberOutputDataIndices;
        private short numberFeatureButtonCaps;
        private short numberFeatureValueCaps;
        private short numberFeatureDataIndices;

        /// <summary>OS の構造体を介さずに作る（HID を持たない通信路の機器、テスト用）。レポートの長さ以外は 0。</summary>
        public HidDeviceCapabilities(short inputReportByteLength, short outputReportByteLength, short featureReportByteLength = 0)
        {
            this.inputReportByteLength = inputReportByteLength;
            this.outputReportByteLength = outputReportByteLength;
            this.featureReportByteLength = featureReportByteLength;
            reserved = new short[17];
        }

        internal HidDeviceCapabilities(NativeMethods.HIDP_CAPS capabilities)
        {
            usage = capabilities.Usage;
            usagePage = capabilities.UsagePage;
            inputReportByteLength = capabilities.InputReportByteLength;
            outputReportByteLength = capabilities.OutputReportByteLength;
            featureReportByteLength = capabilities.FeatureReportByteLength;
            reserved = capabilities.Reserved;
            numberLinkCollectionNodes = capabilities.NumberLinkCollectionNodes;
            numberInputButtonCaps = capabilities.NumberInputButtonCaps;
            numberInputValueCaps = capabilities.NumberInputValueCaps;
            numberInputDataIndices = capabilities.NumberInputDataIndices;
            numberOutputButtonCaps = capabilities.NumberOutputButtonCaps;
            numberOutputValueCaps = capabilities.NumberOutputValueCaps;
            numberOutputDataIndices = capabilities.NumberOutputDataIndices;
            numberFeatureButtonCaps = capabilities.NumberFeatureButtonCaps;
            numberFeatureValueCaps = capabilities.NumberFeatureValueCaps;
            numberFeatureDataIndices = capabilities.NumberFeatureDataIndices;

        }

        public ushort Usage { get => usage; private set => usage = value; }
        public ushort UsagePage { get => usagePage; private set => usagePage = value; }
        public short InputReportByteLength { get => inputReportByteLength; private set => inputReportByteLength = value; }
        public short OutputReportByteLength { get => outputReportByteLength; private set => outputReportByteLength = value; }
        public short FeatureReportByteLength { get => featureReportByteLength; private set => featureReportByteLength = value; }
        public short[] Reserved { get => reserved; private set => reserved = value; }
        public short NumberLinkCollectionNodes { get => numberLinkCollectionNodes; private set => numberLinkCollectionNodes = value; }
        public short NumberInputButtonCaps { get => numberInputButtonCaps; private set => numberInputButtonCaps = value; }
        public short NumberInputValueCaps { get => numberInputValueCaps; private set => numberInputValueCaps = value; }
        public short NumberInputDataIndices { get => numberInputDataIndices; private set => numberInputDataIndices = value; }
        public short NumberOutputButtonCaps { get => numberOutputButtonCaps; private set => numberOutputButtonCaps = value; }
        public short NumberOutputValueCaps { get => numberOutputValueCaps; private set => numberOutputValueCaps = value; }
        public short NumberOutputDataIndices { get => numberOutputDataIndices; private set => numberOutputDataIndices = value; }
        public short NumberFeatureButtonCaps { get => numberFeatureButtonCaps; private set => numberFeatureButtonCaps = value; }
        public short NumberFeatureValueCaps { get => numberFeatureValueCaps; private set => numberFeatureValueCaps = value; }
        public short NumberFeatureDataIndices { get => numberFeatureDataIndices; private set => numberFeatureDataIndices = value; }
    }
}
