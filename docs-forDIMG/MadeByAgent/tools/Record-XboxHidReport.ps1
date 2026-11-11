# Xbox 360 (USB, xinputhid) の HID 入力レポートを、操作ごとに記録する調査用スクリプト（Phase22-Step0）。
# 使い方: 実機を USB でつなぎ、DS4Windows を止めた状態（または HidHide で DS4Windows を許可した状態）で実行する。
#   pwsh -File Record-XboxHidReport.ps1 [-Vid 045E] [-ProductId 028E] [-OutFile xbox-reports.txt]
# 仮想パッド（ViGEm）も同じ VID/PID を持つので、一覧から親が実機のものを選ぶ（番号を聞かれる）。
# 出力: 操作ごとに、出現した異なるレポート（15 バイトの 16 進）と、操作前（無操作）との差分バイト。
param(
    [string]$Vid = '045E',
    [string]$ProductId = '028E',
    [string]$OutFile = (Join-Path $PSScriptRoot 'xbox-reports.txt'),
    [string]$Only = ''   # 指定すると、名前にこの文字を含む操作だけを記録する（例: -Only Guide -OutFile xbox-guide.txt）
)

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

public static class XHid
{
    [StructLayout(LayoutKind.Sequential)] struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
    [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr p);
    [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr p);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr p, out HIDP_CAPS caps);
    [StructLayout(LayoutKind.Sequential)] struct HIDP_CAPS { public ushort Usage, UsagePage, InputReportByteLength, OutputReportByteLength, FeatureReportByteLength; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved; public ushort NumberLinkCollectionNodes, NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices, NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices, NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices; }
    [DllImport("setupapi.dll", SetLastError = true)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr h, int flags);
    [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr s, IntPtr d, ref Guid g, int i, ref SP_DEVICE_INTERFACE_DATA data);
    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr s, ref SP_DEVICE_INTERFACE_DATA d, IntPtr detail, int size, out int req, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr s);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(SafeFileHandle h, byte[] buf, int len, out int read, IntPtr ov);
    [DllImport("kernel32.dll")] static extern bool CancelIoEx(SafeFileHandle h, IntPtr ov);

    public static List<string> FindPaths(string vidPid)
    {
        var list = new List<string>();
        Guid g; HidD_GetHidGuid(out g);
        IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12); // PRESENT | DEVICEINTERFACE
        for (int i = 0; ; i++)
        {
            var d = new SP_DEVICE_INTERFACE_DATA(); d.cbSize = Marshal.SizeOf(d);
            if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d)) break;
            int req; SetupDiGetDeviceInterfaceDetail(set, ref d, IntPtr.Zero, 0, out req, IntPtr.Zero);
            IntPtr buf = Marshal.AllocHGlobal(req);
            Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);
            if (SetupDiGetDeviceInterfaceDetail(set, ref d, buf, req, out req, IntPtr.Zero))
            {
                string path = Marshal.PtrToStringAuto(IntPtr.Add(buf, 4));
                if (path.ToLowerInvariant().Contains(vidPid)) list.Add(path);
            }
            Marshal.FreeHGlobal(buf);
        }
        SetupDiDestroyDeviceInfoList(set);
        return list;
    }

    public class Reader : IDisposable
    {
        SafeFileHandle h; Thread t; volatile bool stop; readonly object gate = new object();
        public int ReportLength;
        public volatile bool Ended;
        public int EndError;
        public byte[] Latest = new byte[0];
        public readonly List<string> Seen = new List<string>();
        public Reader(string path)
        {
            h = CreateFile(path, 0x80000000u | 0x40000000u, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h.IsInvalid) h = CreateFile(path, 0x80000000u, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (h.IsInvalid) throw new Exception("open failed: " + Marshal.GetLastWin32Error());
            IntPtr p; HidD_GetPreparsedData(h, out p); HIDP_CAPS caps; HidP_GetCaps(p, out caps); HidD_FreePreparsedData(p);
            ReportLength = caps.InputReportByteLength;
            t = new Thread(Loop) { IsBackground = true }; t.Start();
        }
        void Loop()
        {
            var buf = new byte[Math.Max(ReportLength, 64)];
            while (!stop)
            {
                int n; if (!ReadFile(h, buf, ReportLength, out n, IntPtr.Zero)) { EndError = Marshal.GetLastWin32Error(); Ended = true; break; }
                var r = new byte[n]; Array.Copy(buf, r, n);
                lock (gate) { Latest = r; string s = BitConverter.ToString(r); if (!Seen.Contains(s)) Seen.Add(s); }
            }
        }
        public void ClearSeen() { lock (gate) Seen.Clear(); }
        public string[] SnapshotSeen() { lock (gate) return Seen.ToArray(); }
        public void Dispose() { stop = true; CancelIoEx(h, IntPtr.Zero); h.Dispose(); }
    }
}
'@

