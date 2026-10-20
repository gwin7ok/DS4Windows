using System;
using System.Reflection;
using System.Text;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step8 W1（Phase8-Step8-Plan.md §3 の方針 1。Q4＝A）: 旧経路の判定・アナログ値の関数（<c>Mapping</c> の private メソッド）の戻り値を、
    /// 決定的な入力（種つきの乱数）で、全コントロールについて表にする。旧関数を削除（8a・8b）したあとも、新経路の関数（<c>TriggerInput</c>）を、
    /// 旧関数が返した値（ゴールデン）と比べられるようにするため、削除の前に値を固定する。
    /// 1 行 ＝ 1 回の入力。コントロールごとに 1 つの項目（9 文字）をコンマで区切る:
    /// 判定（特殊アクション用）1、判定（Controls 用）1、<c>GetBoolMapping</c> 1、バイト値 2（16 進）、軸の値 alt＝false 2、軸の値 alt＝true 2。
    /// 真偽は 1／0、例外は x。新経路の表（<see cref="BuildNew"/>）は、<c>GetBoolMapping</c> の位置が '-'（新経路に同じ関数がない。Q3＝A で SA の判定に揃えた）。
    /// </summary>
    internal static class LegacyFunctionTable
    {
        public const int Iterations = 60;
        private const int Device = MappingCharacterizationScope.Device;
        private const int Seed = 80801;

        public static string FileName(GyroOutMode mode) => $"step8-legacy-functions-{mode}.txt".ToLowerInvariant();

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        private static string Header()
        {
            var sb = new StringBuilder();
            sb.Append("# Phase8-Step8 W1: 旧関数の戻り値の表。1 行 ＝ 1 回の入力（種 ").Append(Seed).Append("）。項目 ＝ コントロールごと（下の順）。\n");
            sb.Append("# 項目（9 文字）: 判定(SA)1 判定(Controls)1 GetBoolMapping 1 バイト値 2 軸(alt=false) 2 軸(alt=true) 2。真偽は 1／0、例外は x、'-' は新経路にない\n");
            sb.Append("# controls:");
            for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++) sb.Append(' ').Append((DS4Controls)c);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void MakeInput(Random random, out DS4State state, out DS4StateFieldMapping fields)
        {
            state = new DS4State
            {
                LX = (byte)random.Next(256), LY = (byte)random.Next(256),
                RX = (byte)random.Next(256), RY = (byte)random.Next(256),
            };
            state.calculateStickAngles();
            fields = new DS4StateFieldMapping();
            for (int i = 0; i < fields.buttons.Length; i++)
            {
                fields.buttons[i] = random.Next(2) == 0;
                fields.axisdirs[i] = (byte)random.Next(256);
                fields.triggers[i] = (byte)random.Next(256);
                fields.gryodirs[i] = random.Next(-150, 151);
                fields.swipedirs[i] = (byte)random.Next(256);
                fields.swipedirbools[i] = random.Next(2) == 0;
            }
        }

        private static string B(bool value) => value ? "1" : "0";

        private static string Hex(byte value) => value.ToString("x2");

        /// <summary>旧関数（<c>Mapping</c> の private メソッド）の表。削除の前に、これを生成してゴールデンに固定する。</summary>
        public static string BuildLegacy(GyroOutMode mode)
        {
            IProfileSettingsService settings = Settings();
            Mapping mapping = TestMappingFactory.Create(settings);
            MethodInfo special = typeof(Mapping).GetMethod("getBoolSpecialActionMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo action = typeof(Mapping).GetMethod("GetBoolActionMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo boolMapping = typeof(Mapping).GetMethod("GetBoolMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo byteMapping = typeof(Mapping).GetMethod("GetByteMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo axisMapping = typeof(Mapping).GetMethod("GetXYAxisMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            if (special == null || action == null || boolMapping == null || byteMapping == null || axisMapping == null)
                throw new InvalidOperationException("旧関数が見つからない（削除後は、ゴールデンだけを使う）");

            var sb = new StringBuilder(Header());
            var random = new Random(Seed);
            GyroOutMode saved = settings.GyroOutputMode[Device];
            settings.GyroOutputMode[Device] = mode;
            try
            {
                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    MakeInput(random, out DS4State state, out DS4StateFieldMapping fields);
                    sb.Append(iteration.ToString("D3")).Append(' ');
                    for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                    {
                        var control = (DS4Controls)c;
                        if (c > 0) sb.Append(',');
                        sb.Append(Call(() => B((bool)special.Invoke(mapping, new object[] { Device, control, state, null, null, fields }))));
                        sb.Append(Call(() => B((bool)action.Invoke(mapping, new object[] { Device, control, state, null, null, fields, false }))));
                        sb.Append(Call(() => B((bool)boolMapping.Invoke(mapping, new object[] { Device, control, state, null, null, fields }))));
                        sb.Append(Call(() => Hex((byte)byteMapping.Invoke(mapping, new object[] { Device, control, state, null, null, fields })), "xx"));
                        sb.Append(Call(() => Hex((byte)axisMapping.Invoke(mapping, new object[] { Device, control, state, null, null, fields, false })), "xx"));
                        sb.Append(Call(() => Hex((byte)axisMapping.Invoke(mapping, new object[] { Device, control, state, null, null, fields, true })), "xx"));
                    }

                    sb.Append('\n');
                }
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }

            return sb.ToString();
        }

        /// <summary>新経路（<see cref="TriggerInput"/>）の表。<c>GetBoolMapping</c> の位置は '-'。</summary>
        public static string BuildNew(GyroOutMode mode)
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);

            var sb = new StringBuilder(Header());
            var random = new Random(Seed);
            GyroOutMode saved = settings.GyroOutputMode[Device];
            settings.GyroOutputMode[Device] = mode;
            try
            {
                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    MakeInput(random, out DS4State state, out DS4StateFieldMapping fields);
                    input.Update(state, null, null, null, fields);
                    sb.Append(iteration.ToString("D3")).Append(' ');
                    for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                    {
                        var control = (DS4Controls)c;
                        if (c > 0) sb.Append(',');
                        sb.Append(B(input.IsActive(control)));
                        sb.Append(B(input.IsControlActive(control)));
                        sb.Append('-');
                        sb.Append(Hex(input.GetByteValue(control)));
                        sb.Append(Hex(input.GetAxisValue(control, false)));
                        sb.Append(Hex(input.GetAxisValue(control, true)));
                    }

                    sb.Append('\n');
                }
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }

            return sb.ToString();
        }

        /// <summary>ゴールデンの表の、<c>GetBoolMapping</c> の位置（各項目の 3 文字目）を '-' にする（新経路の表と比べるため）。</summary>
        public static string MaskBoolMapping(string table)
        {
            string[] lines = table.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#", StringComparison.Ordinal) || lines[i].Length == 0) continue;

                int space = lines[i].IndexOf(' ');
                string[] tokens = lines[i].Substring(space + 1).Split(',');
                for (int t = 0; t < tokens.Length; t++)
                    tokens[t] = tokens[t].Substring(0, 2) + "-" + tokens[t].Substring(3);
                lines[i] = lines[i].Substring(0, space + 1) + string.Join(",", tokens);
            }

            return string.Join("\n", lines);
        }

        private static string Call(Func<string> f, string failure = "x")
        {
            try
            {
                return f();
            }
            catch (TargetInvocationException)
            {
                return failure;
            }
        }
    }
}
