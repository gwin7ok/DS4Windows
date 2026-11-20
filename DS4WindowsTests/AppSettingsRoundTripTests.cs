#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// アプリ設定（<c>Profiles.xml</c>）の保存→読み込みの往復で、設定ファイルへ保存される全項目が、元の値で戻ること。
    /// 項目は、<see cref="BackingStore"/> の公開の値（フィールド・プロパティ）と、<see cref="BackingStore"/> が持つ設定の領域
    /// （<c>LayoutSettings</c> など。<c>DS4Windows.Core.Settings</c> の型で、名前が <c>Settings</c>／<c>State</c> で終わるもの）の値を、
    /// 反射で集める。項目をどの型に置いても、同じ検査が通るようにするため（置き場所を変えても、保存の形が変わらないことの固定）。
    /// 読み込み時に値を作り直す項目（版の数値、偽の exe 名）と、設定ファイルに保存しない実行時の状態は、除外の表で明示する。
    /// 並列実行は xunit.runner.json で無効（静的状態の書き換えが他のテストと競合しない）。
    /// </summary>
    public class AppSettingsRoundTripTests : IDisposable
    {
        /// <summary>読み込みで作り直す、または保存しない項目（理由つき）。</summary>
        private static readonly Dictionary<string, string> Excluded = new Dictionary<string, string>
        {
            ["lastVersionCheckedNum"] = "読み込みのとき、lastVersionChecked から計算し直す",
            ["fakeExeFileName"] = "別のファイル（DS4Windows.exe の隣）から読み込む",
            ["ds4Mapping"] = "設定ファイルに保存しない（書き出し・読み込みのコードが残っていない）",
        };

        private readonly string _tempDir;
        private readonly string _originalExeDirPath;
        private readonly string _profilesXml;

        public AppSettingsRoundTripTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ds4w_roundtrip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _originalExeDirPath = TestGlobals.ExeDirPath;
            TestGlobals.ExeDirPath = _tempDir;
            _profilesXml = Path.Combine(_tempDir, "Profiles.xml");
        }

        public void Dispose()
        {
            TestGlobals.ExeDirPath = _originalExeDirPath;
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        /// <summary>設定の領域の型か（名前が Settings／State で終わる、同じ名前空間の型）。</summary>
        private static bool IsArea(Type t) =>
            t.Namespace == "DS4Windows.Core.Settings" && t.IsClass && (t.Name.EndsWith("Settings", StringComparison.Ordinal) || t.Name.EndsWith("State", StringComparison.Ordinal))
            && t != typeof(BackingStore);

        private static bool IsSimple(Type t) =>
            t == typeof(int) || t == typeof(bool) || t == typeof(string) || t == typeof(double) || t == typeof(DateTime) || t == typeof(ulong) || t.IsEnum;

        /// <summary>公開の単純な値の「入れ物（対象のオブジェクト、メンバー）」を集める。</summary>
        private static List<(object Owner, string Name, Type Type, Func<object, object?> Get, Action<object, object?> Set)> Collect(BackingStore store)
        {
            var list = new List<(object, string, Type, Func<object, object?>, Action<object, object?>)>();
            void Walk(object owner, bool top)
            {
                foreach (FieldInfo f in owner.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.IsInitOnly || !IsSimple(f.FieldType)) continue;
                    list.Add((owner, f.Name, f.FieldType, o => f.GetValue(o), (o, v) => f.SetValue(o, v)));
                }

                foreach (PropertyInfo p in owner.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (IsSimple(p.PropertyType) && p.CanRead && p.CanWrite && p.GetSetMethod() != null)
                        list.Add((owner, p.Name, p.PropertyType, o => p.GetValue(o), (o, v) => p.SetValue(o, v)));
                    else if (top && IsArea(p.PropertyType) && p.GetValue(owner) is object area)
                        Walk(area, false);
                }
            }

            Walk(store, true);
            return list;
        }

        private static object? NonDefault(Type type, int index)
        {
            if (type == typeof(bool)) return null;   // 呼び出し側で反転する
            if (type == typeof(int)) return 2000 + index;   // 範囲の丸め（ポートは 1024 以上など）に掛からない値
            if (type == typeof(ulong)) return (ulong)(300 + index);
            if (type == typeof(double)) return 0.5 + index / 1000.0;
            if (type == typeof(string)) return "1.2." + index;   // lastVersionChecked は、版の形式のみ読み込まれる
            if (type == typeof(DateTime)) return new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local).AddMinutes(index);
            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                return values.GetValue(values.Length > 1 ? 1 : 0);
            }

            return null;
        }

        [Fact]
        public void EveryPersistedSettingSurvivesSaveAndLoad()
        {
            var writer = new BackingStore(new EnvironmentService()) { m_Profile = _profilesXml };
            var expected = new Dictionary<string, object?>();
            int index = 0;
            foreach (var m in Collect(writer))
            {
                index++;
                if (Excluded.ContainsKey(m.Name) || m.Name.StartsWith("m_", StringComparison.Ordinal)) continue;
                object? value = m.Type == typeof(bool) ? !(bool)m.Get(m.Owner)! : NonDefault(m.Type, index);
                m.Set(m.Owner, value);
                expected[m.Name] = m.Get(m.Owner);
            }

            Assert.True(expected.Count >= 60, "集めた項目が少なすぎる: " + expected.Count);
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(writer).Save(), "保存に失敗した");

            var reader = new BackingStore(new EnvironmentService()) { m_Profile = _profilesXml };
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(reader).Load());

            var actual = Collect(reader).Where(m => expected.ContainsKey(m.Name)).ToDictionary(m => m.Name, m => m.Get(m.Owner));
            var differences = expected.Where(kv => !Equals(kv.Value, actual[kv.Key])).Select(kv => $"{kv.Key}: {kv.Value} → {actual[kv.Key]}").ToList();
            Assert.True(differences.Count == 0, "往復で値が変わった項目: " + string.Join(", ", differences));
        }
    }
}
