using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step6: 出力デバイス種別の配列（OutDevTypeTemp／ActiveOutDevType）の所有権を、Global から OutputSlotService へ移したことの回帰ガード。
    /// - Global に配列がない。BackingStore は Global から読まず、自身の ActiveOutDevType を読む。
    /// - 配列の実体は OutputSlotService の readonly フィールド（参照は再代入されない。ControlService のキャッシュの前提）。
    /// - BackingStore への配列の受け渡しは IOutputSlotService の登録内で行い、BackingStore の登録内で IOutputSlotService を解決しない
    ///   （IOutputSlotStore → IPathService → BackingStore と循環し、スタックオーバーフローになる）。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は Phase7-Step6-Plan.md。
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
            string store = SourceText.ReadCode("DS4Windows/Core/Settings/BackingStore.cs");
            if (store == null) return;

            Assert.Matches(@"public\s+OutContType\[\]\s+ActiveOutDevType\s*\{\s*get;\s*set;\s*\}", store);
            // Phase7-Step10c W4: プロファイルの読込（1 件）は LegacyProfileXmlSerializer へ移した（store.ActiveOutDevType）。合計は変わらない
            // Phase7-Step10c W6: プリセットの読込（7 件）も ProfilePresets へ移した。3 つのソースを合わせて 8 件
            string serializer = SourceText.ReadCode("DS4Windows/Core/Persistence/LegacyProfileXmlSerializer.cs");
            string presets = SourceText.ReadCode("DS4Windows/Core/Persistence/ProfilePresets.cs");
            if (serializer == null || presets == null) return;
            Assert.Equal(8, Regex.Matches(store + serializer + presets, @"OutContType oldContType = (store\.)?ActiveOutDevType\[device\];").Count);
        }

        [Fact]
        public void TheArrays_AreOwnedByTheDeviceTypeState_AndTheServiceForwardsThem()
        {
            // Phase13-Step7 7-b: 配列の持ち主は OutputSlotService から IOutputDeviceTypeState（OutputDeviceTypeState）へ移った
            string state = SourceText.ReadCode("DS4Windows/Output/Services/OutputDeviceTypeState.cs");
            string service = SourceText.ReadCode("DS4Windows/Output/Services/OutputSlotService.cs");
            if (state == null || service == null) return;

            Assert.Matches(@"OutContType\[\]\s+OutDevTypeTemp\s*\{\s*get;\s*\}\s*=\s*CreateFilled\(OutContType\.X360\)", state);
            Assert.Matches(@"OutContType\[\]\s+ActiveOutDevType\s*\{\s*get;\s*\}\s*=\s*CreateFilled\(OutContType\.None\)", state);
            Assert.DoesNotContain("CreateFilled", service);
            Assert.Contains("OutContType[] OutDevTypeTemp => _types.OutDevTypeTemp;", service);
            Assert.Contains("OutContType[] ActiveOutDevType => _types.ActiveOutDevType;", service);
        }

        [Fact]
        public void Registration_HandsTheArrayToBackingStore_WithoutResolvingTheServiceFromTheStoreRegistration()
        {
            // Phase14d-Step3: 配線は、コンテナが組み立てた結果で確かめる（IOutputDeviceTypeState の配列を BackingStore が同じ参照で持つ）
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