$key = ('vid_{0}&pid_{1}' -f $Vid, $ProductId).ToLowerInvariant()
$paths = [XHid]::FindPaths($key)
if ($paths.Count -eq 0) { Write-Host "HID デバイスが見つかりません（$key）。"; exit 1 }
for ($i = 0; $i -lt $paths.Count; $i++) { Write-Host ("[{0}] {1}" -f $i, $paths[$i]) }
$idx = if ($paths.Count -eq 1) { 0 } else { [int](Read-Host '記録する番号（実機の HID の子を選ぶ。仮想パッドは ROOT 配下）') }

$reader = New-Object 'XHid+Reader' -ArgumentList $paths[$idx]
Write-Host ("入力レポート長: {0} バイト" -f $reader.ReportLength)

$ops = @(
    '無操作（何も触らない）',
    'A', 'B', 'X', 'Y', 'LB', 'RB', 'Back', 'Start', 'LS 押し込み', 'RS 押し込み',
    '十字キー 上', '十字キー 右上', '十字キー 右', '十字キー 右下', '十字キー 下', '十字キー 左下', '十字キー 左', '十字キー 左上',
    'LT を半分', 'LT を最大', 'RT を半分', 'RT を最大',
    '左スティック 左いっぱい', '左スティック 右いっぱい', '左スティック 上いっぱい', '左スティック 下いっぱい',
    '右スティック 左いっぱい', '右スティック 右いっぱい', '右スティック 上いっぱい', '右スティック 下いっぱい',
    'A と B と X と Y を同時'
)
# Guide は通常の一覧に含めない（長押しでタスクビューが開く）。-Only Guide を指定したときだけ、短押しを記録する
if ($Only) { $ops = @($ops + 'Guide（Xbox ボタン）を短く押す' + 'Guide（Xbox ボタン）を長押し（3 秒間押し続ける）') | Where-Object { $_ -like "*$Only*" } }
$out = New-Object System.Collections.Generic.List[string]
$out.Add("# Xbox HID report record  $(Get-Date -Format s)  $($paths[$idx])  len=$($reader.ReportLength)")
$baseline = $null
foreach ($op in $ops) {
    Read-Host ("[{0}] Enter を押した「あと」に操作を始め、3 秒間そのまま保持する" -f $op) | Out-Null
    $reader.ClearSeen(); Start-Sleep -Milliseconds 3000
    $seen = $reader.SnapshotSeen()
    $last = [BitConverter]::ToString($reader.Latest)
    if ($reader.Ended) { Write-Host ("  ※読み取りが終了しました（エラー {0}）" -f $reader.EndError); $out.Add("  ※読み取り終了 エラー $($reader.EndError)") }
    if ($null -eq $baseline) { $baseline = [byte[]](0x00,0x00,0x80,0xFF,0x7F,0x00,0x80,0xFF,0x7F,0x00,0x80,0x00,0x00,0x00,0x00) }
    $diff = @()
    for ($b = 0; $b -lt $reader.Latest.Length; $b++) { if ($reader.Latest[$b] -ne $baseline[$b]) { $diff += $b } }
    $line = "{0} | 最後: {1} | 無操作との差分バイト: [{2}] | 出現数: {3}" -f $op, $last, ($diff -join ','), $seen.Count
    Write-Host $line
    $out.Add($line)
    foreach ($s in $seen) { $out.Add("    " + $s) }
}
$reader.Dispose()
$out | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "記録を書き出しました: $OutFile"
