using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class TestSpecialActionUtils
    {
        [TestMethod]
        public void NormalizeActionName_TrimsWhitespace()
        {
            string input = "  My Action Name  ";
            string expected = "My Action Name";
            string actual = Util.NormalizeActionName(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NormalizeActionName_NullReturnsEmpty()
        {
            string input = null;
            string expected = string.Empty;
            string actual = Util.NormalizeActionName(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScpUtilAndUtil_NormalizeConsistent()
        {
            // Phase6-Step9b-4（F14）: 旧実装は Util.NormalizeActionName 同士を比較しており、常に通っていた。
            // Global（ScpUtil.cs）と Util は、それぞれ独立した実装を持つため、両者の結果が一致することを検証する。
            string[] inputs = { "  Action\t\n", "Action", "  My Action Name  ", string.Empty, "   ", null };
            foreach (string input in inputs)
            {
                string fromUtil = Util.NormalizeActionName(input);
                string fromGlobal = Global.NormalizeActionName(input);
                Assert.AreEqual(fromUtil, fromGlobal, $"入力 '{input ?? "(null)"}' で Util と Global の結果が異なる");
            }

            // 期待値そのものも固定する（両者が同じ誤りをしていても検出できるように）
            Assert.AreEqual("Action", Global.NormalizeActionName("  Action\t\n"));
            Assert.AreEqual(string.Empty, Global.NormalizeActionName(null));
        }
    }
}
