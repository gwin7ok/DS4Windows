using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W2: 合成入力の状態（globalState／deviceState／syncStateLock）を <c>SyntheticInputState</c> へ切り出したことと、
    /// <c>BackingStore</c> から <c>Mapping</c> への逆参照を <c>ActionsLoaded</c> イベントへ置き換えたことの回帰ガード。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2〜Q5）。
    /// </summary>
    public class Phase7Step7SyntheticInputStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step7SyntheticInputStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void Mapping_HoldsNoSyntheticStateOfItsOwn_OnlyForwarders()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            Assert.DoesNotContain("public class SyntheticState", mapping);
            Assert.DoesNotContain("new ReaderWriterLockSlim()", mapping);
            Assert.DoesNotContain("new SyntheticState()", mapping);
            Assert.Contains("private static readonly SyntheticInputState syntheticInput = SyntheticInputState.Shared;", mapping);
            Assert.Contains("syncStateLock => syntheticInput.SyncStateLock;", mapping);
            Assert.Contains("public static SyntheticState[] deviceState => syntheticInput.DeviceState;", mapping);
        }

        [Fact]
        public void BackingStore_DoesNotReferenceMapping_AndRaisesActionsLoadedInBothPaths()
        {
            string store = Read("DS4Windows/DS4Control/BackingStore.cs");
            if (store == null) return;

            Assert.False(Regex.IsMatch(store, @"\bMapping\."), "BackingStore が Mapping を参照している");
            Assert.Contains("public event Action ActionsLoaded;", store);
            Assert.Equal(2, Regex.Matches(store, @"RaiseActionsLoaded\(\);").Count);
        }

        [Fact]
        public void Registration_SubscribesTheResetOnce_AndRegistersTheSharedState()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            Assert.Contains("store.ActionsLoaded -= ActionsLoadedRuntimeReset.Reset;", registration);
            Assert.Contains("store.ActionsLoaded += ActionsLoadedRuntimeReset.Reset;", registration);
            Assert.Contains("services.AddSingleton<SyntheticInputState>(_ => SyntheticInputState.Shared);", registration);
        }

        [Fact]
        public void ResetHandler_KeepsTheOrderAndTheLogMessage()
        {
            string reset = Read("DS4Windows/DS4Control/Services/ActionsLoadedRuntimeReset.cs");
            if (reset == null) return;

            int state = reset.IndexOf("SyntheticInputState.Shared.ResetAll();");
            int toggled = reset.IndexOf("ActionManager.ClearAllToggledOn();");
            int macro = reset.IndexOf("MacroRuntimeState.Shared.ClearMacroDone();");
            int log = reset.IndexOf("Runtime synthetic state reset after Actions load");
            Assert.True(state >= 0 && state < toggled && toggled < macro && macro < log);
        }

        [Fact]
        public void SyntheticDispatcher_UsesTheHolder()
        {
            string dispatcher = Read("DS4Windows/DS4Control/SyntheticDispatcher.cs");
            if (dispatcher == null) return;

            Assert.DoesNotContain("Mapping.deviceState", dispatcher);
            Assert.DoesNotContain("Mapping.globalState", dispatcher);
            Assert.DoesNotContain("Mapping.SyntheticState", dispatcher);
            Assert.Contains("SyntheticInputState.Shared.DeviceState", dispatcher);
        }
    }
}
