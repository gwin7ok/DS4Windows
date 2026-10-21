using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W4（Phase10-Step4c-Plan.md §2 項目 1・§4 W4、ユーザー承認 2026-10-21。案 B）: Controls の束縛のうち、<b>デジタルな出力</b>
    /// （Key、Macro、Special、Extras、パッドのボタン、マウスのボタン・タッチパッドクリック・デジタルのホイール）は SA と同じ閾値（<see cref="ControlOutputNature.Digital"/>）で判定し、
    /// <b>アナログな出力</b>（パッドの軸・トリガー、マウスの移動、アナログのホイール、Default の軸の素通し）は従来の判定（<see cref="ControlOutputNature.Analog"/>）のままであることを固定する。
    /// 閾値そのものの値は <c>Phase10Step4cW0JudgementTableTests</c> が固定する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW4JudgementTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        [Fact]
        public void TheDigitalOutputs_UseTheSpecialActionThreshold_AndTheAnalogOnesKeepTheControlsOne()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.L1, 65);
            t.MapMacro(DS4Controls.R1, new[] { 65, 65 });
            t.MapButton(DS4Controls.Cross, X360Controls.A);            // パッドのボタン
            t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse);   // マウスのボタン
            t.MapButton(DS4Controls.Square, X360Controls.MouseUp);     // マウスの移動（アナログ）
            t.MapButton(DS4Controls.Triangle, X360Controls.LT);   // パッドのトリガー（アナログ）
            t.MapButton(DS4Controls.Share, X360Controls.LXNeg);        // パッドの軸（アナログ）
            t.MapExtras(DS4Controls.Options, "1,2,0,0,0,0,0,0,0");
            t.MapKey(DS4Controls.LXPos, 66);                           // 反対側（LXNeg）は素通し

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            var expected = new (string Prefix, DS4Controls Control, ControlOutputNature Judgement)[]
            {
                ("Synthetic_Key", DS4Controls.L1, ControlOutputNature.Digital),
                ("Synthetic_Macro", DS4Controls.R1, ControlOutputNature.Digital),
                ("Synthetic_Pad", DS4Controls.Cross, ControlOutputNature.Digital),
                ("Synthetic_Mouse", DS4Controls.Circle, ControlOutputNature.Digital),
                ("Synthetic_AnalogMouse", DS4Controls.Square, ControlOutputNature.Analog),
                ("Synthetic_Pad", DS4Controls.Triangle, ControlOutputNature.Analog),
                ("Synthetic_Pad", DS4Controls.Share, ControlOutputNature.Analog),
                ("Synthetic_Extras", DS4Controls.Options, ControlOutputNature.Digital),
                ("Synthetic_PadPassthrough", DS4Controls.LXNeg, ControlOutputNature.Analog),
                ("Synthetic_Key", DS4Controls.LXPos, ControlOutputNature.Digital),
            };

            foreach ((string prefix, DS4Controls control, ControlOutputNature judgement) in expected)
            {
                string name = $"{prefix}_{Device}_{(int)control}";
                IActionBinding b = bindings.SingleOrDefault(x => x.Source.name == name);
                Assert.True(b != null, name + " の束縛がない");
                Assert.True(judgement == ((ControlButtonInputAction)b.Input).OutputNature, name + " の判定");
            }
        }

        [Fact]
        public void ADigitalOutputWithToggle_UsesTheSpecialActionThreshold_AndAnAnalogOneWithToggleDoesNot()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.A).keyType = DS4KeyType.Toggle;            // パッドのボタン + Toggle
            t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse).keyType = DS4KeyType.Toggle;   // マウスのボタン + Toggle
            t.MapButton(DS4Controls.Share, X360Controls.LXNeg).keyType = DS4KeyType.Toggle;        // パッドの軸 + Toggle（アナログの出力を含む）
            t.MapButton(DS4Controls.Square, X360Controls.MouseUp).keyType = DS4KeyType.Toggle;     // マウスの移動 + Toggle

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            ControlOutputNature Of(string prefix, DS4Controls control)
                => ((ControlButtonInputAction)bindings.Single(x => x.Source.name == $"{prefix}_{Device}_{(int)control}").Input).OutputNature;

            Assert.Equal(ControlOutputNature.Digital, Of("Synthetic_Pad", DS4Controls.Cross));
            Assert.Equal(ControlOutputNature.Digital, Of("Synthetic_Mouse", DS4Controls.Circle));
            Assert.Equal(ControlOutputNature.Analog, Of("Synthetic_Pad", DS4Controls.Share));
            Assert.Equal(ControlOutputNature.Analog, Of("Synthetic_AnalogMouse", DS4Controls.Square));
        }
    }
}
