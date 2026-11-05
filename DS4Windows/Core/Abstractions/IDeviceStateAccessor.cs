using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    public interface IDeviceStateAccessor
    {
        DS4Device GetController(int deviceIndex);
    }
}