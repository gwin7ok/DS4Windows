#nullable enable

using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// キーの連打（繰り返し）の開始・停止の契約。
    /// </summary>
    public interface IRepeater : IDisposable
    {
        /// <summary>繰り返しを始める。間隔・最初の待ちは実装（<c>RepeatHelper</c>）が持つ。</summary>
        void Start();
        void Stop();
    }
}
