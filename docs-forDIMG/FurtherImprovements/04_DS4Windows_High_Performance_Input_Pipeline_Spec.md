# DS4Windows 高性能入力パイプライン & リアルタイム最適化仕様書
**〜 1000Hz (1ms) ポーリング・ゼロアロケーション・低ジッター入力処理の極限設計 〜**

- **対象リポジトリ**: `gwin7ok/DS4Windows`
- **文書番号**: 04
- **文書種別**: パフォーマンス・リアルタイム入出力詳細仕様書 (Performance Specification)
- **前提文書**: `01_DS4Windows_Architecture_Analysis_Report.md`, `02_DS4Windows_Refactoring_Design_Proposal.md`

---

## 1. リアルタイム性能目標と制約条件

ゲームパッドエミュレータであるDS4Windowsにおいて、アーキテクチャのモジュール化・疎結合化が**入力遅延（Input Latency）やフレーム落ち（GCスタッター）を招くことは絶対に許容されません**。

| 指標 | 目標値 | 許容限界 | 測定条件 |
| :--- | :---: | :---: | :--- |
| **ポーリング周期** | **1,000 Hz (1.0 ms)** | 250 Hz (4.0 ms) | USB / 高速BT接続時 |
| **パイプライン処理時間** | **< 0.05 ms (50 µs)** | < 0.20 ms (200 µs) | 生HID受信から仮想パッド送信完了まで |
| **GCアロケーション** | **0 bytes / frame** | 0 bytes / frame | ホットパス走行中の Gen0 アロケーション |
| **ジッター (遅延のブレ)** | **< 0.1 ms** | < 0.5 ms | 10,000フレーム連続サンプリング時 |

---

## 2. ゼロアロケーション（Zero-Allocation）データパイプライン

### 2.1 メモリレイアウトの設計：`UniversalControllerState`
ヒープを一切汚染しないため、入力状態はすべてスタック上に確保される値型（`struct`）として定義し、明示的なメモリレイアウト（`LayoutKind.Explicit`）でキャッシュライン（64バイト）に最適化します。

```csharp
using System.Runtime.InteropServices;

namespace DS4Windows.Core.Input
{
    // 64バイト（一般的なCPUキャッシュラインサイズ）に完全に収める構造体設計
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct UniversalControllerState
    {
        // [Offset 0〜15]: アナログスティック (正規化 -1.0f 〜 +1.0f)
        [FieldOffset(0)]  public float LeftStickX;
        [FieldOffset(4)]  public float LeftStickY;
        [FieldOffset(8)]  public float RightStickX;
        [FieldOffset(12)] public float RightStickY;

        // [Offset 16〜23]: アナログトリガー (0.0f 〜 1.0f)
        [FieldOffset(16)] public float LeftTrigger;
        [FieldOffset(20)] public float RightTrigger;

        // [Offset 24〜31]: デジタルボタン（64ビットビットマスクで全ボタンを1整数化）
        [FieldOffset(24)] public ulong ButtonFlags;

        // [Offset 32〜47]: 6軸センサー (ジャイロ角速度 & 加速度)
        [FieldOffset(32)] public short AngularVelocityX;
        [FieldOffset(34)] public short AngularVelocityY;
        [FieldOffset(36)] public short AngularVelocityZ;
        [FieldOffset(38)] public short AccelerometerX;
        [FieldOffset(40)] public short AccelerometerY;
        [FieldOffset(42)] public short AccelerometerZ;

        // [Offset 48〜55]: タッチパッド座標 (Touch 1, Touch 2)
        [FieldOffset(48)] public ushort Touch1X;
        [FieldOffset(50)] public ushort Touch1Y;
        [FieldOffset(52)] public byte Touch1Id;
        [FieldOffset(53)] public byte Touch1Active;

        // [Offset 56〜63]: メタ情報 (タイムスタンプ, バッテリー)
        [FieldOffset(56)] public uint TimestampUs; // マイクロ秒タイムスタンプ
        [FieldOffset(60)] public byte BatteryLevel;
        [FieldOffset(61)] public byte IsCharging;
    }
}
```

