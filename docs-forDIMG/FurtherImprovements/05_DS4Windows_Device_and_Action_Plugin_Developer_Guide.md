# DS4Windows デバイス & アクション プラグイン開発ガイド
**〜 Xbox用コントローラー (Series X|S / Elite 2) のネイティブHID統合を例にした拡張仕様 〜**

- **対象リポジトリ**: `gwin7ok/DS4Windows`
- **文書番号**: 05
- **文書種別**: デバイス・アクション拡張SDK開発ガイド (Plugin Developer Guide)
- **前提文書**: `01_DS4Windows_Architecture_Analysis_Report.md`, `02_DS4Windows_Refactoring_Design_Proposal.md`

---

## 1. カートリッジ拡張アーキテクチャの概要

本仕様書は、コアの内部実装を改変することなく、新しい入力ハードウェア（コントローラー）や特殊アクションを **1つの独立したプラグイン（カートリッジ）** として追加するための開発仕様を定めます。

本書では、Windows環境で最も普及しているにもかかわらず、これまでDS4Windowsの「入力デバイス」として直接扱われてこなかった **「Xbox ワイヤレス コントローラー（Xbox Series X|S / Xbox Elite Series 2 等のネイティブHID入力）」** の完全統合を題材として解説します。

---

## 2. デバイスプラグインの3大構成要素

新しいコントローラーをシステムに認識させるには、以下の3つのインターフェースを実装します。

```text
┌─────────────────────────────────────────────────────────────┐
│ 1. IInputDeviceModule     : VID/PIDの定義、接続検知、ライフサイクル │
├─────────────────────────────────────────────────────────────┤
│ 2. IInputReportParser     : 生HIDバイト列を正規化Stateへ変換（入力）│
├─────────────────────────────────────────────────────────────┤
│ 3. IOutputReportEncoder   : 振動・LED指示を生HIDパケットへ変換（出力）│
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 実践ケーススタディ：Xbox コントローラー (Bluetooth/USB) の統合

### 3.1 ハードウェア仕様とVID/PID
* **ベンダーID (VID)**: `0x045E` (Microsoft Corporation)
* **主なプロダクトID (PID)**:
  * `0x0B12`: Xbox Wireless Controller (Series X|S - USB接続)
  * `0x0B13`: Xbox Wireless Controller (Series X|S - Bluetooth接続 / HID Report ID 0x01)
  * `0x0B05`: Xbox Elite Wireless Controller Series 2 (Bluetooth接続)

---

### 3.2 パケット解析カートリッジ（`XboxReportParser.cs`）

OS依存コードを一切排除し、Bluetooth/USBの生HID入力パケットを `UniversalControllerState` に変換する純粋ロジックです。

```csharp
using System;
using DS4Windows.Core.Input;

namespace DS4Windows.Devices.Xbox
{
    public sealed class XboxReportParser : IInputReportParser
    {
        public bool CanHandle(ushort vid, ushort pid)
        {
            return vid == 0x045E && (pid == 0x0B12 || pid == 0x0B13 || pid == 0x0B05);
        }

