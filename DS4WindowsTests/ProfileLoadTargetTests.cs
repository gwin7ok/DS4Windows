using System;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-d: <see cref="IProfileLoadTarget"/>（<see cref="ProfileLoadTarget"/>）が、鎖の <c>control</c> 引数（<c>ControlService</c>）と
    /// 同じ結果を返すこと（6-e で型を置き換えても挙動が変わらないことの根拠）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ProfileLoadTargetTests
    {
        public ProfileLoadTargetTests()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            TestHost.GetService<ControlService>();
        }

        [Fact]
        public void TheTarget_IsResolvedFromTheContainer_AsASingleton()
        {
            var a = TestHost.GetService<IProfileLoadTarget>();
            var b = TestHost.GetService<IProfileLoadTarget>();
            Assert.NotNull(a);
            Assert.Same(a, b);
            Assert.IsType<ProfileLoadTarget>(a);
        }

        [Fact]
        public void TheArrays_AreTheSameAsTheOnesOfControlService()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            var slots = TestHost.GetService<IDeviceSlotState>();

            for (int i = 0; i < 4; i++)
            {
                Assert.Same(slots.TouchPads[i], target.GetTouchPad(i));
                Assert.Equal(TestHost.GetService<DS4Windows.Processing.Abstractions.IVirtualPadService>().HasOutputDevice(i), target.HasOutputDevice(i));
                Assert.Same(slots.DS4Controllers[i], target.GetController(i));
            }

            var device = TestDs4Device.Create();
            DS4Device old = slots.DS4Controllers[0];
            slots.DS4Controllers[0] = device;
            try
            {
                Assert.Same(device, target.GetController(0));
            }
            finally
            {
                slots.DS4Controllers[0] = old;
            }
        }

        [Fact]
        public void OutOfRange_ThrowsLikeTheArraysOfControlService()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            Assert.Throws<IndexOutOfRangeException>(() => target.GetController(ControllerLimits.MaxControllerCount));
            Assert.Throws<IndexOutOfRangeException>(() => target.GetTouchPad(ControllerLimits.MaxControllerCount));
        }

        [Fact]
        public void PreLoadReset_ResetsTheFlickProgress()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            var stick = TestHost.GetService<StickMotionState>();
            const int ind = 0;

            stick.FlickMappingData[ind].flickProgress = 5.0;
            stick.FlickMappingData[ind].flickSize = 7.0;
            target.PreLoadReset(ind);
            Assert.Equal(FlickStickMappingData.DEFAULT_FLICK_PROGRESS, stick.FlickMappingData[ind].flickProgress);
            Assert.Equal(FlickStickMappingData.DEFAULT_FLICK_SIZE, stick.FlickMappingData[ind].flickSize);

        }

        [Fact]
        public void PreLoadReset_AtTheTestProfileIndex_DoesNothing()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            var stick = TestHost.GetService<StickMotionState>();
            int ind = ControllerLimits.TestProfileIndex;
            if (ind >= stick.FlickMappingData.Length) { target.PreLoadReset(ind); return; }   // 配列の外なら、例外にならないこと自体が検査

            stick.FlickMappingData[ind].flickProgress = 5.0;
            try
            {
                target.PreLoadReset(ind);
                Assert.Equal(5.0, stick.FlickMappingData[ind].flickProgress);
            }
            finally
            {
                stick.FlickMappingData[ind].Reset();
            }
        }

        [Fact]
        public void Constructor_RequiresAllDependencies()
        {
            var slots = TestHost.GetService<IDeviceSlotState>();
            var pad = TestHost.GetService<IVirtualPadService>();
            var setup = TestHost.GetService<IDeviceConnectionSetup>();
            var toggle = TestHost.GetService<ITouchpadToggleService>();
            var stick = TestHost.GetService<StickMotionState>();

            Assert.Throws<ArgumentNullException>(() => new ProfileLoadTarget(null, pad, setup, toggle, stick));
            Assert.Throws<ArgumentNullException>(() => new ProfileLoadTarget(slots, null, setup, toggle, stick));
            Assert.Throws<ArgumentNullException>(() => new ProfileLoadTarget(slots, pad, null, toggle, stick));
            Assert.Throws<ArgumentNullException>(() => new ProfileLoadTarget(slots, pad, setup, null, stick));
            Assert.Throws<ArgumentNullException>(() => new ProfileLoadTarget(slots, pad, setup, toggle, null));
        }
    }
}
