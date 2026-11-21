#nullable enable
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 自動プロファイルの行の選択位置と、保存側のプロファイル名の対応
    /// （0＝なし、1 からがプロファイル一覧の位置。拡張のコントローラー 5〜8 は、使うときだけ）。
    /// </summary>
    public class AutoProfileSelectionMapperTests
    {
        private static (AutoProfileSelectionMapper mapper, ProfileList list) Create(params string[] names)
        {
            var paths = TestHost.GetService<IPathService>();
            var list = new ProfileList(paths);
            foreach (string name in names)
                list.ProfileListCol.Add(new ProfileEntity(paths) { Name = name });
            return (new AutoProfileSelectionMapper(list), list);
        }

        private static ProgramItem Item() => new ProgramItem(@"C:\Games\game.exe", usingExpandedControllers: true);

        [Fact]
        public void WriteNames_ConvertsPositionsToNames_AndZeroToNone()
        {
            var (mapper, _) = Create("A", "B");
            var item = Item();
            item.SelectedIndexCon1 = 2;
            item.SelectedIndexCon2 = 0;
            item.SelectedIndexCon3 = 1;
            var entry = new AutoProfileEntity(item.Path, "t");

            mapper.WriteNames(item, entry, usingExpandedControllers: false);

            Assert.Equal("B", entry.ProfileNames[0]);
            Assert.Equal(AutoProfileEntity.NONE_STRING, entry.ProfileNames[1]);
            Assert.Equal("A", entry.ProfileNames[2]);
        }

        [Fact]
        public void WriteNames_TouchesControllers5To8_OnlyWhenExpanded()
        {
            var (mapper, _) = Create("A");
            var item = Item();
            item.SelectedIndexCon5 = 1;
            var basic = new AutoProfileEntity(item.Path, "t");
            var expanded = new AutoProfileEntity(item.Path, "t");
            basic.ProfileNames[4] = "Keep";

            mapper.WriteNames(item, basic, usingExpandedControllers: false);
            mapper.WriteNames(item, expanded, usingExpandedControllers: true);

            Assert.Equal("Keep", basic.ProfileNames[4]);
            Assert.Equal("A", expanded.ProfileNames[4]);
        }

        [Fact]
        public void ReadIndexes_ConvertsNamesToPositions_NoneToZero_AndLeavesUnknownNamesAlone()
        {
            var (mapper, _) = Create("A", "B");
            var item = Item();
            item.SelectedIndexCon4 = 7;
            var entry = new AutoProfileEntity(item.Path, "t");
            entry.ProfileNames[0] = "B";
            entry.ProfileNames[1] = AutoProfileEntity.NONE_STRING;
            entry.ProfileNames[2] = "Missing";
            item.SelectedIndexCon3 = 9;

            mapper.ReadIndexes(item, entry, usingExpandedControllers: false);

            Assert.Equal(2, item.SelectedIndexCon1);
            Assert.Equal(0, item.SelectedIndexCon2);
            Assert.Equal(9, item.SelectedIndexCon3);   // 一覧に無い名前は、位置を変えない
        }
    }
}
