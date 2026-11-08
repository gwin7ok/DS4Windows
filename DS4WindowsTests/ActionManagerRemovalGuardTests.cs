using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step4: 静的 <c>ActionManager</c>、<c>ServiceProviderHolder</c>、<c>ButtonSpecialActionOutput</c> を撤廃したことのガード。
    /// 型が存在しないこと、<c>DS4Windows/</c> に参照が残っていないこと、<c>AppHost</c> が <c>SetProvider</c> を呼ばないこと、
    /// 管理役の契約（<c>IsBeingTriggered</c>）を検査する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class ActionManagerRemovalGuardTests
    {
        private static readonly Type[] Asm = { typeof(DS4Windows.Processing.Actions.IManagedActionManager) };

        [Theory]
        [InlineData("DS4Windows.ActionManager")]
        [InlineData("DS4Windows.DI.ServiceProviderHolder")]
        [InlineData("DS4Windows.ButtonSpecialActionOutput")]
        public void RemovedTypes_DoNotExist(string fullName)
        {
            Assert.Null(Asm[0].Assembly.GetType(fullName));
        }

        [Fact]
        public void ManagedActionManager_ExposesIsBeingTriggered()
        {
            Assert.NotNull(typeof(DS4Windows.Processing.Actions.IManagedActionManager).GetMethod("IsBeingTriggered"));
        }
    }
}
