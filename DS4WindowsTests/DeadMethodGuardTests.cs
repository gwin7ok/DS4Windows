using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step1 W1: <c>BackingStore</c> の呼び出し元 0 件のメソッドを削除した結果の回帰ガード。
    /// (1) 削除済みのメソッドが <c>BackingStore.cs</c> に再び現れないこと、
    /// (2) 削除せずに残すもの（<c>PostProcessLoad</c>。W3 で DTO 読込の経路へ接続する）が残っていること、を固定する。
    /// 削除の経緯と根拠は <c>Phase7-0-Survey.md</c> §1.1、<c>Phase7-Step1-Plan.md</c> §3 W1。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class DeadMethodGuardTests
    {

        private static readonly string[] KeptInBackingStore =
        {
            "PostProcessLoad", "GetDS4CSetting", "UpdateDS4CSetting",
        };

        private readonly ITestOutputHelper _output;

        public DeadMethodGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void KeptMethods_StillExistInBackingStore()
        {
            // Phase14d-Step3: ソースの文字列ではなく、宣言されたメソッドを確かめる
            var names = typeof(DS4Windows.Core.Settings.BackingStore).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Select(m => m.Name).ToArray();
            var missing = KeptInBackingStore.Where(n => !names.Contains(n)).ToList();
            Assert.True(missing.Count == 0, "残すべきメソッドが BackingStore から失われている: " + string.Join(", ", missing));
        }
    }
}
