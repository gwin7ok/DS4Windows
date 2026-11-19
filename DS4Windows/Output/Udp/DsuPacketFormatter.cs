#nullable enable

using System;
using System.Runtime.InteropServices;
using DS4Windows.Core.Model;
using DS4Windows.Input;

namespace DS4Windows.Output.Udp
{
    /// <summary>
    /// Cemuhook（DSU）の応答パケットを組み立てる、状態を持たない書式の部品。
    /// 外部のクライアントとの互換のため、バイト列は <c>UdpPacketGoldenTests</c> の黄金で凍結している。
    /// ソケット・クライアントの管理には触れない。
    /// </summary>
    public static unsafe class DsuPacketFormatter
    {
        public const int DataRspPacketLength = 100;
        private const int HeaderLength = 16;

        // CalculateBasicHash が使う高速テーブルは、既定の多項式でテーブルを作った時点で初めて埋まる。
        // 他で CRC を使う前に最初のパケットを作ると、CRC が不正（FFFFFFFF）になるため、ここで必ず初期化する。
        static DsuPacketFormatter()
        {
            Crc32Algorithm.InitializeTable(Crc32Algorithm.DefaultPolynomial);
        }

        /// <summary>ヘッダー（"DSUS"、版、長さ、CRC の場所、サーバー ID）を書き、次の書き込み位置を返す。</summary>
        public static int BeginPacket(byte[] packetBuf, uint serverId, ushort protocolVersion)
        {
            int currIdx = 0;
            packetBuf[currIdx++] = (byte)'D';
            packetBuf[currIdx++] = (byte)'S';
            packetBuf[currIdx++] = (byte)'U';
            packetBuf[currIdx++] = (byte)'S';

            Array.Copy(BitConverter.GetBytes(protocolVersion), 0, packetBuf, currIdx, 2);
            currIdx += 2;

            Array.Copy(BitConverter.GetBytes((ushort)packetBuf.Length - HeaderLength), 0, packetBuf, currIdx, 2);
            currIdx += 2;

            Array.Clear(packetBuf, currIdx, 4); // CRC の場所
            currIdx += 4;

            Array.Copy(BitConverter.GetBytes(serverId), 0, packetBuf, currIdx, 4);
            currIdx += 4;

            return currIdx;
        }

        public static void BeginDataRspPacket(ref PadDataRspPacket rsp, uint serverId, ushort protocolVersion)
        {
            rsp.initCode[0] = (byte)'D';
            rsp.initCode[1] = (byte)'S';
            rsp.initCode[2] = (byte)'U';
            rsp.initCode[3] = (byte)'S';

            rsp.protocolVersion = protocolVersion;
            rsp.messageLen = (ushort)DataRspPacketLength - HeaderLength;

            rsp.crc = 0;
            rsp.serverId = serverId;
        }

        public static void FinishPacket(byte[] packetBuf)
        {
            Array.Clear(packetBuf, 8, 4);
            WriteCrc(packetBuf);
        }

        public static void FinishDataRspPacket(ref PadDataRspPacket rsp, byte[] packetBuf)
        {
            rsp.crc = 0;
            CopyBytes(ref rsp, packetBuf, DataRspPacketLength);
            WriteCrc(packetBuf);
        }

        /// <summary>パッドの情報（番号、状態、機種、接続、MAC アドレス、電池）を書く。MAC は配列を作らずに書く。</summary>
        public static void FillPadInfo(ref PadDataRspPacket rsp, in DualShockPadMeta meta, uint packetCounter)
        {
            rsp.padId = (byte)meta.PadId;
            rsp.padState = (byte)meta.PadState;
            rsp.model = (byte)meta.Model;
            rsp.connectionType = (byte)meta.ConnectionType;
            byte[] mac = meta.PadMacAddress.GetAddressBytes();
            for (int i = 0; i < 6; i++)
                rsp.address[i] = mac[i];
            rsp.batteryStatus = (byte)meta.BatteryStatus;
            rsp.isActive = meta.IsActive ? (byte)1 : (byte)0;
            rsp.packetCounter = packetCounter;
        }

