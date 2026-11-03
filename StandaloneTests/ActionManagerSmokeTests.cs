using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class ActionManagerSmokeTests
    {
        [TestMethod]
        public void ActionDoneFlagRoundtrip()
        {
            // Phase13-Step4: 静的 ActionManager（と、それを支えた ServiceProviderHolder）は廃止した。管理役（DefaultActionManager）を直接使う。
            // Func<IMapping> は切断時の後始末（ClearDeviceState）にだけ使われ、このテストでは呼ばれない。
            var manager = new DS4Windows.Actions.DefaultActionManager(() => null);

            // Arrange: create a lightweight SpecialAction
            var sa = TestHelpers.CreateKeyAction("smoke_test", "L1", "32");

            // Act: get state and flip IsToggledOn
            var st = manager.GetStateFor(sa, 0);
            Assert.IsNotNull(st, "Action state should not be null");
            st.IsToggledOn = true;

            // Assert: value persisted
            var st2 = manager.GetStateFor(sa, 0);
            Assert.IsNotNull(st2);
            Assert.IsTrue(st2.IsToggledOn, "IsToggledOn should be true after set");

            // Clear entries and ensure new state resets flag
            manager.ClearAllEntries();
            var st3 = manager.GetStateFor(sa, 0);
            Assert.IsNotNull(st3);
            Assert.IsFalse(st3.IsToggledOn, "IsToggledOn should be false after ClearAllEntries");
        }
    }
}