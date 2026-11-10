namespace DS4Windows.Core.Model
{
    /// <summary>
    /// 出力の種類（<see cref="OutContType"/>）と、保存するキーおよび画面の選択肢の番号との対応の表。
    /// 画面（コンボボックス）と保存ファイルが、種類ごとに if／switch を書かずに、同じ表を引く。値は純粋な定数で、状態を持たない。
    /// 選択肢の番号は 2 通り: プロファイルの出力の種類（0: Xbox 360、1: DS4）と、スロットの割り当て（0: なし、1: Xbox 360、2: DS4）。
    /// </summary>
    public static class OutContTypeInfo
    {
        // プロファイルで選べる種類の並び（選択肢の番号の順）。種類を足すときはここへ足す
        private static readonly OutContType[] ProfileChoices = { OutContType.X360, OutContType.DS4 };
        private static readonly string[] ProfileKeys = { "X360", "DS4" };

        /// <summary>プロファイルに保存するキー。None は X360 として保存する（旧と同じ）。</summary>
        public static string SaveKey(OutContType type)
        {
            int i = System.Array.IndexOf(ProfileChoices, type);
            return ProfileKeys[i < 0 ? 0 : i];
        }

        /// <summary>保存されたキーから種類を求める。"None" と未知のキーは X360（旧と同じ）。</summary>
        public static OutContType FromSaveKey(string key)
        {
            int i = System.Array.IndexOf(ProfileKeys, key);
            return i < 0 ? OutContType.X360 : ProfileChoices[i];
        }

        /// <summary>プロファイルの出力の種類の選択肢の番号（X360 = 0、DS4 = 1。それ以外は 0）。</summary>
        public static int ProfileChoiceIndex(OutContType type)
        {
            int i = System.Array.IndexOf(ProfileChoices, type);
            return i < 0 ? 0 : i;
        }

        /// <summary>選択肢の番号から種類を求める。範囲外は X360（旧の「1 なら DS4、それ以外は X360」と同じ）。</summary>
        public static OutContType FromProfileChoiceIndex(int index)
        {
            return TryFromProfileChoiceIndex(index, out OutContType type) ? type : OutContType.X360;
        }

        /// <summary>選択肢の番号が有効なときだけ種類を返す（無効なら false）。</summary>
        public static bool TryFromProfileChoiceIndex(int index, out OutContType type)
        {
            if (index >= 0 && index < ProfileChoices.Length)
            {
                type = ProfileChoices[index];
                return true;
            }

            type = OutContType.None;
            return false;
        }

        /// <summary>スロットの割り当ての選択肢の番号（None = 0、X360 = 1、DS4 = 2）。</summary>
        public static int SlotChoiceIndex(OutContType type)
        {
            int i = System.Array.IndexOf(ProfileChoices, type);
            return i < 0 ? 0 : i + 1;
        }

        /// <summary>スロットの割り当ての選択肢の番号から種類を求める。範囲外は None。</summary>
        public static OutContType FromSlotChoiceIndex(int index)
        {
            return index >= 1 && index <= ProfileChoices.Length ? ProfileChoices[index - 1] : OutContType.None;
        }
    }
}
