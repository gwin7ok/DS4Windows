using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// コマンドラインのクライアントと、動いている本体のプロセスとの間の通信に使うメモリマップトファイル（MMF）の窓口。
    /// 本体は、メイン画面のウィンドウクラス名を MMF に置き（クライアントが <c>WM_COPYDATA</c> の宛先を探す）、問い合わせの結果を結果の MMF へ書く。
    /// クライアントは、クラス名を読み、結果の MMF を作って、本体が書き終えるのを待って読む。
    /// MMF は、このインスタンスがハンドルを持っている間だけ存在する。ここでの失敗は致命的ではないため、例外は握る。
    /// </summary>
    [System.Security.SuppressUnmanagedCodeSecurity]
    public sealed class IpcMemoryMap : IDisposable
    {
        private const string ClassNameMapName = "DS4Windows_IPCClassName.dat";
        private const string ResultDataMapName = "DS4Windows_IPCResultData.dat";
        private const string ResultReadyEventName = "DS4Windows_IPCResultData_ReadyEvent";
        private const int ClassNameBufferSize = 128;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private MemoryMappedFile classNameMmf;
        private MemoryMappedFile resultDataMmf;

        // ---- 本体側 ----

        /// <summary>メイン画面のウィンドウクラス名を MMF に書く（すでに持っているときは何もしない）。</summary>
        public void PublishWindowClassName(IntPtr hWnd)
        {
            if (classNameMmf != null) return; // Already holding a handle to MMF file. No need to re-write the data

            try
            {
                StringBuilder wndClassNameStr = new StringBuilder(ClassNameBufferSize);
                if (GetClassName(hWnd, wndClassNameStr, wndClassNameStr.Capacity) != 0 && wndClassNameStr.Length > 0)
                {
                    byte[] buffer = ASCIIEncoding.ASCII.GetBytes(wndClassNameStr.ToString());

                    classNameMmf = MemoryMappedFile.CreateNew(ClassNameMapName, ClassNameBufferSize);
                    MemoryMappedViewAccessor ipcClassNameMMA_Now = classNameMmf.CreateViewAccessor(0, buffer.Length);
                    ipcClassNameMMA_Now.WriteArray(0, buffer, 0, buffer.Length);
                    ipcClassNameMMA_Now?.Dispose();
                    // The MMF file is alive as long this process holds the file handle open
                }
            }
            catch (Exception)
            {
                /* Eat all exceptions because errors here are not fatal for DS4Win */
            }
        }

        /// <summary>
        /// 問い合わせ（"-command QueryProfile.device#"）の結果を書き、クライアントへ通知する。
        /// クライアントが結果の MMF を作って待っているときだけ意味がある。
        /// </summary>
        public void WriteResult(string dataStr)
        {
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mma = null;
            EventWaitHandle ipcNotifyEvent = null;

            try
            {
                ipcNotifyEvent = EventWaitHandle.OpenExisting(ResultReadyEventName);

                // UTF-8（非 ASCII の文字が「?」に化けないため）。終端の 0 を残すため最大 255 バイト（文字の境目で切り詰める）
                byte[] buffer = IpcResultText.Encode(dataStr);
                mmf = MemoryMappedFile.OpenExisting(ResultDataMapName);
                mma = mmf.CreateViewAccessor(0, IpcResultText.BufferSize);
                mma.WriteArray(0, buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                // Eat all exceptions
            }
            finally
            {
                if (mma != null) mma.Dispose();
                if (mmf != null) mmf.Dispose();

                if (ipcNotifyEvent != null) ipcNotifyEvent.Set();
            }
        }

        // ---- クライアント側 ----

        /// <summary>動いている本体のメイン画面のウィンドウクラス名を読む。見つからなければ null。</summary>
        public string ReadWindowClassName()
        {
            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mma = null;

            try
            {
                byte[] buffer = new byte[ClassNameBufferSize];
                mmf = MemoryMappedFile.OpenExisting(ClassNameMapName);
                mma = mmf.CreateViewAccessor(0, ClassNameBufferSize);
                mma.ReadArray(0, buffer, 0, buffer.Length);
                return ASCIIEncoding.ASCII.GetString(buffer);
            }
            catch (Exception)
            {
                // Eat all exceptions
            }
            finally
            {
                if (mma != null) mma.Dispose();
                if (mmf != null) mmf.Dispose();
            }

            return null;
        }

        /// <summary>結果を受け取る MMF を作る（すでに持っているときは何もしない）。</summary>
        public void CreateResultBuffer()
        {
            if (resultDataMmf != null) return; // Already holding a handle to MMF file. No need to re-write the data

            try
            {
                resultDataMmf = MemoryMappedFile.CreateNew(ResultDataMapName, IpcResultText.BufferSize);
                // The MMF file is alive as long this process holds the file handle open
            }
            catch (Exception)
            {
                /* Eat all exceptions because errors here are not fatal for DS4Win */
            }
        }

        /// <summary>結果の通知を待ち（最大 10 秒）、結果の MMF から読む。得られなければ空の文字列。</summary>
        public string WaitAndReadResult(EventWaitHandle ipcNotifyEvent)
        {
            if (resultDataMmf != null)
            {
                // Wait until the inter-process-communication (IPC) result data is available and read the result
                try
                {
                    // Wait max 10 secs and if the result is still not available then timeout and return "empty" result
                    if (ipcNotifyEvent == null || ipcNotifyEvent.WaitOne(10000))
                    {
                        byte[] buffer = new byte[IpcResultText.BufferSize];
                        // 結果の MMF（resultDataMmf）から読む。クラス名の MMF は、クライアントのプロセスでは null のため、読むと結果が常に空になる
                        MemoryMappedViewAccessor ipcResultDataMMA = resultDataMmf.CreateViewAccessor(0, buffer.Length);
                        ipcResultDataMMA.ReadArray(0, buffer, 0, buffer.Length);
                        ipcResultDataMMA.Dispose();
                        // UTF-8。最初の 0 まで
                        return IpcResultText.Decode(buffer);
                    }
                }
                catch (Exception)
                {
                    /* Eat all exceptions because errors here are not fatal for DS4Win */
                }
            }

            return String.Empty;
        }

        /// <summary>結果の MMF を手放す（次のクライアントが同じ MMF を作れるよう、ミューテックスを手放す前に呼ぶ）。</summary>
        public void DisposeResultBuffer()
        {
            if (resultDataMmf != null) resultDataMmf.Dispose();
            resultDataMmf = null;
        }

        public void Dispose()
        {
            if (classNameMmf != null) classNameMmf.Dispose();
            classNameMmf = null;
            DisposeResultBuffer();
        }
    }
}
