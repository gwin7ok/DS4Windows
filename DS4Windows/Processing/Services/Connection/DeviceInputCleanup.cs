#nullable enable

using DS4Windows.Processing.Synthetic;
using System;
using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// <see cref="IDeviceInputCleanup"/> の実装。旧 <c>Mapping.HandleDeviceDisconnect</c> の、合成キーの解放、マクロのキューの消去、ボタン／キーのコントローラーの解除を、
    /// そのまま移したもの（各段は、互いに独立で、失敗しても次へ進む）。<c>Mapping</c> に残る <c>deviceRuntime</c> の作り直しは、<c>Mapping</c> が行う。
    /// </summary>
    public sealed class DeviceInputCleanup : IDeviceInputCleanup
    {
        private readonly SyntheticState[] deviceState;
        private readonly ISyntheticDispatcher _syntheticDispatcher;
        private readonly IMacroPlayback _macroPlayback;
        private readonly IKeyButtonControllerRegistry _keyButtonControllers;

        public DeviceInputCleanup(SyntheticInputState syntheticInput, ISyntheticDispatcher syntheticDispatcher,
            IMacroPlayback macroPlayback, IKeyButtonControllerRegistry keyButtonControllers)
        {
            if (syntheticInput == null) throw new ArgumentNullException(nameof(syntheticInput));
            deviceState = syntheticInput.DeviceState;
            _syntheticDispatcher = syntheticDispatcher ?? throw new ArgumentNullException(nameof(syntheticDispatcher));
            _macroPlayback = macroPlayback ?? throw new ArgumentNullException(nameof(macroPlayback));
            _keyButtonControllers = keyButtonControllers ?? throw new ArgumentNullException(nameof(keyButtonControllers));
        }

        public void Clear(int device)
        {
            try
            {
                // 保持中の合成キーに、解放を送る
                try
                {
                    if (deviceState != null && device >= 0 && device < deviceState.Length)
                    {
                        var ds = deviceState[device];
                        if (ds?.keyPresses != null)
                        {
                            var keys = new List<ushort>(ds.keyPresses.Keys);
                            foreach (var k in keys)
                            {
                                try
                                {
                                    uint native = 0;
                                    ds.nativeKeyAlias?.TryGetValue(k, out native);
                                    _syntheticDispatcher.SendRelease(device, k, native, false, null);
                                }
                                catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ }
                            }
                            try { ds.keyPresses.Clear(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                            try { ds.nativeKeyAlias!.Clear(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ } // 実行中は設定済み（null なら従来どおり NullReferenceException）
                        }
                    }
                }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }

                // マクロのキューの消去
                try { _macroPlayback.ClearDevice(device); }
                catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }

                // デバイスごとのボタン／キーのコントローラーの解除
                try { _keyButtonControllers.ClearForDevice(device); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }
    }
}
