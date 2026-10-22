using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: <see cref="MacroStepExecutor"/> を、旧実装（<c>Mapping.PlayMacroCodeValue</c> を逐語的に写したもの）と<b>突き合わせる差分テスト</b>。
    /// 仮想キーボード・マウスへの送出を、呼び出しの名前と引数で記録して比べる。ゲームパッドのボタンの出力フラグ（<c>MacroControl</c>）と個数（<c>MacroCount</c>）、
    /// ライトバーの強制色（<c>DS4LightBar</c> の静的な配列）、待ちの要否の戻り値、押下の表（<c>keydown</c>）も比べる。
    /// 旧実装は private で、切り出しの後は <c>Mapping</c> から消えるため、写しをこのファイルに残す（Phase9 の後のテスト整理まで）。
    /// 旧実装は全コントローラーで 1 つのフラグと個数を共有していたが、新しい実装はデバイスごと（ユーザー判断 Q2）。1 台のときの結果は同じ。
    /// </summary>
    public class Phase9CMacroStepExecutorDifferentialTests
    {
        /// <summary>送出の呼び出しを文字列で記録する <see cref="IVirtualKBM"/>。</summary>
        private sealed class LogKbm : IVirtualKBM
        {
            public readonly List<string> Log = new List<string>();
            public bool fakeKeyRepeat { get; set; }
            public bool Connect() => true;
            public bool Disconnect() => true;
            public void MoveRelativeMouse(int x, int y) => Log.Add($"Move:{x},{y}");
            public void PerformMouseWheelEvent(int vertical, int horizontal) => Log.Add($"Wheel:{vertical},{horizontal}");
            public void PerformMouseButtonEvent(uint mouseButton) => Log.Add($"MouseButton:{mouseButton}");
            public void PerformMouseButtonEventAlt(uint mouseButton, int type) => Log.Add($"MouseButtonAlt:{mouseButton},{type}");
            public void PerformMouseButtonPress(uint mouseButton) => Log.Add($"MousePress:{mouseButton}");
            public void PerformMouseButtonRelease(uint mouseButton) => Log.Add($"MouseRelease:{mouseButton}");
            public void PerformKeyPress(uint key) => Log.Add($"KeyPress:{key}");
            public void PerformKeyPressAlt(uint key) => Log.Add($"KeyPressAlt:{key}");
            public void PerformKeyRelease(uint key) => Log.Add($"KeyRelease:{key}");
            public void PerformKeyReleaseAlt(uint key) => Log.Add($"KeyReleaseAlt:{key}");
            public void Sync() { }
            public string GetDisplayName() => string.Empty;
            public string GetIdentifier() => string.Empty;
            public string GetFullDisplayName() => string.Empty;
        }

        /// <summary>旧 <c>Mapping.PlayMacroCodeValue</c> の逐語的な写し。フラグと個数は、旧実装のとおり、この instance が 1 組だけ持つ。</summary>
        private sealed class OldMacroStep
        {
            public readonly IVirtualKBM VirtualKBM;
            private readonly IProfileSettingsService profileSettings;
            private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;
            public readonly bool[] macroControl = new bool[26];
            public uint MacroCount;

            public OldMacroStep(IVirtualKBM kbm, IProfileSettingsService settings, Func<IDeviceStateAccessor> accessor)
            {
                VirtualKBM = kbm;
                profileSettings = settings;
                _deviceStateAccessor = accessor;
            }

            public bool PlayMacroCodeValue(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown)
            {
                // Phase6-Step3-6c（決定P2）: 非同期マクロ経路（別スレッド、ctrl を持たない連鎖）のため、Mapping が保持する profileSettings から読む。
                // Phase7-Step8 W2 で instance 化（コンストラクター注入）済み。この経路も instance の profileSettings を読む（Phase7c W3 でコメントを訂正）。
                bool doDelayOnCaller = false;
                if (macroCodeValue >= 261 && macroCodeValue <= DS4ControlSettings.MAX_MACRO_VALUE)
                {
                    // Gamepad button up or down macro event. macroCodeValue index value is the button identifier (codeValue-261 = idx in 0..24 range)
                    if (!keydown[macroCodeValue])
                    {
                        macroControl[macroCodeValue - 261] = keydown[macroCodeValue] = true;
                        MacroCount++;
                    }
                    else
                    {
                        macroControl[macroCodeValue - 261] = keydown[macroCodeValue] = false;
                        if (MacroCount > 0) MacroCount--;
                    }
                }
                else if (macroCodeValue < 300)
                {
                    // Keyboard key or mouse button macro event
                    if (!keydown[macroCodeValue])
                    {
                        switch (macroCodeValue)
                        {
                            //anything above 255 is not a keyvalue
                            case 256:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseLeftDown");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);
                                break;
                            case 257:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseRightDown");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                                break;
                            case 258:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseMiddleDown");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                                break;
                            case 259:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonDown btn=1");
                                VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                                break;
                            case 260:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonDown btn=2");
                                VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                                break;

                            default:
                                uint eventMacroCode = !profileSettings.OutputKBMMapping.macroKeyTranslate ? (uint)macroCodeValue :
                                    profileSettings.OutputKBMMapping.GetRealEventKey((uint)macroCodeValue);

                                if (keyType.HasFlag(DS4KeyType.ScanCode))
                                {
                                    AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyPressAlt native={eventMacroCode}");
                                    VirtualKBM.PerformKeyPressAlt(eventMacroCode);
                                }
                                else
                                {
                                    AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyPress native={eventMacroCode}");
                                    VirtualKBM.PerformKeyPress(eventMacroCode);
                                }
                                break;
                        }
                        keydown[macroCodeValue] = true;
                    }
                    else
                    {
                        switch (macroCodeValue)
                        {
                            //anything above 255 is not a keyvalue
                            case 256:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseLeftUp");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                                break;
                            case 257:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseRightUp");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                                break;
                            case 258:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseMiddleUp");
                                VirtualKBM.PerformMouseButtonEvent(profileSettings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                                break;
                            case 259:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonUp btn=1");
                                VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                                break;
                            case 260:
                                AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=MouseXButtonUp btn=2");
                                VirtualKBM.PerformMouseButtonEventAlt(profileSettings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
                                break;

                            default:
                                uint eventMacroCode = !profileSettings.OutputKBMMapping.macroKeyTranslate ? (uint)macroCodeValue :
                                    profileSettings.OutputKBMMapping.GetRealEventKey((uint)macroCodeValue);

                                if (keyType.HasFlag(DS4KeyType.ScanCode))
                                {
                                    long _nowTicksDbg = DateTime.UtcNow.Ticks;
                                    long _deltaSendDbg = 0; // inline macro release: lastSyntheticSend not tracked here
                                    AppLogger.LogTrace($"INLINE RELEASE TRACE device={device} macroCode={macroCodeValue} native={eventMacroCode} lastSendDeltaMs={_deltaSendDbg}");
                                    AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyReleaseAlt native={eventMacroCode}");
                                    VirtualKBM.PerformKeyReleaseAlt(eventMacroCode);
                                }
                                else
                                {
                                    long _nowTicksDbg = DateTime.UtcNow.Ticks;
                                    long _deltaSendDbg = 0;
                                    AppLogger.LogTrace($"INLINE RELEASE TRACE device={device} macroCode={macroCodeValue} native={eventMacroCode} lastSendDeltaMs={_deltaSendDbg}");
                                    AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event=KeyRelease native={eventMacroCode}");
                                    VirtualKBM.PerformKeyRelease(eventMacroCode);
                                }
                                break;
                        }
                        keydown[macroCodeValue] = false;
                    }
                }
                else if (macroCodeValue >= 1000000000)
                {
                    // Lightbar color event
                    if (macroCodeValue > 1000000000)
                    {
                        string lb = macroCodeValue.ToString().Substring(1);
                        byte r = (byte)(int.Parse(lb[0].ToString()) * 100 + int.Parse(lb[1].ToString()) * 10 + int.Parse(lb[2].ToString()));
                        byte g = (byte)(int.Parse(lb[3].ToString()) * 100 + int.Parse(lb[4].ToString()) * 10 + int.Parse(lb[5].ToString()));
                        byte b = (byte)(int.Parse(lb[6].ToString()) * 100 + int.Parse(lb[7].ToString()) * 10 + int.Parse(lb[8].ToString()));
                        DS4LightBar.forcelight[device] = true;
                        DS4LightBar.forcedFlash[device] = 0;
                        DS4LightBar.forcedColor[device] = new DS4Color(r, g, b);
                    }
                    else
                    {
                        DS4LightBar.forcedFlash[device] = 0;
                        DS4LightBar.forcelight[device] = false;
                    }
                }
                else if (macroCodeValue >= 1000000)
                {
                    // Rumble event
                    DS4Device d = null;
                    try
                    {
                        var accessor = _deviceStateAccessor();
                        if (accessor != null) d = accessor.GetController(device);
                    }
                    catch { }
                    // Phase7-Step5 W2: IDeviceStateAccessor は ControlService 自身の登録のため、旧 Program.rootHub のフォールバックは冗長だった
                    string r = macroCodeValue.ToString().Substring(1);
                    byte heavy = (byte)(int.Parse(r[0].ToString()) * 100 + int.Parse(r[1].ToString()) * 10 + int.Parse(r[2].ToString()));
                    byte light = (byte)(int.Parse(r[3].ToString()) * 100 + int.Parse(r[4].ToString()) * 10 + int.Parse(r[5].ToString()));
                    if (profileSettings.InverseRumbleMotors[device])
                        d.setRumble(heavy, light);
                    else
                        d.setRumble(light, heavy);
                }
                else
                {
                    // Delay specification. Indicate to caller that it should do a delay of macroCodeValue-300 msecs
                    doDelayOnCaller = true;
                }

                return doDelayOnCaller;
            }
        }

        private static int[] CodeValues()
        {
            var list = new List<int> { 0, 1, 8, 13, 65, 90, 160, 255, 256, 257, 258, 259, 260, 261, 262, 273, 285, 286, 299 };
            list.Add(DS4ControlSettings.MAX_MACRO_VALUE + 1);   // ゲームパッドの範囲の外（キーとして扱われる）
            list.AddRange(new[] { 300, 301, 350, 5000, 999999 });                // 待ち
            list.AddRange(new[] { 1000000000, 1255128064, 1000000001, 1999999999 });   // ライトバー（消灯・色）
            return list.Distinct().ToArray();
        }

        /// <summary>共有ホストの設定には出力 KBM マッピングがない（null）ため、テストの間だけ設定し、終わりに戻す。</summary>
        private sealed class KbmMappingScope : IDisposable
        {
            private readonly IProfileSettingsService _settings;
            private readonly VirtualKBMMapping _saved;

            public KbmMappingScope(IProfileSettingsService settings)
            {
                _settings = settings;
                _saved = settings.OutputKBMMapping;
                VirtualKBMMapping mapping = VirtualKBMFactory.GetMappingInstance();
                mapping.PopulateConstants();
                mapping.PopulateMappings();
                settings.OutputKBMMapping = mapping;
            }

            public void Dispose() => _settings.OutputKBMMapping = _saved;
        }

        private const int OldDevice = 5;   // 旧実装とライトバーの静的な配列を共有するため、別のスロットを使う
        private const int NewDevice = 6;

        [Theory]
        [InlineData(DS4KeyType.None)]
        [InlineData(DS4KeyType.ScanCode)]
        public void Execute_MatchesTheOldImplementation_ForEveryCodeValue(DS4KeyType keyType)
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            using var kbmMapping = new KbmMappingScope(settings);
            var oldKbm = new LogKbm();
            var newKbm = new LogKbm();
            var oldImpl = new OldMacroStep(oldKbm, settings, () => null);
            var state = new MacroRuntimeState();
            var newImpl = new MacroStepExecutor(newKbm, settings, state, () => null);
            var oldKeydown = new bool[512];
            var newKeydown = new bool[512];

            foreach (int code in CodeValues())
            {
                // 押下 → 離上（同じ値を 2 回）。待ちと光は 1 回でも十分だが、同じ手順で比べる
                for (int pass = 0; pass < 2; pass++)
                {
                    bool expectedDelay = oldImpl.PlayMacroCodeValue(OldDevice, keyType, code, oldKeydown);
                    bool actualDelay = newImpl.Execute(NewDevice, keyType, code, newKeydown);

                    string where = $"keyType={keyType} code={code} pass={pass}";
                    Assert.True(expectedDelay == actualDelay, where + ": 待ちの要否が違う");
                    Assert.Equal(oldKbm.Log, newKbm.Log);
                    Assert.True(oldKeydown.SequenceEqual(newKeydown), where + ": 押下の表が違う");
                    // 旧実装の写しの配列は 26 個、新しい実装は 28 個（M6 の是正）。共通の 0〜25 を比べ、広げた 26・27 は立たないことを確かめる
                    Assert.True(oldImpl.macroControl.SequenceEqual(state.MacroControl[NewDevice].Take(oldImpl.macroControl.Length)), where + ": ボタンの出力フラグが違う");
                    Assert.False(state.MacroControl[NewDevice][26] || state.MacroControl[NewDevice][27], where + ": 広げたフラグが立っている");
                    Assert.True(oldImpl.MacroCount == state.MacroCount[NewDevice], where + ": 個数が違う");
                    Assert.True(DS4LightBar.forcelight[OldDevice] == DS4LightBar.forcelight[NewDevice], where + ": 強制点灯が違う");
                    Assert.True(DS4LightBar.forcedFlash[OldDevice] == DS4LightBar.forcedFlash[NewDevice], where + ": 強制の点滅が違う");
                    if (DS4LightBar.forcelight[NewDevice])
                    {
                        Assert.True(DS4LightBar.forcedColor[OldDevice].Equals(DS4LightBar.forcedColor[NewDevice]), where + ": 強制色が違う");
                    }
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        [InlineData(1255128)]
        public void RumbleValue_ThrowsTheSameExceptionAsTheOldImplementation_WhenThereIsNoController(int code)
        {
            // 旧実装の癖（計画書 M4）: コントローラーが取れないと NullReferenceException になる。そのまま移した
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            var oldImpl = new OldMacroStep(new LogKbm(), settings, () => null);
            var newImpl = new MacroStepExecutor(new LogKbm(), settings, new MacroRuntimeState(), () => null);

            Exception expected = Record.Exception(() => oldImpl.PlayMacroCodeValue(OldDevice, DS4KeyType.None, code, new bool[512]));
            Exception actual = Record.Exception(() => newImpl.Execute(NewDevice, DS4KeyType.None, code, new bool[512]));

            Assert.NotNull(expected);
            Assert.NotNull(actual);
            Assert.Equal(expected.GetType(), actual.GetType());
        }

        [Theory]
        [InlineData(287)]
        [InlineData(288)]
        public void TouchStartedAndEndedValues_NoLongerThrow_ButOutputNothing(int code)
        {
            // 計画書 M6 の是正（ユーザー承認）: 旧実装は、ゲームパッドの範囲（261〜288）のうち、フラグの配列（26 個＝261〜286）を超える 287（Touch Started）・
            // 288（Touch Ended）で IndexOutOfRangeException になっていた（旧実装の写しは、今も同じ例外を出す）。新しい実装は、フラグの配列を 28 個にして例外にしない。
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            var oldImpl = new OldMacroStep(new LogKbm(), settings, () => null);
            Assert.IsType<IndexOutOfRangeException>(Record.Exception(() => oldImpl.PlayMacroCodeValue(OldDevice, DS4KeyType.None, code, new bool[512])));

            var state = new MacroRuntimeState();
            var newImpl = new MacroStepExecutor(new LogKbm(), settings, state, () => null);
            var keydown = new bool[512];

            newImpl.Execute(NewDevice, DS4KeyType.None, code, keydown);   // 押下: 例外にならない
            Assert.True(state.MacroControl[NewDevice][code - 261]);
            Assert.Equal(1u, state.MacroCount[NewDevice]);
            var mapped = new DS4State();
            state.ApplyHeldControls(NewDevice, mapped);   // 対応する出力がないので、何も重ならない
            Assert.False(mapped.Cross);
            Assert.Equal(new DS4State().LX, mapped.LX);

            newImpl.Execute(NewDevice, DS4KeyType.None, code, keydown);   // 離上
            Assert.False(state.MacroControl[NewDevice][code - 261]);
            Assert.Equal(0u, state.MacroCount[NewDevice]);
        }

        [Fact]
        public void GamepadButtonValue_SetsAndClearsTheFlagOfThatDeviceOnly()
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            var state = new MacroRuntimeState();
            var executor = new MacroStepExecutor(new LogKbm(), settings, state, () => null);
            var keydown = new bool[512];

            executor.Execute(2, DS4KeyType.None, 261, keydown);   // 261 = フラグ 0（Cross）の押下
            Assert.True(state.MacroControl[2][0]);
            Assert.Equal(1u, state.MacroCount[2]);
            Assert.All(Enumerable.Range(0, state.MacroControl.Length).Where(d => d != 2), d =>
            {
                Assert.False(state.MacroControl[d][0]);
                Assert.Equal(0u, state.MacroCount[d]);
            });

            executor.Execute(2, DS4KeyType.None, 261, keydown);   // 離上
            Assert.False(state.MacroControl[2][0]);
            Assert.Equal(0u, state.MacroCount[2]);
        }
    }
}
