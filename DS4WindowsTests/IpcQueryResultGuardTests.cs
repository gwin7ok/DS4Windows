using System;
using System.Threading;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 不具合の修正（2026-10-23）: コマンドラインのクライアントが、query の結果を、結果の MMF（<c>IpcMemoryMap</c> の結果の領域）から読むこと。
    /// 以前は、クラス名の MMF（クライアントのプロセスでは null）から読んでいたため、例外が握りつぶされ、結果が常に空だった。
    /// 実際の名前付き MMF とイベントで、クライアント（結果の領域を作って待つ側）とサーバー（結果を書く側）を同じプロセスの 2 つの窓口で動かして確かめる。
    /// 名前はプロセス間で共有されるため、実アプリを同時に動かしているとこのテストは失敗する。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class IpcQueryResultGuardTests
    {
        private const string ReadyEventName = "DS4Windows_IPCResultData_ReadyEvent";

        [Fact]
        public void TheClient_ReadsTheQueryResultFromTheResultMmf()
        {
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, ReadyEventName);
            using var client = new IpcMemoryMap();
            using var server = new IpcMemoryMap();

            client.CreateResultBuffer();
            server.WriteResult("プロファイル A");

            Assert.Equal("プロファイル A", client.WaitAndReadResult(ready));
            client.DisposeResultBuffer();
        }
    }
}