### 2.2 ゼロコピー・パケット解析（`ReadOnlySpan<byte>`）
ヒープへのバイト配列コピーを一切行わず、OSのHIDバッファを直接 `Span` でスライスして解析します。

```csharp
public unsafe interface IInputReportParser
{
    // ref struct または ref 引数により、スタック渡しでゼロアロケーションを保証
    bool TryParseReport(ReadOnlySpan<byte> reportBuffer, ref UniversalControllerState outState);
}
```

---

## 3. インターフェース抽象化とバーチャルコール撲滅（Devirtualization）

### 3.1 課題：インターフェース呼び出し（`vtable`）のコスト
1msに1回走るホットパスにおいて、インターフェース経由のメソッド呼び出しは、仮想関数テーブルの参照（インダイレクション）とインライン化の阻害を引き起こします。

### 3.2 解決策：ジェネリクス制約による静的ディスパッチ
C#コンパイラ（JIT）の **Devirtualization（脱仮想化）** を利用し、実行時のオーバーヘッドをゼロ（通常の直接呼び出しと同等）にします。

```csharp
// ジェネリクス型引数 TParser に構造体パーサーを渡すことで、JITが直接コードを展開・インライン化する
public sealed class HighPerformanceInputWorker<TParser>
    where TParser : struct, IInputReportParser
{
    private TParser _parser;
    private UniversalControllerState _state;

    public void ProcessRawPacket(ReadOnlySpan<byte> packet)
    {
        // 仮想呼び出しにならず、インライン化される
        if (_parser.TryParseReport(packet, ref _state))
        {
            DispatchToMapping(in _state);
        }
    }
}
```

---

## 4. 仮想パッド出力（ViGEm）のロックフリー送信パイプライン

### 4.1 現状の課題
複数のスレッド（入力ポーリングスレッド、マクロ再生スレッド、UIスレッド）が同時に仮想コントローラー出力を叩く際、`lock (locker)` によるスレッド競合が発生し、マイクロ秒オーダーのジッター（遅延の揺らぎ）を生んでいます。

### 4.2 ロックフリー・シングルライターパイプライン設計

```text
[ 入力スレッド (1000Hz) ] ──┐
[ マクロスレッド ]         ──┼──> [ Lock-Free SPSC RingBuffer ] ──> [ 専用送信ワーカースレッド ] ──> ViGEm Driver
[ UI / 特殊アクション ]    ──┘
```

```csharp
using System.Threading.Channels;

public sealed class LockFreeOutputDispatcher
{
    // 単一コンシューマ（ViGEm書き込み専用スレッド）向けに最適化されたチャネル
    private readonly Channel<UniversalControllerState> _queue = Channel.CreateBounded<UniversalControllerState>(
        new BoundedChannelOptions(4)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropOldest // 滞留時は常に最新の入力フレームを優先
        });

    public void SubmitState(in UniversalControllerState state)
    {
        _queue.Writer.TryWrite(state);
    }

    public async Task StartSendLoopAsync(IVirtualPadBackend backend, CancellationToken ct)
    {
        var reader = _queue.Reader;
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var state))
            {
                // ViGEm 送信APIへのダイレクト発行
                backend.SendReport(in state);
            }
        }
    }
}
```

---

## 5. パフォーマンス回帰テスト基準（BenchmarkDotNet）

リファクタリングによってナノ秒単位でも性能劣化が起きないよう、以下の自動ベンチマークをテストスイートに組み込みます。

```csharp
[MemoryDiagnoser] // アロケーションを厳密に監視
public class InputPipelineBenchmark
{
    private byte[] _samplePacket;
    private UniversalControllerState _state;
    private Ds4ReportParser _parser;

    [GlobalSetup]
    public void Setup()
    {
        _samplePacket = new byte[64];
        _samplePacket[0] = 0x01; // Sample DS4 Input Report
    }

    [Benchmark]
    public bool ParseBenchmark()
    {
        return _parser.TryParseReport(_samplePacket, ref _state);
    }

    // 合格基準: Mean < 50.0 ns, Allocated = 0 B
}
```