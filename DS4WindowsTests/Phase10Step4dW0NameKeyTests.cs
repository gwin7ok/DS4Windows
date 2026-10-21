using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4d W0（Phase10-Step4d-Plan.md §4 W0）: プロファイルと SA を<b>名前で識別・参照</b>している現行の挙動を固定する特性化テスト。
    /// 名前変更・削除の追従（リンクプロファイル、自動プロファイル、全プロファイルの <c>&lt;ProfileActions&gt;</c>）、名前の比較規則、保存形式（ID がないこと）、
    /// 実行時の鍵（<c>ActionManager</c> の登録簿が SA の名前で状態を引くこと）を固定する。製品コードは変えない。
    /// W1 以降で ID を足し、参照を ID に替えるとき、変わらないべき部分（既存の形式、名前の併記）と、替わる部分（追従）を、このテストで見分ける。
    /// SA／Controls の Load Profile の切り替え先の追従は、<c>ProfileRenamePropagationTests</c>・<c>Phase10Step4W6ControlProfileTests</c> が固定している。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4dW0NameKeyTests
    {
        private static ProfileRepository NewRepository(PersistenceGoldenContext ctx, BackingStore store)
            => TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService(store),
                TestRepositoryFactory.CreateProfileXmlStore(store), new PathService(ctx.AppData), null, store);

        private static string ProfilesDir(PersistenceGoldenContext ctx) => Path.Combine(ctx.AppData, "Profiles");

        private static void WriteProfile(PersistenceGoldenContext ctx, string name, string profileActions)
        {
            string node = profileActions == null ? string.Empty : "<ProfileActions>" + profileActions + "</ProfileActions>";
            File.WriteAllText(Path.Combine(ProfilesDir(ctx), name + ".xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><Other>1</Other>" + node + "</DS4Windows>");
        }

        private static string ReadProfileActions(PersistenceGoldenContext ctx, string name)
        {
            var doc = new XmlDocument();
            doc.Load(Path.Combine(ProfilesDir(ctx), name + ".xml"));
            return doc.SelectSingleNode("DS4Windows/ProfileActions")?.InnerText;
        }

        // ---- リンクプロファイル ----

        // Phase10-Step4d W7: 以前ここにあった、リンクプロファイル（ReplaceLinkedProfileName）と自動プロファイル（ReplaceProfileName）の名前変更の追従のテストは、撤去したメンバーのため削除した。
        // 同じ振る舞い（名前変更に追従し、保存し直すと新しい名前と ID が書かれる）は、Phase10Step4dW5dTests／Phase10Step4dW7FollowUpTests が固定する

        [Fact]
        public void TheActionRename_RewritesEveryProfileThatListsTheOldName_ComparingByOrdinal()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfile(ctx, "A", "Cross/Old/Square");
            WriteProfile(ctx, "B", "old");                // 大文字小文字は区別する（SA の名前は Ordinal）
            WriteProfile(ctx, "C", "Old10/Keep");         // 部分一致では書き換えない
            WriteProfile(ctx, "D", null);                 // 節がない
            WriteProfile(ctx, "E", "New/Old");            // 変更後の名前がすでにあれば、重複は 1 つにする

            IReadOnlyList<string> changed = NewRepository(ctx, PersistenceHarness.NewStore()).ReplaceActionReference("Old", "New");

            Assert.Equal(new[] { "A", "E" }, changed.OrderBy(x => x).ToArray());
            Assert.Equal("Cross/New/Square", ReadProfileActions(ctx, "A"));
            Assert.Equal("old", ReadProfileActions(ctx, "B"));
            Assert.Equal("Old10/Keep", ReadProfileActions(ctx, "C"));
            Assert.Null(ReadProfileActions(ctx, "D"));
            Assert.Equal("New", ReadProfileActions(ctx, "E"));
        }

        [Fact]
        public void TheActionRename_IgnoresBlankOrUnchangedNames()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfile(ctx, "A", "Old");
            ProfileRepository repo = NewRepository(ctx, PersistenceHarness.NewStore());

            Assert.Empty(repo.ReplaceActionReference(" ", "New"));
            Assert.Empty(repo.ReplaceActionReference("Old", string.Empty));
            Assert.Empty(repo.ReplaceActionReference("Old", "Old"));
            Assert.Equal("Old", ReadProfileActions(ctx, "A"));
        }

        [Fact]
        public void TheActionRemoval_TakesTheNameOutOfEveryProfile()
        {
            using var ctx = new PersistenceGoldenContext();
            WriteProfile(ctx, "A", "Cross/Old/Square");
            WriteProfile(ctx, "B", "Old");
            WriteProfile(ctx, "C", "Keep");

            IReadOnlyList<string> changed = NewRepository(ctx, PersistenceHarness.NewStore()).RemoveActionReference("Old");

            Assert.Equal(new[] { "A", "B" }, changed.OrderBy(x => x).ToArray());
            Assert.Equal("Cross/Square", ReadProfileActions(ctx, "A"));
            Assert.Equal(string.Empty, ReadProfileActions(ctx, "B"));
            Assert.Equal("Keep", ReadProfileActions(ctx, "C"));
        }

        [Fact]
        public void TheAppliedProfilesUsingAnAction_AreFoundByTheSlotsActionNames()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.profileActions[0] = new List<string> { "Used", "Other" };
            store.profilePath[0] = "ProfileOne";
            store.profileActions[1] = new List<string> { "Other" };
            store.profilePath[1] = "ProfileTwo";
            store.profileActions[2] = new List<string> { "Used" };
            store.profilePath[2] = "profileone";   // 同じプロファイル（大文字小文字違い）は 1 つにまとめる

            IReadOnlyList<string> found = NewRepository(ctx, store).GetAppliedProfilesUsingAction("Used");

            Assert.Equal(new[] { "ProfileOne" }, found.ToArray());
            Assert.Empty(NewRepository(ctx, store).GetAppliedProfilesUsingAction("used"));   // SA の名前は大文字小文字を区別する
        }

        // ---- 保存形式（ID がない現行の形）----

        [Fact]
        public void TheProfileXml_ListsTheActionsByName()
        {
            // Phase10-Step4d W1 で、プロファイル自身の ID（<ProfileId>）が足された。SA への参照は、W3 までは名前のまま
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            store.profileActions[PersistenceHarness.Slot] = new List<string> { "ActionA", "ActionB" };

            string xml = PersistenceHarness.SaveProfile(ctx, store, "Step4dW0Format");

            Assert.Contains("<ProfileActions>ActionA/ActionB</ProfileActions>", xml);
            Assert.DoesNotContain("ActionId", xml);
        }

        // Phase10-Step4d W2: 以前ここにあった「Actions.xml に ID がない」の検査は、W2 で Action に Id 属性を足したため、Phase10Step4dW2ActionIdTests に置き換えた

        // ---- 実行時の鍵 ----

        [Fact]
        public void TheActionManager_FindsTheStateByTheActionsName()
        {
            IManagedActionManager manager = TestHost.GetService<IManagedActionManager>();
            string name = "p10s4dw0_" + Guid.NewGuid().ToString("N");

            ActionInstanceState first = manager.GetStateFor(new SpecialAction(name, "Cross", "Macro", "65"), 0);
            ActionInstanceState sameName = manager.GetStateFor(new SpecialAction(name, "Circle", "Key", "66"), 0);   // 名前が同じなら、別の定義でも、同じ状態
            ActionInstanceState otherName = manager.GetStateFor(new SpecialAction(name + "_other", "Cross", "Macro", "65"), 0);
            ActionInstanceState otherDevice = manager.GetStateFor(new SpecialAction(name, "Cross", "Macro", "65"), 1);

            Assert.NotNull(first);
            Assert.Same(first, sameName);
            Assert.NotSame(first, otherName);
            Assert.NotSame(first, otherDevice);
        }

        // ---- 画面の経路（ソースの検査）----

        // Phase10-Step4d W7: MainWindow の名前変更の追従の検査は、Phase10Step4dW7FollowUpTests に置き換えた（SA の名前変更が ReplaceActionReference を呼ぶ検査は、同ファイルに移した）
    }
}
