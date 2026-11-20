#nullable enable

namespace DS4Windows.Processing
{
    public class PostMapStickData
    {
        public bool dirty;
        public byte LX = 128;
        public byte LY = 128;
        public byte RX = 128;
        public byte RY = 128;

        public void Reset()
        {
            dirty = false;
            LX = 128;
            LY = 128;
            RX = 128;
            RY = 128;
        }
    }
}
