#nullable enable

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// トリガーの設定の文字列（「4,5」のような、コンマ区切りのボタンの番号）を、数の配列へ直したもの。
    /// 判定は毎フレーム行われるため、同じ文字列のあいだは直した結果を使い回し、Split と TryParse の割り当てを避ける。
    /// 数でない要素は、判定では「押されていない」として扱う（Valid が偽）。
    /// </summary>
    internal sealed class ParsedTriggerList
        {
            public readonly string Source;
            public readonly bool IsEmpty;
            public readonly int[] Values;
            public readonly bool[] Valid;

            public ParsedTriggerList(string source)
            {
                Source = source;
                string[] parts = (source ?? string.Empty).Split(',');
                IsEmpty = string.IsNullOrEmpty(parts[0]);
                Values = new int[parts.Length];
                Valid = new bool[parts.Length];
                for (int k = 0; k < parts.Length; k++)
                {
                    Valid[k] = int.TryParse(parts[k], out Values[k]);
                }
            }
        }
}
