namespace DS4Windows
{
    /// <summary>
    /// スティックの 1 点と、その時刻（アンチスナップバックの履歴の 1 件）。
    /// 旧: <c>Mapping.DS4TimedStickAxisValue</c>（入れ子の構造体。Phase9 9-A(2)-B で、1 ファイル 1 型の原則に従い、独立させた）。
    /// </summary>
    public struct DS4TimedStickAxisValue
    {
        public long timestamp;
        public double x;
        public double y;
        public DS4TimedStickAxisValue(byte x, byte y, long timestamp)
        {
            this.timestamp = timestamp;
            this.x = x;
            this.y = y;
        }
    }
}
