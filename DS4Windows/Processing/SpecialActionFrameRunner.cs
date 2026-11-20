#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing
{
    /// <summary>
    /// 1 フレームぶんのスペシャルアクションの評価（プロファイルのアクション名を写し、トリガーのエンジンへ順に渡す）。
    /// <see cref="Mapping"/> が MapCustom の途中で呼ぶ。アクション名の写しは、毎フレームの割り当てを避けるデバイスごとの使い回しのバッファー。
    /// </summary>
    public sealed class SpecialActionFrameRunner
    {
        private readonly IProfileActionProvider _provider;
        private readonly ITriggerDispatchEngine _engine;
        private readonly TriggerInput[] _triggerInputs;
        private readonly FrameOutput[] _frameOutputs;
        private readonly string[][] _buffers = new string[ControllerLimits.MaxControllerCount][];

        public SpecialActionFrameRunner(IProfileActionProvider provider, ITriggerDispatchEngine engine,
            TriggerInput[] triggerInputs, FrameOutput[] frameOutputs)
        {
            ArgumentNullException.ThrowIfNull(provider);
            _provider = provider;
            ArgumentNullException.ThrowIfNull(engine);
            _engine = engine;
            ArgumentNullException.ThrowIfNull(triggerInputs);
            _triggerInputs = triggerInputs;
            ArgumentNullException.ThrowIfNull(frameOutputs);
            _frameOutputs = frameOutputs;
        }

        /// <summary>アクション名を <paramref name="buffer"/> へ写し（足りなければ広げ）、名前の数を返す。一覧が null なら 0。</summary>
        internal static int CopyProfileActionNames(IReadOnlyList<string>? names, ref string[] buffer)
        {
            if (names == null) return 0;
            int count = names.Count;
            if (buffer == null || buffer.Length < count) buffer = new string[Math.Max(count, 8)];
            // UI スレッドがプロファイルの読込で一覧を Clear→Add していると、数えたあとで要素が減ることがある。その場合は読めた分だけを使う。
            int copied = 0;
            try
            {
                for (; copied < count; copied++) buffer[copied] = names[copied];
            }
            catch (ArgumentOutOfRangeException)
            {
                // 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける）
            }
            return copied;
        }

        public void Run(int device)
        {
            try
            {
                // 例外（プロファイルの編集との競合など）はここで記録して、フレームを続ける。
                int totalActionCount = _provider.ActionDefinitionCount;

                // プロファイルのアクション名のスナップショット（UI スレッドが一覧を変更しても、列挙中に壊れないように）。
                // 毎フレームの割り当てを避けるため、デバイスごとの使い回しのバッファーへ写す。
                string[] actionNames = _buffers[device];
                int profileListLen = CopyProfileActionNames(_provider.GetProfileActionsRaw(device), ref actionNames);
                _buffers[device] = actionNames;

                // Issue8-1(3)是正: 抑制中のボタンのうち、実際に離されたものを解除する（デバイスにつき 1 回）。
                try
                {
                    _engine.ReleaseSuppressed(device, _triggerInputs[device]);
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }

                for (int actionIndex = 0; actionIndex < profileListLen; actionIndex++)
                {
                    string actionname = actionNames[actionIndex];
                    SpecialAction? action = _provider.GetProfileAction(device, actionname);
                    int index = _provider.GetProfileActionIndexOf(device, actionname);

                    if (index < 0 || index >= totalActionCount)
                        continue;

                    if (action == null)
                        continue;

                    // 束縛が登録されているアクションは、エンジンが処理する。プロファイル内のアクションの順序は変わらない。
                    if (_engine.Owns(device, action))
                    {
                        if (_engine.ProcessOwned(device, action, _triggerInputs[device], _frameOutputs[device]) == DS4Windows.Processing.Actions.FrameResult.EndFrame)
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AppLogger.Log(AppLogLevel.Error, $"MapCustomAction exception: {ex}", LogOptions.UserFacing);
                }
                catch { /* 診断の出力の失敗は無視してよい */ }

                return;
            }

            // 自動の復帰（Unload on trigger release）は、エンジンが持つ合図で確認する
            _engine.ProcessUntrigger(device, _triggerInputs[device]);
        }
    }
}
