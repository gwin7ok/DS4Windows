using DS4Windows.Core.Model;
using System;
using System.Collections.Generic;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-3 のテスト用スタブ一式。UDP のソケットとイベントスレッドを使わず、
    /// サーバーの開始・停止・送出の呼び出しを記録する。
    /// </summary>
    public sealed class FakeUdpMotionServer : IUdpMotionServer
    {
        public readonly List<(int Port, string Address)> Starts = new List<(int, string)>();
        public int Stops;
        public Exception StartException;
        public readonly List<(byte PadId, double AccelXG, double AngVelYaw, byte[] Buffer)> Reports = new List<(byte, double, double, byte[])>();

        public void Start(int port, string listenAddress = "")
        {
            Starts.Add((port, listenAddress));
            if (StartException != null) throw StartException;
        }

        public void Stop() { Stops++; }

        public void NewReportIncoming(ref DualShockPadMeta padMeta, ControllerState hidReport, byte[] outputData)
        {
            Reports.Add((padMeta.PadId, hidReport.Motion.accelXG, hidReport.Motion.angVelYaw, outputData));
        }
    }

    public sealed class FakeUdpMotionServerFactory : IUdpMotionServerFactory
    {
        public readonly FakeUdpMotionServer Server = new FakeUdpMotionServer();
        public int Created;
        public UdpPadDetailProvider LastProvider;

        public IUdpMotionServer Create(UdpPadDetailProvider getPadDetail)
        {
            Created++;
            LastProvider = getPadDetail;
            return Server;
        }
    }
}
