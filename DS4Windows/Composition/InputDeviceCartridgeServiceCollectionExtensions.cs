using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Composition
{
    /// <summary>入力デバイスのカートリッジの登録（機種の追加は、実装を作って、この登録を 1 行足す）。</summary>
    public static class InputDeviceCartridgeServiceCollectionExtensions
    {
        public static IServiceCollection AddInputDeviceCartridge<TCartridge>(this IServiceCollection services)
            where TCartridge : class, IInputDeviceCartridge
        {
            return services.AddSingleton<IInputDeviceCartridge, TCartridge>();
        }
    }
}
