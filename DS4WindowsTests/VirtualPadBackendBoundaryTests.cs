using System;
using System.Linq;
using DS4Windows.Output.Backend;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 仮想パッドのドライバー（ViGEm）の型を参照してよいのは <c>DS4Windows.Output.Backend</c> だけ、という境界（規則 L9）と、
    /// 自前のボタン・十字キーの定数がドライバーの値と同じであること（差分テスト）。
    /// </summary>
    public class VirtualPadBackendBoundaryTests
    {
        [Fact]
        public void OnlyTheBackendNamespace_ReferencesTheViGEmTypes()
        {
            var sources = LayerDependencyScanner.TopLevelTypesReferencingNamespace("Nefarius.ViGEm");

            // 空振りの防止: バックエンドの実装からは ViGEm の型が見えること
            Assert.Contains("DS4Windows.Output.Backend.ViGEmVirtualPadBackend", sources);
            var outside = sources.Where(t => !t.StartsWith("DS4Windows.Output.Backend.", StringComparison.Ordinal)).OrderBy(t => t, StringComparer.Ordinal).ToList();
            Assert.True(outside.Count == 0, "ViGEm の型を参照している（Output.Backend の外）: " + string.Join(", ", outside));
        }

        [Fact]
        public void Xbox360Buttons_MatchTheDriverValues()
        {
            Assert.Equal(Xbox360Buttons.Up, Xbox360Button.Up.Value);
            Assert.Equal(Xbox360Buttons.Down, Xbox360Button.Down.Value);
            Assert.Equal(Xbox360Buttons.Left, Xbox360Button.Left.Value);
            Assert.Equal(Xbox360Buttons.Right, Xbox360Button.Right.Value);
            Assert.Equal(Xbox360Buttons.Start, Xbox360Button.Start.Value);
            Assert.Equal(Xbox360Buttons.Back, Xbox360Button.Back.Value);
            Assert.Equal(Xbox360Buttons.LeftThumb, Xbox360Button.LeftThumb.Value);
            Assert.Equal(Xbox360Buttons.RightThumb, Xbox360Button.RightThumb.Value);
            Assert.Equal(Xbox360Buttons.LeftShoulder, Xbox360Button.LeftShoulder.Value);
            Assert.Equal(Xbox360Buttons.RightShoulder, Xbox360Button.RightShoulder.Value);
            Assert.Equal(Xbox360Buttons.Guide, Xbox360Button.Guide.Value);
            Assert.Equal(Xbox360Buttons.A, Xbox360Button.A.Value);
            Assert.Equal(Xbox360Buttons.B, Xbox360Button.B.Value);
            Assert.Equal(Xbox360Buttons.X, Xbox360Button.X.Value);
            Assert.Equal(Xbox360Buttons.Y, Xbox360Button.Y.Value);
        }

        [Fact]
        public void Ds4Buttons_MatchTheDriverValues()
        {
            Assert.Equal(Ds4Buttons.ThumbRight, DualShock4Button.ThumbRight.Value);
            Assert.Equal(Ds4Buttons.ThumbLeft, DualShock4Button.ThumbLeft.Value);
            Assert.Equal(Ds4Buttons.Options, DualShock4Button.Options.Value);
            Assert.Equal(Ds4Buttons.Share, DualShock4Button.Share.Value);
            Assert.Equal(Ds4Buttons.TriggerRight, DualShock4Button.TriggerRight.Value);
            Assert.Equal(Ds4Buttons.TriggerLeft, DualShock4Button.TriggerLeft.Value);
            Assert.Equal(Ds4Buttons.ShoulderRight, DualShock4Button.ShoulderRight.Value);
            Assert.Equal(Ds4Buttons.ShoulderLeft, DualShock4Button.ShoulderLeft.Value);
            Assert.Equal(Ds4Buttons.Triangle, DualShock4Button.Triangle.Value);
            Assert.Equal(Ds4Buttons.Circle, DualShock4Button.Circle.Value);
            Assert.Equal(Ds4Buttons.Cross, DualShock4Button.Cross.Value);
            Assert.Equal(Ds4Buttons.Square, DualShock4Button.Square.Value);
            Assert.Equal(Ds4SpecialButtons.Ps, DualShock4SpecialButton.Ps.Value);
            Assert.Equal(Ds4SpecialButtons.Touchpad, DualShock4SpecialButton.Touchpad.Value);
        }

        [Fact]
        public void Ds4DPadDirection_MatchesTheDriverValues()
        {
            Assert.Equal((ushort)Ds4DPadDirection.None, DualShock4DPadDirection.None.Value);
            Assert.Equal((ushort)Ds4DPadDirection.North, DualShock4DPadDirection.North.Value);
            Assert.Equal((ushort)Ds4DPadDirection.Northeast, DualShock4DPadDirection.Northeast.Value);
            Assert.Equal((ushort)Ds4DPadDirection.East, DualShock4DPadDirection.East.Value);
            Assert.Equal((ushort)Ds4DPadDirection.Southeast, DualShock4DPadDirection.Southeast.Value);
            Assert.Equal((ushort)Ds4DPadDirection.South, DualShock4DPadDirection.South.Value);
            Assert.Equal((ushort)Ds4DPadDirection.Southwest, DualShock4DPadDirection.Southwest.Value);
            Assert.Equal((ushort)Ds4DPadDirection.West, DualShock4DPadDirection.West.Value);
            Assert.Equal((ushort)Ds4DPadDirection.Northwest, DualShock4DPadDirection.Northwest.Value);
        }

        [Fact]
        public void TheFakeBackend_FollowsTheStartStopContract()
        {
            var backend = new FakeVirtualPadBackend { StartSucceeds = false };
            backend.Start();
            Assert.False(backend.IsStarted);
            Assert.Throws<InvalidOperationException>(() => backend.CreateXbox360Target());

            backend.StartSucceeds = true;
            backend.Start();
            Assert.True(backend.IsStarted);
            Assert.NotNull(backend.CreateDs4Target());
            backend.Stop();
            Assert.False(backend.IsStarted);
            Assert.Equal(2, backend.StartCount);
            Assert.Equal(1, backend.StopCount);
        }
    }
}
