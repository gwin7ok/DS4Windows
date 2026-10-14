using System;
using System.Threading;
using DS4Windows;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IMacroPlayer の標準実装
    /// IVirtualKBM をコンストラクタ注入で受領し、
    /// Mapping.PlayMacroDirect / Mapping.EndMacroDirect へ安全に委譲し、
    /// 機能・エッジケース処理を 100% 維持したまま DI 境界を提供します。
    /// </summary>
    public class DefaultMacroPlayer : IMacroPlayer
    {
        // Phase6-Step9b-3（F05）: 4 スロット固定だと、拡張スロット数が 8 のときデバイス 4〜7 のマクロが再生されなかった。
        private readonly bool[] _isPlaying = new bool[Global.MAX_DS4_CONTROLLER_COUNT];
        private readonly object _lock = new object();
        private readonly IVirtualKBM _virtualKBM;

        public DefaultMacroPlayer(IVirtualKBM virtualKBM = null)
        {
            _virtualKBM = virtualKBM ?? DS4WinWPF.AppHost.GetService<IVirtualKBM>();
        }

        public bool IsPlaying(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _isPlaying.Length) return false;
            lock (_lock)
            {
                return _isPlaying[deviceIndex];
            }
        }

        public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default)
        {
            if (deviceIndex < 0 || deviceIndex >= _isPlaying.Length || action == null) return;

            lock (_lock)
            {
                _isPlaying[deviceIndex] = true;
            }

            Mapping.PlayMacroDirect(deviceIndex, action);
        }

        public void Stop(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _isPlaying.Length) return;

            lock (_lock)
            {
                _isPlaying[deviceIndex] = false;
            }

            Mapping.EndMacroDirect(deviceIndex);
        }
    }
}
