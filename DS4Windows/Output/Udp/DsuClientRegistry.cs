using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace DS4Windows.Output.Udp
{
    /// <summary>
    /// Cemuhook（DSU）のクライアントの登録簿。クライアントが「どのパッドのデータが欲しいか」の要求を、
    /// 最後の要求の時刻つきで覚え、5 秒以内に要求のあったものだけを送信の相手にする。
    /// 全パッド・番号・MAC のいずれの要求も 5 秒以上ないクライアントは、取り除く。
    /// 時刻は <see cref="TimeProvider"/> から取る（テストで時間を進められる）。排他は、この型の中で行う。
    /// </summary>
    public sealed class DsuClientRegistry
    {
        public const int PadCount = 4;
        private const double TimeoutSeconds = 5;

        private sealed class ClientRequestTimes
        {
            public DateTime AllPads = DateTime.MinValue;
            public readonly DateTime[] PadIds = new DateTime[PadCount];
            public readonly Dictionary<PhysicalAddress, DateTime> PadMacs = new Dictionary<PhysicalAddress, DateTime>();

            public ClientRequestTimes()
            {
                for (int i = 0; i < PadIds.Length; i++)
                    PadIds[i] = DateTime.MinValue;
            }

            public void RequestPadInfo(DateTime now, byte regFlags, byte idToReg, PhysicalAddress macToReg)
            {
                if (regFlags == 0)
                {
                    AllPads = now;
                    return;
                }

                if ((regFlags & 0x01) != 0 && idToReg < PadIds.Length) // 番号の指定
                    PadIds[idToReg] = now;
                if ((regFlags & 0x02) != 0) // MAC の指定
                    PadMacs[macToReg] = now;
            }
        }

        private readonly Dictionary<IPEndPoint, ClientRequestTimes> _clients = new Dictionary<IPEndPoint, ClientRequestTimes>();
        private readonly TimeProvider _time;
        private List<IPEndPoint> _deleteScratch;

        public DsuClientRegistry(TimeProvider time = null)
        {
            _time = time ?? TimeProvider.System;
        }

        /// <summary>登録されているクライアントの数。</summary>
        public int Count
        {
            get { lock (_clients) return _clients.Count; }
        }

        /// <summary>クライアントのパッドのデータの要求（PadDataReq）を記録する。初めてのクライアントは登録する。</summary>
        public void RegisterRequest(IPEndPoint client, byte regFlags, byte idToReg, PhysicalAddress macToReg)
        {
            DateTime now = _time.GetUtcNow().UtcDateTime;
            lock (_clients)
            {
                if (!_clients.TryGetValue(client, out ClientRequestTimes times))
                {
                    times = new ClientRequestTimes();
                    _clients[client] = times;
                }

                times.RequestPadInfo(now, regFlags, idToReg, macToReg);
            }
        }

        /// <summary>
        /// 指定のパッドのデータを送る相手を、<paramref name="into"/> へ集める（先に空にする）。
        /// どのパッドの要求も 5 秒以上ないクライアントは、登録から取り除く。
        /// </summary>
        public void CollectRecipients(int padId, PhysicalAddress padMac, List<IPEndPoint> into)
        {
            into.Clear();
            DateTime now = _time.GetUtcNow().UtcDateTime;
            lock (_clients)
            {
                List<IPEndPoint> toDelete = _deleteScratch;
                toDelete?.Clear();

                foreach (var cl in _clients)
                {
                    ClientRequestTimes t = cl.Value;
                    if ((now - t.AllPads).TotalSeconds < TimeoutSeconds)
                        into.Add(cl.Key);
                    else if (padId < t.PadIds.Length && (now - t.PadIds[(byte)padId]).TotalSeconds < TimeoutSeconds)
                        into.Add(cl.Key);
                    else if (padMac != null && t.PadMacs.TryGetValue(padMac, out DateTime macTime)
                             && (now - macTime).TotalSeconds < TimeoutSeconds)
                        into.Add(cl.Key);
                    else if (!IsAlive(t, now))
                        (toDelete ??= (_deleteScratch = new List<IPEndPoint>())).Add(cl.Key);
                }

                if (toDelete != null)
                {
                    foreach (var dead in toDelete)
                        _clients.Remove(dead);
                    toDelete.Clear();
                }
            }
        }

        private static bool IsAlive(ClientRequestTimes t, DateTime now)
        {
            for (int i = 0; i < t.PadIds.Length; i++)
            {
                if ((now - t.PadIds[i]).TotalSeconds < TimeoutSeconds)
                    return true;
            }

            foreach (var mac in t.PadMacs)
            {
                if ((now - mac.Value).TotalSeconds < TimeoutSeconds)
                    return true;
            }

            return false;
        }
    }
}
