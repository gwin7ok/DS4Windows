/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using DS4Windows.Input;
using DS4Windows.Core.Model;

namespace DS4Windows.Output.Udp
{

    class UdpServer
    {
        public const int NUMBER_SLOTS = 4;
        private Socket? udpSock;
        private uint serverId;
        private bool running;
        private byte[] recvBuffer = new byte[1024];
        // 送信用の SocketAsyncEventArgs（それぞれ 100 バイトの自前のバッファーを持つ）を使い回す。
        // 同時に送信中にできる数は ARG_BUFFER_LEN まで（_pool の数）。
        private readonly ConcurrentBag<SocketAsyncEventArgs> _argsPool = new ConcurrentBag<SocketAsyncEventArgs>();
        private readonly SemaphoreSlim _pool;
        private const int ARG_BUFFER_LEN = 80;

        [ThreadStatic]
        private static List<IPEndPoint>? _clientsListCache;

        public delegate void GetPadDetail(int padIdx, ref DualShockPadMeta meta);

        private GetPadDetail portInfoGet;

        public UdpServer(GetPadDetail getPadDetailDel, TimeProvider? time = null)
        {
            portInfoGet = getPadDetailDel;
            _clients = new DsuClientRegistry(time);
            _pool = new SemaphoreSlim(ARG_BUFFER_LEN);
            for (int num = 0; num < ARG_BUFFER_LEN; num++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[DsuPacketFormatter.DataRspPacketLength], 0, DsuPacketFormatter.DataRspPacketLength);
                args.Completed += SocketEvent_AsyncCompleted;
                _argsPool.Add(args);
            }
        }

