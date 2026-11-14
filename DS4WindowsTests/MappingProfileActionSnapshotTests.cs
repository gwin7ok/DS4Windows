using System;
using System.Collections.Generic;
using System.Reflection;
using DS4Windows.Processing;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 特殊アクションを持つプロファイルでは、入力 1 フレームごとに、アクション名の一覧を写して列挙する。
    /// 写しの先は、デバイスごとの使い回しのバッファーとし、毎フレームの割り当てを起こさないこと。
    /// </summary>
    public class MappingProfileActionSnapshotTests
    {
        private static readonly MethodInfo Copy = typeof(Mapping).GetMethod("CopyProfileActionNames", BindingFlags.Static | BindingFlags.NonPublic);

        private static int Invoke(IReadOnlyList<string> names, ref string[] buffer)
        {
            Assert.NotNull(Copy);
            object[] args = { names, buffer };
            int count = (int)Copy.Invoke(null, args);
            buffer = (string[])args[1];
            return count;
        }

        [Fact]
        public void TheNames_AreCopied_AndTheBufferGrowsOnlyWhenNeeded()
        {
            string[] buffer = null;
            var names = new List<string> { "a", "b", "c" };

            Assert.Equal(3, Invoke(names, ref buffer));
            Assert.Equal(new[] { "a", "b", "c" }, new[] { buffer[0], buffer[1], buffer[2] });

            string[] first = buffer;
            names.RemoveAt(0);
            Assert.Equal(2, Invoke(names, ref buffer));
            Assert.Same(first, buffer);
            Assert.Equal("b", buffer[0]);

            for (int i = 0; i < 20; i++) names.Add("x" + i);
            Assert.Equal(22, Invoke(names, ref buffer));
            Assert.True(buffer.Length >= 22);
        }

        [Fact]
        public void ANullList_GivesZeroNames()
        {
            string[] buffer = null;
            Assert.Equal(0, Invoke(null, ref buffer));
        }

        [Fact]
        public void ACopy_DoesNotAllocate_WhenTheBufferIsBigEnough()
        {
            string[] buffer = null;
            var names = new List<string> { "a", "b", "c", "d" };
            Invoke(names, ref buffer);

            // リフレクションの呼び出し自体が割り当てるため、メソッドを直接呼べる形にして測る
            var direct = (CopyDelegate)Delegate.CreateDelegate(typeof(CopyDelegate), Copy);
            for (int i = 0; i < 100; i++) direct(names, ref buffer);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++) direct(names, ref buffer);
            long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / 5000;

            Assert.True(perCall == 0, $"1 回あたり {perCall} バイト");
        }

        private delegate int CopyDelegate(IReadOnlyList<string> names, ref string[] buffer);
    }
}
