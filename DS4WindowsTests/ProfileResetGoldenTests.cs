using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>BackingStore.ResetProfile</c>（プロファイルの読み込みの前に、スロットの設定を既定へ戻す）の守り。
    /// 全項目を既定と異なる値にしてから戻し、残った差（戻し漏れ）をゴールデンに固定する。
    /// ゴールデンが無いときは、生成して失敗する（内容を確認してコミットし、再実行する）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ProfileResetGoldenTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileResetGoldenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(8)]
        public void ResetProfile_AfterEveryItemWasChanged_LeavesOnlyTheGoldenDifferences(int device)
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore store = PersistenceHarness.NewStore();
                BackingStoreStateFiller.Fill(store, device);

                store.ResetProfile(device);

                string actual = BackingStoreStateDumper.Diff(
                    ctx.Normalize(BackingStoreStateDumper.Dump(PersistenceHarness.NewStore())),
                    ctx.Normalize(BackingStoreStateDumper.Dump(store)));
                GoldenFile.AssertMatches($"profile-reset-slot{device}.state.txt", actual, _output);
            }
        }
    }
}
