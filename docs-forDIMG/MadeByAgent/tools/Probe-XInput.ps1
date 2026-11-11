# XInput の調査用スクリプト（Phase22-Step1b）。読み取り専用（振動や設定の変更はしない）。
# 目的:
#   (a) XInputGetStateEx（序数 100）で、番号 0〜3 の状態（Guide を含む）が読めるか
#   (b) XInputGetCapabilitiesEx（序数 108）で、番号ごとの VID/PID が取れるか
#   (c) XUSB のデバイス インターフェース（GUID EC87F1E3-...）の一覧と、その親の連鎖（仮想パッドの識別の手がかり）
#   (d) 利用者が操作したとき、どの番号が反応するか（番号と物理の機器の対応づけ）
# 使い方: pwsh -File Probe-XInput.ps1 [-Seconds 15] [-OutFile xinput-probe.txt]
#   実行中、確かめたい機器のボタン（A など）を押したり離したりする。Guide も押してみる。
param(
    [int]$Seconds = 15,
    [string]$OutFile = (Join-Path $PSScriptRoot 'xinput-probe.txt')
)

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class XProbe
{
    [StructLayout(LayoutKind.Sequential)] public struct GAMEPAD { public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }
    [StructLayout(LayoutKind.Sequential)] public struct STATE { public uint Packet; public GAMEPAD Pad; }
    [StructLayout(LayoutKind.Sequential)] public struct VIBRATION { public ushort L, R; }
    [StructLayout(LayoutKind.Sequential)] public struct CAPS { public byte Type, SubType; public ushort Flags; public GAMEPAD Pad; public VIBRATION Vib; }
    [StructLayout(LayoutKind.Sequential)] public struct CAPS_EX { public CAPS Caps; public ushort VendorId, ProductId, Version, Unk1; public uint Unk2; }

    [DllImport("xinput1_4.dll", EntryPoint = "#100")] static extern uint GetStateEx(uint index, out STATE s);
    [DllImport("xinput1_4.dll", EntryPoint = "#108")] static extern uint GetCapsEx(uint one, uint index, uint flags, out CAPS_EX c);
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] static extern uint GetState(uint index, out STATE s);

    public static string State(uint i)
    {
        STATE s; uint r;
        try { r = GetStateEx(i, out s); } catch (Exception) { r = GetState(i, out s); }
        if (r != 0) return "未接続(" + r + ")";
        return string.Format("pkt={0} btn=0x{1:X4} LT={2} RT={3} L=({4},{5}) R=({6},{7})", s.Packet, s.Pad.Buttons, s.Pad.LT, s.Pad.RT, s.Pad.LX, s.Pad.LY, s.Pad.RX, s.Pad.RY);
    }
    public static string Caps(uint i)
    {
        CAPS_EX c; uint r;
        try { r = GetCapsEx(1, i, 0, out c); } catch (Exception e) { return "取得不可: " + e.Message; }
        if (r != 0) return "未接続(" + r + ")";
        return string.Format("VID={0:X4} PID={1:X4} ver={2:X4} type={3} subtype={4} flags=0x{5:X4}", c.VendorId, c.ProductId, c.Version, c.Caps.Type, c.Caps.SubType, c.Caps.Flags);
    }

    // --- XUSB デバイス インターフェースの列挙 ---
    [StructLayout(LayoutKind.Sequential)] struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid Class; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct SP_DEVINFO_DATA { public int cbSize; public Guid Class; public int DevInst; public IntPtr Reserved; }
    [DllImport("setupapi.dll", SetLastError = true)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr h, int f);
    [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, int i, ref SP_DEVICE_INTERFACE_DATA data);
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DEVICE_INTERFACE_DATA d, IntPtr detail, int size, out int req, ref SP_DEVINFO_DATA info);
    [DllImport("setupapi.dll", CharSet = CharSet.Auto)] static extern bool SetupDiGetDeviceInstanceId(IntPtr s, ref SP_DEVINFO_DATA d, System.Text.StringBuilder id, int size, out int req);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
    [DllImport("cfgmgr32.dll")] static extern int CM_Get_Parent(out int parent, int dev, int flags);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Auto)] static extern int CM_Get_Device_ID(int dev, System.Text.StringBuilder buf, int len, int flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string n, uint a, uint s, IntPtr sec, uint d, uint f, IntPtr t);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle h, uint code, byte[] i, int il, byte[] o, int ol, out int ret, IntPtr ov);

    // 読み取り専用の IOCTL: GET_INFORMATION（0x80006000。末尾 4 バイトが VID/PID）と GET_LED_STATE（0x8000E008。入力 00-00-00。3 バイト目が LED の模様。6〜9 が番号 0〜3）
    static string Ioctl(string path)
    {
        var h = CreateFile(path, 0xC0000000u, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (h.IsInvalid) return "open失敗 " + Marshal.GetLastWin32Error();
        string r = "";
        int n; var o = new byte[64];
        if (DeviceIoControl(h, 0x80006000u, new byte[3], 3, o, 64, out n, IntPtr.Zero) && n >= 12)
            r += string.Format("VID={0:X4} PID={1:X4}", o[8] | (o[9] << 8), o[10] | (o[11] << 8));
        o = new byte[64];
        if (DeviceIoControl(h, 0x8000E008u, new byte[3], 3, o, 64, out n, IntPtr.Zero) && n >= 3)
            r += string.Format("  LED={0} → XInput 番号 {1}", o[2], o[2] - 6);
        h.Dispose();
        return r;
    }

    public static List<string> Xusb()
    {
        var list = new List<string>();
        Guid g = new Guid("EC87F1E3-C13B-4100-B5F7-8B84D54260CB");
        IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12);
        for (int i = 0; ; i++)
        {
            var d = new SP_DEVICE_INTERFACE_DATA(); d.cbSize = Marshal.SizeOf(d);
            if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d)) break;
            var info = new SP_DEVINFO_DATA(); info.cbSize = Marshal.SizeOf(info);
            int req; SetupDiGetDeviceInterfaceDetail(set, ref d, IntPtr.Zero, 0, out req, ref info);
            IntPtr buf = Marshal.AllocHGlobal(req);
            Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);
            string path = "";
            if (SetupDiGetDeviceInterfaceDetail(set, ref d, buf, req, out req, ref info)) path = Marshal.PtrToStringAuto(IntPtr.Add(buf, 4));
            Marshal.FreeHGlobal(buf);
            var sb = new System.Text.StringBuilder(512); int need;
            SetupDiGetDeviceInstanceId(set, ref info, sb, 512, out need);
            string chain = sb.ToString();
            int dev = info.DevInst;
            for (int depth = 0; depth < 8; depth++)
            {
                int parent; if (CM_Get_Parent(out parent, dev, 0) != 0) break;
                var pb = new System.Text.StringBuilder(512); CM_Get_Device_ID(parent, pb, 512, 0);
                chain += "  <-  " + pb;
                dev = parent;
            }
            list.Add(path + "\n      IOCTL: " + Ioctl(path) + "\n      " + chain);
        }
        SetupDiDestroyDeviceInfoList(set);
        return list;
    }
}
'@

