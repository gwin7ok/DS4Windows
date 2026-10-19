using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.1-W3（Phase8-Step7-Plan.md §5.3・Q2＝A）: 本番の既定のスイッチ（<c>ServiceRegistration</c> の一覧）に Button が入っていること、
    /// その 1 行を外すだけで旧経路に戻る（束縛が作られない）こと、を確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step7W3ButtonSwitchTests
    {
        [Fact]
        public void TheHostDefault_MigratesAllTheTypesOfStep7ExceptMultiAction()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();

            // Phase8-Step7: PR ごとに 1 種別ずつ足す（7.1 Button、7.2 Key、7.3 Macro、7.4 Program、7.5a Profile、7.6a GyroCalibrate、7.6b DisconnectBT、7.6c BatteryCheck、7.6d ステアリング較正）
            Assert.Equal(new[] { SpecialAction.ActionTypeId.BatteryCheck, SpecialAction.ActionTypeId.Button, SpecialAction.ActionTypeId.DisconnectBT, SpecialAction.ActionTypeId.GyroCalibrate, SpecialAction.ActionTypeId.Key, SpecialAction.ActionTypeId.Macro, SpecialAction.ActionTypeId.Profile, SpecialAction.ActionTypeId.Program, SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate }, options.MigratedTypes.OrderBy(t => t.ToString(), StringComparer.Ordinal).ToArray());
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Button));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Key));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Macro));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Program));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Profile));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.GyroCalibrate));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.DisconnectBT));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.BatteryCheck));
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate));
            Assert.False(options.IsMigrated(SpecialAction.ActionTypeId.MultiAction));
        }

        [Fact]
        public void TheRegistrationSource_AddsEachMigratedTypeOnOneLine()
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            if (path == null) return;

            string[] lines = File.ReadAllLines(path)
                .Where(l => l.Contains("migration.MigratedTypes.Add(")).ToArray();

            // 種別ごとの PR で 1 行ずつ足す。ロールバックは、その行を外すだけ（Q2＝A）
            Assert.Equal(9, lines.Length);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.Button\);"), lines[0]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.Key\);"), lines[1]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.Macro\);"), lines[2]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.Program\);"), lines[3]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.GyroCalibrate\);"), lines[4]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.DisconnectBT\);"), lines[5]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.BatteryCheck\);"), lines[6]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.SASteeringWheelEmulationCalibrate\);"), lines[7]);
            Assert.Matches(new Regex(@"MigratedTypes\.Add\(SpecialAction\.ActionTypeId\.Profile\);"), lines[8]);
        }

        [Fact]
        public void RemovingTheLine_BringsBackTheLegacyPath_NoBindingIsBuilt()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IActionBindingBuilder>();
            var saved = options.MigratedTypes.ToArray();
            try
            {
                using var t = new SpecialActionTestScope(true);
                t.AddButtonPress("L1");
                Assert.Single(builder.Build(t.Device));

                options.MigratedTypes.Remove(SpecialAction.ActionTypeId.Button);   // 既定の一覧から外した状態

                Assert.Empty(builder.Build(t.Device));
                t.PublishBindings();
                Assert.Empty(TestHost.GetService<IActionRegistry>().GetBindings(t.Device));

                // 旧経路が処理する（Stage3 が Cross を出力する）
                DS4State input = SpecialActionTestScope.Neutral();
                input.L1 = true;
                Assert.True(t.RunFrame(input).Cross);
            }
            finally
            {
                options.MigratedTypes.Clear();
                foreach (var type in saved) options.MigratedTypes.Add(type);
            }
        }
    }
}
