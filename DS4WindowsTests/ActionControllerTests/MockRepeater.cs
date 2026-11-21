using DS4Windows.Processing.Actions;

namespace DS4Windows.Actions.Tests
{
    // Simple mock repeater for testing Start/Stop calls
    class MockRepeater : IRepeater
    {
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }

        public void Start()
        {
            Started = true;
            Stopped = false;
        }

        public void Stop()
        {
            Stopped = true;
            Started = false;
        }

        // Explicit interface implementation to ensure correct interface mapping
        void DS4Windows.Processing.Actions.IRepeater.Stop()
        {
            Stop();
        }

        public void Dispose() { }
    }
}
