using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力デバイス種別の配列（OutDevTypeTemp／ActiveOutDevType）の所有権を、Global から OutputSlotService へ移したことの回帰ガード。
    /// - Global に配列がない。BackingStore は Global から読まず、自身の ActiveOutDevType を読む。
    /// - 配列の実体は OutputSlotService の readonly フィールド（参照は再代入されない。ControlService のキャッシュの前提）。
    /// - BackingStore への配列の受け渡しは IOutputSlotService の登録内で行い、BackingStore の登録内で IOutputSlotService を解決しない
    ///   （IOutputSlotStore → IPathService → BackingStore と循環し、スタックオーバーフローになる）。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は docs-forDIMG/MadeByAgent/Archive/Phase7-Step6-Plan.md。
    /// </summary>
    public class OutputTypeOwnershipGuardTests
    {
        private readonly ITestOutputHelper _output;

        public OutputTypeOwnershipGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BackingStore_ReadsItsOwnActiveOutDevType()
        {
            var prop = typeof(DS4Windows.Core.Settings.BackingStore).GetProperty("ActiveOutDevType");
            Assert.NotNull(prop);
            Assert.Equal(typeof(DS4Windows.Core.Model.OutContType[]), prop.PropertyType);
            Assert.True(prop.CanRead && prop.CanWrite);

            string store = SourceText.ReadCodeWithPartials("DS4Windows/Core/Settings/BackingStore.cs");
            if (store == null) return;

            // プロファイルの読込（1 件）は LegacyProfileXmlSerializer へ移した（store.ActiveOutDevType）。合計は変わらない
            // プリセットの読込も ProfilePresets へ移した。3 つのソースを合わせて 7 件（廃止したジャイロカメラのプリセットの分を除く）
            string serializer = SourceText.ReadCode("DS4Windows/Core/Persistence/LegacyProfileXmlSerializer.cs");
            string presets = SourceText.ReadCodeWithPartials("DS4Windows/Core/Persistence/ProfilePresets.cs");
            if (serializer == null || presets == null) return;
            Assert.Equal(7, Regex.Matches(store + serializer + presets, @"OutContType oldContType = (store\.)?ActiveOutDevType\[device\];").Count);
        }

        [Fact]
        public void TheArrays_AreOwnedByTheDeviceTypeState_AndTheServiceForwardsThem()
        {
            // 配列の持ち主は IOutputDeviceTypeState。一時の種別は既定の種別、実行時の種別は None で埋める。サービスは同じ配列を返すだけ
            var state = new DS4Windows.Output.Services.OutputDeviceTypeState();
            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount, state.OutDevTypeTemp.Length);
            Assert.All(state.OutDevTypeTemp, t => Assert.Equal(DS4Windows.Core.Model.OutContTypeInfo.DefaultType, t));
            Assert.All(state.ActiveOutDevType, t => Assert.Equal(DS4Windows.Core.Model.OutContType.None, t));

            var service = new DS4Windows.Output.Services.OutputSlotService(
                RecordingProxy.Create<DS4Windows.Processing.Abstractions.IVirtualPadService>(),
                RecordingProxy.Create<DS4Windows.Output.Abstractions.IOutputSlotStore>(), state);
            Assert.Same(state.OutDevTypeTemp, service.OutDevTypeTemp);
            Assert.Same(state.ActiveOutDevType, service.ActiveOutDevType);
        }

        [Fact]
        public void Registration_HandsTheArrayToBackingStore_WithoutResolvingTheServiceFromTheStoreRegistration()
        {
            // 配線は、コンテナが組み立てた結果で確かめる（IOutputDeviceTypeState の配列を BackingStore が同じ参照で持つ）
            var state = TestHost.GetService<DS4Windows.Processing.Abstractions.IOutputDeviceTypeState>();
            Assert.Same(state.ActiveOutDevType, TestHost.GetService<DS4Windows.Core.Settings.BackingStore>().ActiveOutDevType);

            // BackingStore の登録のラムダの中で IOutputSlotService を解決しない（解決すると循環し、スタックオーバーフローになるため、実行では確かめられない）
            string registration = RegistrationSource.ReadAll(stripComments: true);
            if (registration == null) return;
            int start = registration.IndexOf("services.AddSingleton<BackingStore>(sp =>");
            Assert.True(start >= 0);
            int end = registration.IndexOf("});", start);
            Assert.True(end > start);
            Assert.DoesNotContain("IOutputSlotService", registration.Substring(start, end - start));
        }
    }
}
