using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    public interface IDeviceStateAccessor
    {
        InputDeviceCore GetController(int deviceIndex);
    }
}