using System;
using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-2 のテスト用スタブ。HidHide ドライバーの制御デバイスの代わりに、設定値を持ち、ホワイトリストの書き込みを記録する。
    /// <see cref="Open"/> は、毎回同じ instance（共有の状態）を返し、閉じた回数を数える。
    /// </summary>
    public sealed class FakeHidHideApi : IHidHideApi, IHidHideApiFactory
    {
        public bool Opened = true;
        public bool Active;
        public bool Inverse;
        public List<string> Blacklist = new List<string>();
        public List<string> Whitelist = new List<string>();
        public readonly List<List<string>> SetCalls = new List<List<string>>();
        public int OpenCount;
        public int DisposeCount;

        public IHidHideApi Open()
        {
            OpenCount++;
            return this;
        }

        public bool IsOpen() => Opened;
        public bool GetActiveState() => Active;
        public List<string> GetBlacklist() => new List<string>(Blacklist);
        public bool GetWhiteListInverseState() => Inverse;
        public List<string> GetWhitelist() => new List<string>(Whitelist);

        public bool SetWhitelist(List<string> instances)
        {
            SetCalls.Add(new List<string>(instances));
            Whitelist = new List<string>(instances);
            return true;
        }

        public readonly List<List<string>> SetBlacklistCalls = new List<List<string>>();
        /// <summary>false にすると、ブラックリストの書き込みが失敗（false）を返す。</summary>
        public bool AllowBlacklistWrite = true;

        public bool SetBlacklist(List<string> instances)
        {
            SetBlacklistCalls.Add(new List<string>(instances));
            if (!AllowBlacklistWrite) return false;
            Blacklist = new List<string>(instances);
            return true;
        }

        public void Dispose() { DisposeCount++; }
    }
}
