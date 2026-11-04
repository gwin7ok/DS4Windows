using System;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-d: <see cref="IProfileLoadTarget"/>（<see cref="ProfileLoadTarget"/>）が、鎖の <c>control</c> 引数（<c>ControlService</c>）と
    /// 同じ結果を返すこと（6-e で型を置き換えても挙動が変わらないことの根拠）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step6ProfileLoadTargetTests
    {
        public Phase13Step6ProfileLoadTargetTests()
        {
            DS4WinWPF.AppHost.CreateHost();
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
            var control = TestHost.GetService<ControlService>();
            var slots = TestHost.GetService<IDeviceSlotState>();

            for (int i = 0; i < 4; i++)
            {
                Assert.Same(control.touchPad[i], target.GetTouchPad(i));
                Assert.Same(control.outputDevices[i], target.GetOutputDevice(i));
                Assert.Same(control.DS4Controllers[i], target.GetController(i));
            }

            var device = TestDs4Device.Create();
            DS4Device old = slots.DS4Controllers[0];
            slots.DS4Controllers[0] = device;
            try
            {
                Assert.Same(device, target.GetController(0));
                Assert.Same(device, control.GetController(0));
            }
            finally
            {
                slots.DS4Controllers[0] = old;
            }
        }

        [Fact]
        public void GetController_OutOfRange_IsNull_AndTheArraysThrowAsBefore()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            Assert.Null(target.GetController(-1));
            Assert.Null(target.GetController(ControllerLimits.MaxControllerCount));
            Assert.Throws<IndexOutOfRangeException>(() => target.GetTouchPad(ControllerLimits.MaxControllerCount));
        }

        [Fact]
        public void PreLoadReset_ResetsTheFlickProgress_LikeControlService()
        {
            var target = TestHost.GetService<IProfileLoadTarget>();
            var control = TestHost.GetService<ControlService>();
            var stick = TestHost.GetService<StickMotionState>();
            const int ind = 0;

            stick.FlickMappingData[ind].flickProgress = 5.0;
            stick.FlickMappingData[ind].flickSize = 7.0;
            target.PreLoadReset(ind);
            Assert.Equal(FlickStickMappingData.DEFAULT_FLICK_PROGRESS, stick.FlickMappingData[ind].flickProgress);
            Assert.Equal(FlickStickMappingData.DEFAULT_FLICK_SIZE, stick.FlickMappingData[ind].flickSize);

            stick.FlickMappingData[ind].flickProgress = 5.0;
            control.PreLoadReset(ind);   // 旧経路（6-e で削除するまで残す）。同じ結果になる
            Assert.Equal(FlickStickMappingData.DEFAULT_FLICK_PROGRESS, stick.FlickMappingData[ind].flickProgress);
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
