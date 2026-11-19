#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="AppSettingsService"/> の設定の変更通知の規則: 値が変わったときだけ、設定を書き換えたあとに、
    /// 呼び出したプロパティの名前で <see cref="AppSettingsService.SettingChanged"/> を 1 回出す。
    /// 真偽値・整数の設定を、すべてリフレクションで確かめる（通知の名前の書き間違いや、同じ値での通知を防ぐ）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class AppSettingsServiceChangeNotificationTests
    {
        private static IEnumerable<PropertyInfo> SimpleSettings() =>
            typeof(AppSettingsService).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && (p.PropertyType == typeof(bool) || p.PropertyType == typeof(int)));

        [Fact]
        public void ChangingASimpleSetting_RaisesOneNotificationNamedAfterTheProperty()
        {
            var service = new AppSettingsService(PersistenceHarness.NewStore());
            int checkedCount = 0;
            foreach (PropertyInfo property in SimpleSettings())
            {
                object original = property.GetValue(service)!;
                object changed = property.PropertyType == typeof(bool) ? !(bool)original : (object)((int)original + 1);

                var names = new List<string>();
                EventHandler<string> handler = (s, name) => names.Add(name);
                service.SettingChanged += handler;
                try
                {
                    try { property.SetValue(service, changed); }
                    catch (TargetInvocationException)
                    {
                        // 範囲の検査で拒否する設定は、通知しない（対象外）
                        continue;
                    }

                    if (!Equals(property.GetValue(service), changed)) continue;
                    Assert.True(names.Count == 1 && names[0] == property.Name,
                        $"{property.Name}: 通知 = [{string.Join(",", names)}]");

                    // 同じ値の設定では、通知しない
                    names.Clear();
                    property.SetValue(service, changed);
                    Assert.True(names.Count == 0, $"{property.Name}: 同じ値で通知した");
                    checkedCount++;
                }
                finally
                {
                    service.SettingChanged -= handler;
                    property.SetValue(service, original);
                }
            }

            Assert.True(checkedCount > 20, $"確かめた設定が少なすぎる: {checkedCount}");
        }
    }
}
