namespace DS4Windows.Input.Hid
{
    /// <summary>HID の入力レポートの読み取りの結果（<see cref="DS4Windows.Input.Abstractions.IHidTransport.ReadFile"/> の戻り値）。</summary>
    public enum HidReadStatus
    {
        Success = 0,
        WaitTimedOut = 1,
        WaitFail = 2,
        NoDataRead = 3,
        ReadError = 4,
        NotConnected = 5
    }
}
