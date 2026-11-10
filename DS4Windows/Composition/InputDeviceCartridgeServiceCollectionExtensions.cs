using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing.Abstractions;

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

        /// <summary>
        /// 入力機種（系統）を 1 行で登録する: 第 1 層のカートリッジ（VID／PID の表と生成）と、第 2 層の接続フック（<see cref="IInputDeviceModule"/>）を、同時に登録する。
        /// モジュールは解決すると登録順の列になるため、呼ぶ順を変えない。
        /// </summary>
        public static IServiceCollection AddInputDevice<TCartridge, TModule>(this IServiceCollection services)
            where TCartridge : class, IInputDeviceCartridge
            where TModule : class, IInputDeviceModule
        {
            services.AddSingleton<IInputDeviceModule, TModule>();
            return services.AddInputDeviceCartridge<TCartridge>();
        }
    }
}
