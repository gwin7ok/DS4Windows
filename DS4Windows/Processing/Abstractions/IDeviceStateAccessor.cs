using DS4Windows.Input;

namespace DS4Windows.Core.Abstractions
{
    public interface IDeviceStateAccessor
    {
        DS4Device GetController(int deviceIndex);
    }
}