$out = New-Object System.Collections.Generic.List[string]
function Emit([string]$s) { Write-Host $s; $out.Add($s) }

Emit "# XInput probe $(Get-Date -Format s)"
Emit "## XUSB デバイス インターフェース（機器と親の連鎖）"
$x = [XProbe]::Xusb()
if ($x.Count -eq 0) { Emit "  （なし）" }
foreach ($e in $x) { Emit "  $e" }

Emit "## 番号ごとの能力（VID/PID）と、はじめの状態"
for ($i = 0; $i -lt 4; $i++) {
    Emit ("  [{0}] caps: {1}" -f $i, [XProbe]::Caps($i))
    Emit ("  [{0}] state: {1}" -f $i, [XProbe]::State($i))
}

Emit "## 変化の記録（$Seconds 秒。確かめたい機器のボタンを押す。Guide も押す）"
$last = @('', '', '', '')
$end = (Get-Date).AddSeconds($Seconds)
while ((Get-Date) -lt $end) {
    for ($i = 0; $i -lt 4; $i++) {
        $s = [XProbe]::State($i)
        $key = ($s -replace 'pkt=\d+ ', '')
        if ($key -ne $last[$i]) {
            $last[$i] = $key
            Emit ("  {0:HH:mm:ss.fff} [{1}] {2}" -f (Get-Date), $i, $s)
        }
    }
    Start-Sleep -Milliseconds 20
}
$out | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "記録を書き出しました: $OutFile"