        public bool TryParseReport(ReadOnlySpan<byte> report, ref UniversalControllerState outState)
        {
            // Bluetooth接続時の標準入力レポート形式 (Report ID: 0x01, 長さ 16〜48バイト)
            if (report.Length < 16) return false;

            int offset = (report[0] == 0x01) ? 1 : 0;

            // 1. スティック解析 (16ビット符号なし整数 0〜65535 -> -1.0f 〜 +1.0f)
            ushort rawLx = BitConverter.ToUInt16(report.Slice(offset + 0, 2));
            ushort rawLy = BitConverter.ToUInt16(report.Slice(offset + 2, 2));
            ushort rawRx = BitConverter.ToUInt16(report.Slice(offset + 4, 2));
            ushort rawRy = BitConverter.ToUInt16(report.Slice(offset + 6, 2));

            outState.LeftStickX = (rawLx - 32768) / 32768.0f;
            outState.LeftStickY = -(rawLy - 32768) / 32768.0f; // Y軸反転
            outState.RightStickX = (rawRx - 32768) / 32768.0f;
            outState.RightStickY = -(rawRy - 32768) / 32768.0f;

            // 2. アナログトリガー解析 (10ビット 0〜1023 -> 0.0f 〜 1.0f)
            ushort rawLt = BitConverter.ToUInt16(report.Slice(offset + 8, 2));
            ushort rawRt = BitConverter.ToUInt16(report.Slice(offset + 10, 2));
            outState.LeftTrigger = rawLt / 1023.0f;
            outState.RightTrigger = rawRt / 1023.0f;

            // 3. デジタルボタン (A, B, X, Y, LB, RB, View, Menu, Guide)
            ushort buttons = BitConverter.ToUInt16(report.Slice(offset + 13, 2));
            ulong flags = 0;

            if ((buttons & 0x0001) != 0) flags |= (1UL << (int)DS4Controls.Cross);    // A -> Cross
            if ((buttons & 0x0002) != 0) flags |= (1UL << (int)DS4Controls.Circle);   // B -> Circle
            if ((buttons & 0x0008) != 0) flags |= (1UL << (int)DS4Controls.Square);   // X -> Square
            if ((buttons & 0x0010) != 0) flags |= (1UL << (int)DS4Controls.Triangle); // Y -> Triangle
            if ((buttons & 0x0040) != 0) flags |= (1UL << (int)DS4Controls.L1);       // LB -> L1
            if ((buttons & 0x0080) != 0) flags |= (1UL << (int)DS4Controls.R1);       // RB -> R1
            if ((buttons & 0x0100) != 0) flags |= (1UL << (int)DS4Controls.Share);    // View -> Share
            if ((buttons & 0x0200) != 0) flags |= (1UL << (int)DS4Controls.Options);  // Menu -> Options
            if ((buttons & 0x0400) != 0) flags |= (1UL << (int)DS4Controls.L3);       // LSB -> L3
            if ((buttons & 0x0800) != 0) flags |= (1UL << (int)DS4Controls.R3);       // RSB -> R3

            // ハットスイッチ (D-Pad: 1〜8方向)
            byte hat = report[offset + 12];
            switch (hat)
            {
                case 1: flags |= (1UL << (int)DS4Controls.DpadUp); break;
                case 2: flags |= (1UL << (int)DS4Controls.DpadUp) | (1UL << (int)DS4Controls.DpadRight); break;
                case 3: flags |= (1UL << (int)DS4Controls.DpadRight); break;
                case 4: flags |= (1UL << (int)DS4Controls.DpadDown) | (1UL << (int)DS4Controls.DpadRight); break;
                case 5: flags |= (1UL << (int)DS4Controls.DpadDown); break;
                case 6: flags |= (1UL << (int)DS4Controls.DpadDown) | (1UL << (int)DS4Controls.DpadLeft); break;
                case 7: flags |= (1UL << (int)DS4Controls.DpadLeft); break;
                case 8: flags |= (1UL << (int)DS4Controls.DpadUp) | (1UL << (int)DS4Controls.DpadLeft); break;
            }

            outState.ButtonFlags = flags;
            return true;
        }
    }
}
```

---

### 3.3 フィードバック出力カートリッジ（`XboxReportEncoder.cs`）

本体モーター（強弱）およびインパルストリガー（左右トリガー振動）を制御する出力パケット生成器です。

```csharp
namespace DS4Windows.Devices.Xbox
{
    public sealed class XboxReportEncoder : IOutputReportEncoder
    {
        public int BuildRumblePacket(byte heavyRumble, byte lightRumble, byte leftTriggerRumble, byte rightTriggerRumble, Span<byte> outBuffer)
        {
            // Windows標準 Xbox HID 出力レポート (Report ID 0x03)
            outBuffer[0] = 0x03; // Output Report ID
            outBuffer[1] = 0x0F; // モーター有効化フラグ (左右メイン + 左右トリガー)
            outBuffer[2] = leftTriggerRumble;  // 左トリガー振動モーター (0〜255)
            outBuffer[3] = rightTriggerRumble; // 右トリガー振動モーター (0〜255)
            outBuffer[4] = heavyRumble;        // 左メイン重振動 (0〜255)
            outBuffer[5] = lightRumble;        // 右メイン軽振動 (0〜255)
            outBuffer[6] = 0xFF;               // 継続時間 (0xFF = 無制限)
            return 7; // 送信バイト長
        }
    }
}
```

---

### 3.4 デバイスモジュール定義とDI登録（`XboxDeviceModule.cs`）

```csharp
namespace DS4Windows.Devices.Xbox
{
    public sealed class XboxDeviceModule : IInputDeviceModule
    {
        public string DeviceName => "Xbox Wireless Controller";
        public InputDeviceType DeviceType => InputDeviceType.Xbox;

        public IReadOnlyList<VidPidInfo> SupportedDevices { get; } = new[]
        {
            new VidPidInfo(0x045E, 0x0B12, "Xbox Controller (Series X|S USB)"),
            new VidPidInfo(0x045E, 0x0B13, "Xbox Controller (Series X|S BT)"),
            new VidPidInfo(0x045E, 0x0B05, "Xbox Elite Series 2 (BT)")
        };

        public IInputReportParser CreateParser() => new XboxReportParser();
        public IOutputReportEncoder CreateEncoder() => new XboxReportEncoder();
    }
}
```

#### DIコンテナへのプラグイン登録（本体コードの改変はこれだけ）
```csharp
// ServiceRegistration.cs
services.AddSingleton<IInputDeviceModule, XboxDeviceModule>();
```

---

## 4. プラグインの単体テスト作成テンプレート

実機を接続しなくても、キャプチャした生パケットデータを渡すだけで、CI上で100%パース精度を検証できます。

```csharp
public class XboxReportParserTests
{
    [Fact]
    public void TryParseReport_AButtonAndRightTrigger_CorrectlyMapped()
    {
        var parser = new XboxReportParser();
        var state = new UniversalControllerState();

        // テスト用の生Bluetoothパケット (Aボタン押下 + 右トリガー全開)
        byte[] rawPacket = new byte[16];
        rawPacket[0] = 0x01; // Report ID
        rawPacket[11] = 0x03; // RT High byte
        rawPacket[12] = 0xFF; // RT Low byte (1023)
        rawPacket[14] = 0x01; // A Button Flag

        bool success = parser.TryParseReport(rawPacket, ref state);

        Assert.True(success);
        Assert.True((state.ButtonFlags & (1UL << (int)DS4Controls.Cross)) != 0); // AがCrossにマッピング
        Assert.Equal(1.0f, state.RightTrigger, precision: 2);
    }
}
```