using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8b-W1（Phase8-Step7-8b-Plan.md §4）: Controls タブの Key の割り当て（<c>DS4ControlSettings</c>）で <c>Mapping.MapCustom</c> を実走行させる最小のハーネス。
    /// <see cref="MappingCharacterizationScope"/> の専用デバイス添字に対して、ボタンの割り当てを「キー」に変える（終了時に既定へ戻す）。
    /// <paramref name="migrated"/> が真なら、<c>BindingMigrationOptions.MigratedControlKinds</c> に Key を足す（終了時に元へ戻す）。W1 の時点では新経路が未実装のため、どちらでも旧経路が処理する
    /// （W2 で新経路が入ると、同じテストが新経路を走らせる）。
    /// </summary>
    public sealed class ControlsKeyTestScope : IDisposable
    {
        private readonly MappingCharacterizationScope _inner = new MappingCharacterizationScope();
        private readonly BindingMigrationOptions _options = TestHost.GetService<BindingMigrationOptions>();
        private readonly ControlBindingKind[] _savedKinds;
        private readonly List<DS4Controls> _touched = new List<DS4Controls>();

        public ControlsKeyTestScope(bool migrated)
        {
            Migrated = migrated;
            ExtrasPort = new RecordingControlExtrasPort(new DefaultControlExtrasPort(() => _inner.Control, _inner.Settings));
            Mapping = _inner.Mapper as Mapping;
            if (Mapping == null)
                throw new InvalidOperationException("IMapping の実体が Mapping ではない");

            _savedKinds = _options.MigratedControlKinds.ToArray();
            _options.MigratedControlKinds.Clear();
            if (migrated)
            {
                _options.MigratedControlKinds.Add(ControlBindingKind.Key);     // Phase8-Step7 7.8b: Controls タブの Key を新経路で処理する
                _options.MigratedControlKinds.Add(ControlBindingKind.Macro);   // Phase8-Step7 7.8c: Controls タブの Macro を新経路で処理する（W2 から有効）
                _options.MigratedControlKinds.Add(ControlBindingKind.Extras);  // Phase8-Step7 7.8e: Controls タブの Extras を新経路で処理する（W2 から有効）
            }

            TestHost.GetService<ITriggerDispatchEngine>().OnDeviceDisconnected(Device);
            ResetExtrasObservables();
        }

        public bool Migrated { get; }

        /// <summary>Phase8-Step7 7.8e: 新経路の Extras が使う窓口（振動は記録、ライトバーと感度は本番の窓口へ転送）。<see cref="PublishControlsOnly"/> が使う。</summary>
        public RecordingControlExtrasPort ExtrasPort { get; }

        public Mapping Mapping { get; }

        public MappingCharacterizationScope Scope => _inner;

        public int Device => MappingCharacterizationScope.Device;

        /// <summary>ボタンの割り当てを Key にする（通常。シフトなし、Extras なし）。</summary>
        public DS4ControlSettings MapKey(DS4Controls control, int key, DS4KeyType keyType = DS4KeyType.None, uint alias = 0)
        {
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, control);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Key;
            dcs.action.actionKey = key;
            dcs.action.actionAlias = alias;
            dcs.keyType = keyType;
            _touched.Add(control);
            return dcs;
        }

        /// <summary>ボタンの割り当てを Macro にする（通常。シフトなし、Extras なし）。<paramref name="steps"/> はマクロの手順（例: { 65, 65 } ＝ キー 65 を押して離す）。</summary>
        public DS4ControlSettings MapMacro(DS4Controls control, int[] steps, DS4KeyType keyType = DS4KeyType.None)
        {
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, control);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Macro;
            dcs.action.actionMacro = steps;
            dcs.keyType = keyType | DS4KeyType.Macro;
            _touched.Add(control);
            return dcs;
        }

        /// <summary>Phase8-Step7 7.8e: ボタンに Extras（振動・ライトバー・感度の 9 個の値）だけを付ける（通常の割り当ては Default）。</summary>
        public DS4ControlSettings MapExtras(DS4Controls control, string extras)
        {
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, control);
            dcs.Reset();
            dcs.extras = extras;
            _touched.Add(control);
            return dcs;
        }

        // ---- Phase8-Step7 7.8e: Extras の観察窓口（旧経路が書く、公開の状態・静的配列） ----
        public bool ForceLight => DS4LightBar.forcelight[Device];
        public DS4Color ForcedColor => DS4LightBar.forcedColor[Device];
        public byte ForcedFlash => DS4LightBar.forcedFlash[Device];
        /// <summary>振動が出ているか。旧経路は <c>extrasRumbleActive</c>、新経路は窓口の記録（最後の呼び出しが 0 でない）。</summary>
        public bool RumbleActive => Mapping.extrasRumbleActive[Device] || ExtrasPort.RumbleOn;
        public ButtonMouseInfo MouseInfo => _inner.Settings.ButtonMouseInfos[Device];

        /// <summary>テスト間で、Extras が書く状態（静的配列、感度、振動の記録、旧経路の held）を初期化する。</summary>
        public void ResetExtrasObservables()
        {
            ExtrasPort.RumbleCalls.Clear();
            DS4LightBar.forcelight[Device] = false;
            DS4LightBar.forcedFlash[Device] = 0;
            DS4LightBar.forcedColor[Device] = default;
            Mapping.extrasRumbleActive[Device] = false;
            _inner.Settings.ButtonMouseInfos[Device].Reset();

            var held = typeof(Mapping).GetField("held", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(Mapping) as DS4Controls[];
            if (held != null) held[Device] = DS4Controls.None;
        }

        /// <summary>Phase8-Step7 7.8f: ボタンの割り当てを Button（X360 のボタン・軸・トリガー、マウスなど）にする（通常。Extras なし）。</summary>
        public DS4ControlSettings MapButton(DS4Controls control, X360Controls target, DS4KeyType keyType = DS4KeyType.None)
        {
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, control);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Button;
            dcs.action.actionBtn = target;
            dcs.keyType = keyType;
            _touched.Add(control);
            return dcs;
        }

        /// <summary>記録用のキーボード出力（マクロの再生で押されたキーが入る）。</summary>
        public RecordingVirtualKbmBase Recorder => _inner.Recorder;

        /// <summary>マクロの再生が実際に出すキー（変換が有効なら、実キーへ変換される）。</summary>
        public uint Native(uint key) => _inner.KbmMapping.macroKeyTranslate ? _inner.KbmMapping.GetRealEventKey(key) : key;

        /// <summary>設定の変更の後に呼ぶ。本番の <c>PublishBindings</c> と同じ経路で、束縛を登録簿へ差し替える（W2 から Controls の束縛を含む）。</summary>
        public void Publish()
        {
            var builder = TestHost.GetService<IActionBindingBuilder>();
            TestHost.GetService<IActionRegistry>().Replace(Device, builder.Build(Device));
        }

        /// <summary>
        /// Phase8-Step7 7.8e: Controls タブの束縛だけを、記録用の窓口つきのビルダーで組み立てて登録する（SA タブの束縛は含めない）。
        /// Extras の振動を、新経路でも観察するため（本番の窓口は、実機がないと振動が見えない）。
        /// </summary>
        public void PublishControlsOnly()
        {
            var builder = new DS4Windows.Services.ControlBindingBuilder(_options, _inner.Settings, TestHost.GetService<IMacroPlayer>(), ExtrasPort);
            TestHost.GetService<IActionRegistry>().Replace(Device, builder.Build(Device));
        }

        /// <summary>旧経路が使う合成 SpecialAction（<c>Synthetic_Key_{device}_{control}</c>）。名前で状態を引くため、新経路の識別用 SA と同じ状態を指す。</summary>
        public SpecialAction SyntheticFor(DS4Controls control, int key, bool toggle = false, bool useScan = false)
            => Mapping.GetOrCreateSyntheticKeyAction(Device, (int)control, (uint)key, toggle, useScan);

        public DS4State Run(DS4State input) => _inner.RunMapCustom(input);

        public static DS4State Neutral() => MappingCharacterizationScope.NeutralState();

        public bool IsBeingTriggered(SpecialAction sa) => ActionManager.IsBeingTriggered(sa, Device);

        public bool IsToggledOn(SpecialAction sa) => ActionManager.GetStateFor(sa, Device)?.IsToggledOn ?? false;

        public void Dispose()
        {
            TestHost.GetService<ITriggerDispatchEngine>().OnDeviceDisconnected(Device);
            Mapping.ClearSyntheticActionCache();

            foreach (DS4Controls control in _touched)
                TestGlobals.Store.GetDS4CSetting(Device, control)?.Reset();

            _options.MigratedControlKinds.Clear();
            foreach (ControlBindingKind kind in _savedKinds) _options.MigratedControlKinds.Add(kind);

            ResetExtrasObservables();
            _inner.Dispose();
        }
    }
}
