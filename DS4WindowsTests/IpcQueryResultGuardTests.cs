using System;
using System.IO;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 不具合の修正（2026-10-23）: コマンドラインのクライアントが、query の結果を、結果の MMF（<c>resultDataMmf</c>。<c>IpcMemoryMap</c>）から読むこと。
    /// 以前は、クラス名の MMF（<c>classNameMmf</c>。クライアントのプロセスでは null）から読んでいたため、例外が握りつぶされ、結果が常に空だった。
    /// </summary>
    public class IpcQueryResultGuardTests
    {
        [Fact]
        public void TheClient_ReadsTheQueryResultFromTheResultMmf()
        {
            string path = SourceFileLocator.Find("DS4Windows/Core/Platform/IpcMemoryMap.cs");
            if (path == null) return;
            string text = File.ReadAllText(path);

            int start = text.IndexOf("public string WaitAndReadResult(", StringComparison.Ordinal);
            Assert.True(start >= 0);
            int end = text.IndexOf("public void DisposeResultBuffer(", start, StringComparison.Ordinal);
            string body = text.Substring(start, end - start);

            Assert.Contains("resultDataMmf.CreateViewAccessor(", body);
            Assert.DoesNotContain("classNameMmf.CreateViewAccessor(", body);
        }
    }
}