        private void SocketEvent_AsyncCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ReturnArgs(e);
        }

        private void CompletedSynchronousSocketEvent(SocketAsyncEventArgs args)
        {
            ReturnArgs(args);
        }

        private void ReturnArgs(SocketAsyncEventArgs args)
        {
            args.RemoteEndPoint = null;
            _argsPool.Add(args);
            _pool.Release();
        }

        // 送信用の枠を 1 つ借りる（空くまで待つ）。_pool の数と _argsPool の数は一致しているため、必ず取れる。
        private SocketAsyncEventArgs RentArgs(IPEndPoint clientEP)
        {
            _pool.Wait();
            _argsPool.TryTake(out SocketAsyncEventArgs? args);
            args!.RemoteEndPoint = clientEP; // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）
            return args;
        }

        enum MessageType
        {
            DSUC_VersionReq = 0x100000,
            DSUS_VersionRsp = 0x100000,
            DSUC_ListPorts = 0x100001,
            DSUS_PortInfo = 0x100001,
            DSUC_PadDataReq = 0x100002,
            DSUS_PadDataRsp = 0x100002,
        };

        private const ushort MaxProtocolVersion = 1001;
        public const int DATA_RSP_PACKET_LEN = 100;

        private readonly DsuClientRegistry _clients;

        private void SendPacket(IPEndPoint clientEP, byte[] usefulData, ushort reqProtocolVersion = MaxProtocolVersion)
        {
            byte[] packetData = new byte[usefulData.Length + 16];
            int currIdx = DsuPacketFormatter.BeginPacket(packetData, serverId, reqProtocolVersion);
            Array.Copy(usefulData, 0, packetData, currIdx, usefulData.Length);
            DsuPacketFormatter.FinishPacket(packetData);

            //try { udpSock.SendTo(packetData, clientEP); }
            SocketAsyncEventArgs args = RentArgs(clientEP);
            Array.Copy(packetData, args.Buffer!, packetData.Length); // RentArgs が確保したバッファー
            //args.SetBuffer(packetData, 0, packetData.Length);
            bool sentAsync = false;
            try {
                sentAsync = udpSock!.SendToAsync(args); // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）
                //if (!sentAsync) CompletedSynchronousSocketEvent();
            }
            catch (Exception /*e*/) { /* UDP の送信・受信の失敗は無視してよい（相手が切れても、サーバーの動作は続ける） */ }
            finally
            {
                if (!sentAsync) CompletedSynchronousSocketEvent(args);
            }
        }

        private void ProcessIncoming(byte[] localMsg, IPEndPoint clientEP)
        {
            try
            {
                int currIdx = 0;
                if (localMsg[0] != 'D' || localMsg[1] != 'S' || localMsg[2] != 'U' || localMsg[3] != 'C')
                    return;
                else
                    currIdx += 4;

                uint protocolVer = BitConverter.ToUInt16(localMsg, currIdx);
                currIdx += 2;

                if (protocolVer > MaxProtocolVersion)
                    return;

                uint packetSize = BitConverter.ToUInt16(localMsg, currIdx);
                currIdx += 2;

                if (packetSize < 0)
                    return;

                packetSize += 16; //size of header
                if (packetSize > localMsg.Length)
                    return;
                else if (packetSize < localMsg.Length)
                {
                    byte[] newMsg = new byte[packetSize];
                    Array.Copy(localMsg, newMsg, packetSize);
                    localMsg = newMsg;
                }

                uint crcValue = BitConverter.ToUInt32(localMsg, currIdx);
                //zero out the crc32 in the packet once we got it since that's whats needed for calculation
                localMsg[currIdx++] = 0;
                localMsg[currIdx++] = 0;
                localMsg[currIdx++] = 0;
                localMsg[currIdx++] = 0;

                uint crcCalc = Crc32Algorithm.Compute(localMsg);
                if (crcValue != crcCalc)
                    return;

                uint clientId = BitConverter.ToUInt32(localMsg, currIdx);
                currIdx += 4;

                uint messageType = BitConverter.ToUInt32(localMsg, currIdx);
                currIdx += 4;

                if (messageType == (uint)MessageType.DSUC_VersionReq)
                {
                    byte[] outputData = new byte[8];
                    int outIdx = 0;
                    Array.Copy(BitConverter.GetBytes((uint)MessageType.DSUS_VersionRsp), 0, outputData, outIdx, 4);
                    outIdx += 4;
                    Array.Copy(BitConverter.GetBytes((ushort)MaxProtocolVersion), 0, outputData, outIdx, 2);
                    outIdx += 2;
                    outputData[outIdx++] = 0;
                    outputData[outIdx++] = 0;

                    SendPacket(clientEP, outputData, 1001);
                }
                else if (messageType == (uint)MessageType.DSUC_ListPorts)
                {
                    int numPadRequests = BitConverter.ToInt32(localMsg, currIdx);
                    currIdx += 4;
                    if (numPadRequests < 0 || numPadRequests > NUMBER_SLOTS)
                        return;

                    int requestsIdx = currIdx;
                    for (int i = 0; i < numPadRequests; i++)
                    {
                        byte currRequest = localMsg[requestsIdx + i];
                        if (currRequest >= NUMBER_SLOTS)
                            return;
                    }

                    byte[] outputData = new byte[16];
                    for (byte i = 0; i < numPadRequests; i++)
                    {
                        byte currRequest = localMsg[requestsIdx + i];
                        DualShockPadMeta padData = new DualShockPadMeta();
                        portInfoGet(currRequest, ref padData);

                        int outIdx = 0;
                        Array.Copy(BitConverter.GetBytes((uint)MessageType.DSUS_PortInfo), 0, outputData, outIdx, 4);
                        outIdx += 4;

                        outputData[outIdx++] = (byte)padData.PadId;
                        outputData[outIdx++] = (byte)padData.PadState;
                        outputData[outIdx++] = (byte)padData.Model;
                        outputData[outIdx++] = (byte)padData.ConnectionType;

                        byte[]? addressBytes = null;
                        if (padData.PadMacAddress != null)
                            addressBytes = padData.PadMacAddress.GetAddressBytes();

                        if (addressBytes != null && addressBytes.Length == 6)
                        {
                            outputData[outIdx++] = addressBytes[0];
                            outputData[outIdx++] = addressBytes[1];
                            outputData[outIdx++] = addressBytes[2];
                            outputData[outIdx++] = addressBytes[3];
                            outputData[outIdx++] = addressBytes[4];
                            outputData[outIdx++] = addressBytes[5];
                        }
                        else
                        {
                            outputData[outIdx++] = 0;
                            outputData[outIdx++] = 0;
                            outputData[outIdx++] = 0;
                            outputData[outIdx++] = 0;
                            outputData[outIdx++] = 0;
                            outputData[outIdx++] = 0;
                        }

                        outputData[outIdx++] = (byte)padData.BatteryStatus;
                        outputData[outIdx++] = 0;

                        SendPacket(clientEP, outputData, 1001);
                    }
                }
                else if (messageType == (uint)MessageType.DSUC_PadDataReq)
                {
                    byte regFlags = localMsg[currIdx++];
                    byte idToReg = localMsg[currIdx++];
                    PhysicalAddress? macToReg = null;
                    {
                        byte[] macBytes = new byte[6];
                        Array.Copy(localMsg, currIdx, macBytes, 0, macBytes.Length);
                        currIdx += macBytes.Length;
                        macToReg = new PhysicalAddress(macBytes);
                    }

                    _clients.RegisterRequest(clientEP, regFlags, idToReg, macToReg);
                }
            }
            catch (Exception /*e*/) { /* UDP の送信・受信の失敗は無視してよい（相手が切れても、サーバーの動作は続ける） */ }
        }

        private void ReceiveCallback(IAsyncResult iar)
        {
            byte[]? localMsg = null;
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                //Get the received message.
                Socket? recvSock = (Socket?)iar.AsyncState;
                int msgLen = recvSock!.EndReceiveFrom(iar, ref clientEP); // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）

                localMsg = new byte[msgLen];
                Array.Copy(recvBuffer, localMsg, msgLen);
            }
            catch (SocketException)
            {
                if (running)
                {
                    ResetUDPConn();
                }
            }
            catch (Exception /*e*/) { /* UDP の送信・受信の失敗は無視してよい（相手が切れても、サーバーの動作は続ける） */ }

            //Start another receive as soon as we copied the data
            StartReceive();

            //Process the data if its valid
            if (localMsg != null)
                ProcessIncoming(localMsg, (IPEndPoint)clientEP);
        }
        private void StartReceive()
        {
            try
            {
                if (running)
                {
                    //Start listening for a new message.
                    EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                    udpSock!.BeginReceiveFrom(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, ref newClientEP, ReceiveCallback, udpSock); // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）
                }
            }
            catch (SocketException /*ex*/)
            {
                if (running)
                {
                    ResetUDPConn();
                    StartReceive();
                }
            }
        }

        /// <summary>
        /// Used to send CONNRESET ioControlCode to Socket used for UDP Server.
        /// Frees Socket from potentially firing SocketException instances after a client
        /// connection is terminated. Avoids memory leak
        /// </summary>
        private void ResetUDPConn()
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            udpSock!.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null); // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）
        }

        public void Start(int port, string listenAddress = "")
        {
            if (running)
            {
                if (udpSock != null)
                {
                    udpSock.Close();
                    udpSock = null;
                }
                running = false;
            }

            udpSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                IPAddress? udpListenIPAddress;
                if (listenAddress == "127.0.0.1" || listenAddress == "")
                {
                    // Listen on local looback interface (default option). Does not allow remote client connections
                    udpListenIPAddress = IPAddress.Loopback;
                }
                else if (listenAddress == "0.0.0.0")
                {
                    // Listen on all IPV4 interfaces. 
                    // Remote client connections allowed. If the local network is not "safe" then may not be a good idea, because at the moment incoming connections are not authenticated in any way
                    udpListenIPAddress = IPAddress.Any;                    
                }
                else
                {
                    // Listen on a specific hostname or IPV4 interface address. If the hostname has multiple interfaces then use the first IPV4 address because it is usually the primary IP addr.
                    // Remote client connections allowed.
                    IPAddress[] ipAddresses = Dns.GetHostAddresses(listenAddress);
                    udpListenIPAddress = null; 
                    foreach (IPAddress ip4 in ipAddresses.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                    {
                        udpListenIPAddress = ip4;
                        break;
                    }
                    if (udpListenIPAddress == null) throw new SocketException(10049 /*WSAEADDRNOTAVAIL*/);
                }
                udpSock.Bind(new IPEndPoint(udpListenIPAddress, port));
            }
            catch (SocketException)
            {
                udpSock.Close();
                udpSock = null;

                throw;
            }

            byte[] randomBuf = new byte[4];
            new Random().NextBytes(randomBuf);
            serverId = BitConverter.ToUInt32(randomBuf, 0);

            running = true;
            StartReceive();
        }

        public void Stop()
        {
            running = false;
            if (udpSock != null)
            {
                udpSock.Close();
                udpSock = null;
            }
        }

        public unsafe void NewReportIncoming(ref DualShockPadMeta padMeta, ControllerState hidReport, byte[] outputData)
        {
            if (!running)
                return;

            // 入力のスレッドごとに一覧を使い回し、毎フレームの割り当てを避ける
            var clientsList = _clientsListCache ??= new List<IPEndPoint>();
            _clients.CollectRecipients(padMeta.PadId, padMeta.PadMacAddress!, clientsList); // 接続中のパッドだけが来る（従来どおり）

            if (clientsList.Count <= 0)
                return;

            unchecked
            {
                PadDataRspPacket currentRsp = new PadDataRspPacket();
                DsuPacketFormatter.BeginDataRspPacket(ref currentRsp, serverId, 1001);
                currentRsp.messageType = (uint)MessageType.DSUS_PadDataRsp;
                DsuPacketFormatter.FillPadInfo(ref currentRsp, padMeta, hidReport.PacketCounter);
                DsuPacketFormatter.FillPadData(hidReport, ref currentRsp);
                DsuPacketFormatter.FinishDataRspPacket(ref currentRsp, outputData);

                foreach (var cl in clientsList)
                {
                    // try
                    // {
                    //     udpSock.SendTo(outputData, cl);
                    // }
                    // catch (Exception)
                    // {
                    // }

                    SocketAsyncEventArgs args = RentArgs(cl);
                    Array.Copy(outputData, args.Buffer!, outputData.Length); // RentArgs が確保したバッファー
                    bool sentAsync = false;
                    try {
                        sentAsync = udpSock!.SendToAsync(args); // 開始（Start）の後にだけ呼ぶ（null なら従来どおり NullReferenceException）
                    }
                    catch (SocketException /*ex*/) { /* UDP の送信の失敗は無視してよい（相手が切れても、サーバーの動作は続ける） */ }
                    finally
                    {
                        if (!sentAsync) CompletedSynchronousSocketEvent(args);
                    }
                }
            }

            clientsList.Clear();
        }
    }
}