        /// <summary>入力の 1 フレーム（ボタン、スティック、タッチ、モーション）を応答へ書く。</summary>
        public static void FillPadData(ControllerState hidReport, ref PadDataRspPacket rsp)
        {
            unchecked
            {
                rsp.buttons1 = 0;
                if (hidReport.DpadLeft) rsp.buttons1 |= 0x80;
                if (hidReport.DpadDown) rsp.buttons1 |= 0x40;
                if (hidReport.DpadRight) rsp.buttons1 |= 0x20;
                if (hidReport.DpadUp) rsp.buttons1 |= 0x10;

                if (hidReport.Options) rsp.buttons1 |= 0x08;
                if (hidReport.R3) rsp.buttons1 |= 0x04;
                if (hidReport.L3) rsp.buttons1 |= 0x02;
                if (hidReport.Share) rsp.buttons1 |= 0x01;

                rsp.buttons2 = 0;

                if (hidReport.Square) rsp.buttons2 |= 0x80;
                if (hidReport.Cross) rsp.buttons2 |= 0x40;
                if (hidReport.Circle) rsp.buttons2 |= 0x20;
                if (hidReport.Triangle) rsp.buttons2 |= 0x10;

                if (hidReport.R1) rsp.buttons2 |= 0x08;
                if (hidReport.L1) rsp.buttons2 |= 0x04;
                if (hidReport.R2Btn) rsp.buttons2 |= 0x02;
                if (hidReport.L2Btn) rsp.buttons2 |= 0x01;

                rsp.psButton = hidReport.PS ? (byte)1 : (byte)0;
                rsp.touchButton = hidReport.TouchButton ? (byte)1 : (byte)0;

                // スティック（Y は慣例により反転する）
                rsp.lx = hidReport.LX;
                rsp.ly = (byte)(255 - hidReport.LY);
                rsp.rx = hidReport.RX;
                rsp.ry = (byte)(255 - hidReport.RY);

                // DS4 にはアナログのボタンがないため、押している間は 0xFF
                rsp.dpadLeft = hidReport.DpadLeft ? (byte)0xFF : (byte)0x00;
                rsp.dpadDown = hidReport.DpadDown ? (byte)0xFF : (byte)0x00;
                rsp.dpadRight = hidReport.DpadRight ? (byte)0xFF : (byte)0x00;
                rsp.dpadUp = hidReport.DpadUp ? (byte)0xFF : (byte)0x00;

                rsp.square = hidReport.Square ? (byte)0xFF : (byte)0x00;
                rsp.cross = hidReport.Cross ? (byte)0xFF : (byte)0x00;
                rsp.circle = hidReport.Circle ? (byte)0xFF : (byte)0x00;
                rsp.triangle = hidReport.Triangle ? (byte)0xFF : (byte)0x00;

                rsp.r1 = hidReport.R1 ? (byte)0xFF : (byte)0x00;
                rsp.l1 = hidReport.L1 ? (byte)0xFF : (byte)0x00;

                rsp.r2 = hidReport.R2;
                rsp.l2 = hidReport.L2;

                // タッチパッドの 2 点
                var t0 = hidReport.TrackPadTouch0;
                rsp.touch1Active = t0.IsActive ? (byte)1 : (byte)0;
                rsp.touch1PacketId = t0.Id;
                rsp.touch1X = (ushort)t0.X;
                rsp.touch1Y = (ushort)t0.Y;
                var t1 = hidReport.TrackPadTouch1;
                rsp.touch2Active = t1.IsActive ? (byte)1 : (byte)0;
                rsp.touch2PacketId = t1.Id;
                rsp.touch2X = (ushort)t1.X;
                rsp.touch2Y = (ushort)t1.Y;

                // モーション（無いときは 0。加速度の Z は符号を反転する）
                var motion = hidReport.Motion;
                if (motion != null)
                {
                    rsp.totalMicroSec = hidReport.totalMicroSec;
                    rsp.accelXG = (float)motion.accelXG;
                    rsp.accelYG = (float)motion.accelYG;
                    rsp.accelZG = (float)-motion.accelZG;
                    rsp.angVelPitch = (float)motion.angVelPitch;
                    rsp.angVelYaw = (float)motion.angVelYaw;
                    rsp.angVelRoll = (float)motion.angVelRoll;
                }
                else
                {
                    rsp.totalMicroSec = 0;
                    rsp.accelXG = 0;
                    rsp.accelYG = 0;
                    rsp.accelZG = 0;
                    rsp.angVelPitch = 0;
                    rsp.angVelYaw = 0;
                    rsp.angVelRoll = 0;
                }
            }
        }

        private static void WriteCrc(byte[] packetBuf)
        {
            uint seed = Crc32Algorithm.DefaultSeed;
            uint crcCalc = ~Crc32Algorithm.CalculateBasicHash(ref seed, ref packetBuf, 0, packetBuf.Length);
            Array.Copy(BitConverter.GetBytes(crcCalc), 0, packetBuf, 8, 4);
        }

        // 構造体を箱に入れず（割り当てなしで）そのままコピーする。
        private static void CopyBytes(ref PadDataRspPacket rsp, byte[] outBuffer, int bufferLen)
        {
            fixed (PadDataRspPacket* p = &rsp)
            {
                Marshal.Copy((IntPtr)p, outBuffer, 0, bufferLen);
            }
        }
    }
}
