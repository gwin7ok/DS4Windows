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
        private readonly bool[] _isPlaying = new bool[ControllerLimits.MaxControllerCount];
        private readonly object _lock = new object();
        private readonly IVirtualKBM _virtualKBM;
        // Phase7-Step8 W2: 旧 Mapping.PlayMacroDirect／EndMacroDirect の静的呼び出し。Phase13-Step8 8-b: 旧 遅延取得デリゲートをやめ、直接受ける（循環なし）
        private readonly DS4Windows.DI.IMapping _mapping;

        public DefaultMacroPlayer(IVirtualKBM virtualKBM, DS4Windows.DI.IMapping mapping)
        {
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM)); // Phase6-Step10-4-4: 必須の依存
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

            _mapping.PlayMacroDirect(deviceIndex, action);
        }

        public void Stop(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _isPlaying.Length) return;

            lock (_lock)
            {
                _isPlaying[deviceIndex] = false;
            }

            _mapping.EndMacroDirect(deviceIndex);
        }
    }
}
