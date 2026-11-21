using DS4Windows.Processing.Actions;

namespace DS4Windows.Actions.Tests
{
    // Deterministic fake repeater for tests: Start／Stop の状態だけを持つ。
    public class FakeDeterministicRepeater : IRepeater
    {
        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Dispose()
        {
            IsRunning = false;
        }
    }
}
