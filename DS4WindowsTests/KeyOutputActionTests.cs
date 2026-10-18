using System;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step3 W4（Phase8-Step3-Plan.md §2.4）: 書き直した <see cref="KeyOutputAction"/> が、系統 B（<c>KeyActionAdapter</c> 経由）と
    /// 同じ押下・解放の列になること、ネイティブキーの求め方（Q7＝A）が現行の経路と一致することを確認する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class KeyOutputActionTests
    {
        private const int Device = 3;

        private sealed class Context : IOutputContext
        {
            public Context(IVirtualKBM handler) { OutputHandler = handler; }
            public int Device => KeyOutputActionTests.Device;
            public IVirtualKBM OutputHandler { get; }
        }

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        /// <summary>テストのホストは出力 KBM マッピングが未設定（null）のため、一時的に SendInputMapping を設定する（終了時に戻す）。</summary>
        private sealed class MappingScope : IDisposable
        {
            private readonly IProfileSettingsService _settings;
            private readonly VirtualKBMMapping _saved;

            public MappingScope(IProfileSettingsService settings)
            {
                _settings = settings;
                _saved = settings.OutputKBMMapping;
                settings.OutputKBMMapping = new SendInputMapping();
            }

            public void Dispose() => _settings.OutputKBMMapping = _saved;
        }

        private static SpecialAction NewKeyAction(string name, DS4KeyType keyType)
        {
            var sa = new SpecialAction(name, "Cross", "Key", "30", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.details = "30";
            sa.keyType = keyType;
            return sa;
        }

        [Theory]
        [InlineData(DS4KeyType.None)]
        [InlineData(DS4KeyType.Toggle)]
        public void ExecuteAndStop_ProduceTheSameKeySequence_AsTheLegacyAdapterPath(DS4KeyType keyType)
        {
            IProfileSettingsService settings = Settings();

            // 新: KeyOutputAction
            var newKbm = new MockVirtualKBM();
            var newSa = NewKeyAction("p8s3_key_new_" + keyType, keyType);
            var output = new KeyOutputAction(newSa, settings);
            var newCtx = new Context(newKbm);
            output.Execute(newCtx);
            output.Stop(newCtx);
            output.Execute(newCtx);
            output.Stop(newCtx);

            // 旧: KeyActionAdapter（系統 B）
            var oldKbm = new MockVirtualKBM();
            var oldSa = NewKeyAction("p8s3_key_old_" + keyType, keyType);
            var adapter = new KeyActionAdapter(oldSa, 0);
            var oldCtx = new MappingContext { LogicalValue = 30, NativeValue = 0, OutputHandler = oldKbm, ActionDef = oldSa };
            adapter.OnTrigger(Device, oldCtx);
            adapter.OnRelease(Device, oldCtx);
            adapter.OnTrigger(Device, oldCtx);
            adapter.OnRelease(Device, oldCtx);

            Assert.Equal(oldKbm.KeyPressCalls.ToArray(), newKbm.KeyPressCalls.ToArray());
            Assert.Equal(oldKbm.KeyReleaseCalls.ToArray(), newKbm.KeyReleaseCalls.ToArray());
            Assert.NotEmpty(newKbm.KeyPressCalls); // 比較が空同士にならないこと
        }

        [Fact]
        public void NativeKey_MatchesTheDispatcherAndTheMappingRoute()
        {
            IProfileSettingsService settings = Settings();
            using var scope = new MappingScope(settings);
            var dispatcher = TestHost.GetService<ISyntheticDispatcher>();

            foreach (ushort key in new ushort[] { 30, 0x41, 0x1B, 0x70, 0xA0 })
                Assert.Equal(settings.OutputKBMMapping.GetRealEventKey(key), dispatcher.ResolveNativeKey(key));
        }

        [Fact]
        public void Press_UsesTheMappedNativeKey()
        {
            IProfileSettingsService settings = Settings();
            using var scope = new MappingScope(settings);
            var kbm = new MockVirtualKBM();
            var sa = NewKeyAction("p8s3_key_native", DS4KeyType.None);
            var output = new KeyOutputAction(sa, settings);

            output.Execute(new Context(kbm));

            Assert.Equal(new[] { settings.OutputKBMMapping.GetRealEventKey(30) }, kbm.KeyPressCalls.ToArray());
            output.Stop(new Context(kbm));
        }

        [Fact]
        public void Id_Constructor_AndNullContext()
        {
            IProfileSettingsService settings = Settings();
            var sa = NewKeyAction("p8s3_key_id", DS4KeyType.None);
            var output = new KeyOutputAction(sa, settings);

            Assert.Equal("p8s3_key_id", output.Id);
            output.Execute(null); // 例外にならない
            output.Stop(null);

            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(null, settings));
            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(sa, null));
        }

        [Fact]
        public void KeyActionBinding_CarriesTheGivenOutput()
        {
            IProfileSettingsService settings = Settings();
            var sa = NewKeyAction("p8s3_key_binding", DS4KeyType.None);
            var output = new KeyOutputAction(sa, settings);

            var binding = new KeyActionBinding(sa, output);

            Assert.Single(binding.Outputs);
            Assert.Same(output, binding.Outputs[0]);
            Assert.Empty(new KeyActionBinding(sa).Outputs);
        }
    }
}
