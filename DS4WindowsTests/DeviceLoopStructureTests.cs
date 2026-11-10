using System;
using System.Linq;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力機種の構造の検査: 全機種が共通の読み取りの骨組み（<see cref="InputLoopDevice"/>）に載り、
    /// 基底の <see cref="DS4Device"/> は自前のループを持たない抽象クラスであること。
    /// </summary>
    public class DeviceLoopStructureTests
    {
        [Fact]
        public void TheBaseDevice_IsAbstract_AndEveryConcreteDevice_RunsOnTheCommonLoop()
        {
            Assert.True(typeof(DS4Device).IsAbstract);

            Type[] concrete = typeof(DS4Device).Assembly.GetTypes()
                .Where(t => typeof(DS4Device).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

            Assert.NotEmpty(concrete);
            Assert.All(concrete, t => Assert.True(typeof(InputLoopDevice).IsAssignableFrom(t), t.Name + " は InputLoopDevice を継承していない"));
            Assert.Contains(typeof(DualShock4Device), concrete);
            Assert.Contains(typeof(DualSenseDevice), concrete);
            Assert.Contains(typeof(SwitchProDevice), concrete);
            Assert.Contains(typeof(ProtocolDevice), concrete);
        }

        [Fact]
        public void TheBaseDevice_HasNoReadLoopOfItsOwn()
        {
            Assert.Null(typeof(DS4Device).GetMethod("performDs4Input",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
        }
    }
}
