namespace DS4Windows.UI.Services
{
    /// <summary>
    /// タッチパッドのスワイプによるプロファイルの切替の規則（画面に依存しない）。
    /// 「left」で 1 つ前、「right」で 1 つ次へ進み、先頭・末尾は反対側へ回り込む。
    /// 通知（スライドの文字列に "t" が含まれるとき）の要否もここで決める。
    /// </summary>
    public sealed class TouchpadSwipeProfileRule
    {
        /// <summary>
        /// スワイプの結果、選ぶべきプロファイルの番号を返す。切替が要らない（スワイプではない・一覧が空）ときは現在の番号のまま返す。
        /// </summary>
        public int NextIndex(int selectedIndex, int profileCount, string slide)
        {
            if (profileCount <= 0) return selectedIndex;

            if (slide == "left")
            {
                return selectedIndex <= 0 ? profileCount - 1 : selectedIndex - 1;
            }

            if (slide == "right")
            {
                return selectedIndex == profileCount - 1 ? 0 : selectedIndex + 1;
            }

            return selectedIndex;
        }

        /// <summary>スライドの文字列が、プロファイルの通知（タップ）を求めているか。</summary>
        public bool WantsNotification(string slide) => slide != null && slide.Contains("t");
    }
}
