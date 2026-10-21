using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0（Phase10-Step4c-Plan.md §2 項目 2・3・§4 W0）: Controls の束縛の種類ごとの<b>方針</b>
    /// （Fresh-press ガード、トリガーのボタンの抑制・出力の消去、離したときの Stop、実行中のブロック、復帰）の現行の値を、1 枚の表で固定する特性化テスト。製品コードは変えない。
    /// SA の種別ごとの表は <c>Phase10Step4W0SaBindingTableTests</c>。W1（項目 2: Fresh-press ガードの規則を 1 か所の表にまとめる。挙動は変えない）は、
    /// この表が変わらないことで守る。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0PolicyTableTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        private sealed class Row
        {
            public string Prefix;
            public DS4Controls Control;
            public bool Fresh, Suppress, Reset, Erase, Stop, Blocks;
            public UntriggerPolicy Untrigger = UntriggerPolicy.None;
            public ControlBindingKind Kind;
        }

        private static Row Controls(string prefix, DS4Controls control, ControlBindingKind kind, bool fresh, bool erase, bool blocks = false, bool stop = true,
            UntriggerPolicy untrigger = UntriggerPolicy.None)
            => new Row { Prefix = prefix, Control = control, Kind = kind, Fresh = fresh, Suppress = false, Reset = false, Erase = erase, Stop = stop, Blocks = blocks, Untrigger = untrigger };

        [Fact]
        public void EveryControlsBindingKind_HasTheCurrentPolicy()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.L1, 65);
            t.MapMacro(DS4Controls.R1, new[] { 65, 65 });
            t.MapButton(DS4Controls.Cross, X360Controls.A);
            t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse);
            t.MapButton(DS4Controls.Square, X360Controls.MouseUp);
            t.MapExtras(DS4Controls.Triangle, "1,2,0,0,0,0,0,0,0");
            t.MapKey(DS4Controls.LXPos, 66);   // 反対側の向きが割り当て済みの軸の Default は、素通しの束縛になる（LXNeg）
            t.MapSpecial(DS4Controls.Share, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.DisconnectBT });
            t.MapSpecial(DS4Controls.Options, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.GyroCalibrate });
            t.MapSpecial(DS4Controls.Mute, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = "C:\\p10step4c-nonexistent\\a.exe" });
            // Capture／SideL などの拡張ボタンは、コントローラーが接続されたときにだけ束ねられる（ControlSettingsGroup.EstablishExtraButtons）ため、通常のボタンを使う
            t.MapSpecial(DS4Controls.L3, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "Other" });
            t.MapSpecial(DS4Controls.R3, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = "Other", AutomaticUntrigger = true });

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            // (名前の接頭辞, ボタン, 区分, Fresh-press ガードを見る, 毎フレーム消す, 実行中をブロック, 離したとき Stop, 復帰)
            // 抑制（SuppressesTriggerButtons）と出力のリセット（ResetsTriggerOutput）は、Controls の束縛ではすべて偽
            var expected = new[]
            {
                Controls("Synthetic_Key", DS4Controls.L1, ControlBindingKind.Key, fresh: false, erase: true),
                Controls("Synthetic_Macro", DS4Controls.R1, ControlBindingKind.Macro, fresh: true, erase: true),
                Controls("Synthetic_Pad", DS4Controls.Cross, ControlBindingKind.ButtonPad, fresh: false, erase: true),
                Controls("Synthetic_Mouse", DS4Controls.Circle, ControlBindingKind.ButtonMouse, fresh: false, erase: true),
                Controls("Synthetic_AnalogMouse", DS4Controls.Square, ControlBindingKind.ButtonAnalogMouse, fresh: false, erase: true),
                Controls("Synthetic_Extras", DS4Controls.Triangle, ControlBindingKind.Extras, fresh: false, erase: false),
                Controls("Synthetic_PadPassthrough", DS4Controls.LXNeg, ControlBindingKind.ButtonPad, fresh: false, erase: false),
                Controls("Synthetic_Key", DS4Controls.LXPos, ControlBindingKind.Key, fresh: false, erase: true),
                Controls("Synthetic_Special", DS4Controls.Share, ControlBindingKind.Special, fresh: true, erase: true),
                Controls("Synthetic_Special", DS4Controls.Options, ControlBindingKind.Special, fresh: true, erase: true),
                Controls("Synthetic_Special", DS4Controls.Mute, ControlBindingKind.Special, fresh: true, erase: true, blocks: true),
                Controls("Synthetic_Special", DS4Controls.L3, ControlBindingKind.Special, fresh: true, erase: true, blocks: true),
                Controls("Synthetic_Special", DS4Controls.R3, ControlBindingKind.Special, fresh: true, erase: true, blocks: true, stop: false, untrigger: UntriggerPolicy.Automatic),
            };

            Assert.Equal(expected.Length, bindings.Count);
            foreach (Row row in expected)
            {
                string name = $"{row.Prefix}_{Device}_{(int)row.Control}";
                IActionBinding b = bindings.SingleOrDefault(x => x.Source.name == name);
                Assert.True(b != null, name + " の束縛がない");

                var input = Assert.IsType<ControlButtonInputAction>(b.Input);
                Assert.True(row.Kind == input.Kind, name + " の区分");
                Assert.True(row.Fresh == b.Policy.RespectsFreshPressGuard, name + " の Fresh-press ガード");
                Assert.False(b.Policy.SuppressesTriggerButtons, name + " の抑制");
                Assert.False(b.Policy.ResetsTriggerOutput, name + " の出力のリセット");
                Assert.True(row.Erase == b.Policy.ErasesOutputEveryFrame, name + " の毎フレームの消去");
                Assert.True(row.Stop == b.Policy.StopOnRelease, name + " の離したときの Stop");
                Assert.True(row.Blocks == b.Policy.BlocksWhileExecuting, name + " の実行中のブロック");
                Assert.True(row.Untrigger == b.Policy.Untrigger, name + " の復帰");
                Assert.False(b.Policy.RoutesOutputsByGesture, name + " のジェスチャー");
            }
        }

        [Fact]
        public void TheFreshPressGuard_IsSeenOnlyByMacroAndTheSpecialTypes_InControls()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.L1, 65);
            t.MapMacro(DS4Controls.R1, new[] { 65, 65 });
            t.MapButton(DS4Controls.Cross, X360Controls.A);
            t.MapSpecial(DS4Controls.Share, new ControlSpecialItem { Type = SpecialAction.ActionTypeId.GyroCalibrate });

            var seeing = TestHost.GetService<IControlBindingBuilder>().Build(Device)
                .Where(b => b.Policy.RespectsFreshPressGuard)
                .Select(b => ((ControlButtonInputAction)b.Input).Kind)
                .OrderBy(k => k)
                .ToArray();

            Assert.Equal(new[] { ControlBindingKind.Macro, ControlBindingKind.Special }, seeing);
        }
    }
}
