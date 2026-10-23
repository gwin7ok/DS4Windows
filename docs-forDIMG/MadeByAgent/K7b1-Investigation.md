# K7b-1（意図しない終了。メモリ破壊の疑い）の調査記録

作成日: 2026-10-23
関連: `Phase7c-Completion-Report.md` §4（切り分け手順）、`Phase8-Completion-Report.md` §4（2026-10-20 の 0xc0000005）、`Phase9-Completion-Report.md` §5

## 1. 事実

- クラッシュダンプ（`DS4Windows.exe.*.dmp`、各 700〜950 MB）が **7 個**ある（2026-10-21 11:10／12:42、10-22 4:37／4:40／20:18／20:24、10-23 5:29）。「再現しない」ではなく、**3 日間で 7 回**落ちている。
- 過去の発生（2026-09-26 の 3 回）は、落ちた場所・種類が毎回違った（入力スレッドの `IndexOutOfRangeException`［`DS4Device.sendOutputReport`］、`coreclr.dll` のアクセス違反、UI スレッドの `AccessViolationException`）。ヒープの破壊に典型的な症状。
- 2026-10-20 の落ち方: 29 秒間ログなし → `coreclr.dll` のアクセス違反（0xc0000005）。管理下の例外のログは 0 件。

## 2. ソースの静的調査（2026-10-23）

`unsafe`／ネイティブ I/O を使う領域を調べた（`HidDevice`、`DS4Device`、`DS4SixAxis`、`DS4OutDeviceExtras`、`HidHideAPIDevice`、`Util`）。**有力な原因を `HidLibrary/HidDevice.cs` で見つけた。**

### 2.1 `ReadFile`（入力スレッドの読み取り。毎フレーム）

```
fixed (byte* pBuffer = inputBuffer)
{
    if (ReadFile(handle, pBuffer, len, null, &ov)) return Success;
}                                    // ← ここで固定が終わる
...
GetOverlappedResultEx(handle, ov, out _, timeout, true)   // ← 保留中の I/O の完了を、固定の外で待つ
```

- 非同期の読み取りが保留の間、バッファ（`DS4Device.inputReport`／`btInputReport` などの `byte[]`）が**固定されていない**。待っている間に GC がこの配列を移動すると、カーネルは**古い位置**へ書き込む。ヒープが壊れ、落ちる場所も種類も毎回変わる。
- GC は、画面を開く・ログを出すなど、割り当てのある操作で起きる。「ColorPicker を開く → 落ちることがある」という再現手順（`Phase7c-Completion-Report.md` §4.1）と整合する。
- 待ち（`GetOverlappedResultEx`）が失敗・タイムアウト（Switch Pro の `SUBCOMMAND_RESPONSE_TIMEOUT`、`WAIT_IO_COMPLETION` など）したときは、I/O が保留のまま関数を抜ける。スタック上の `NativeOverlapped` と、`using` で破棄したイベントが無効になり、後で完了したときに、解放済みのスタック・ハンドルへ書き込む（アクセス違反）。

### 2.2 `WriteOutputReportViaInterrupt`（出力レポートの書き込み）

同じ構造（`fixed` が `WriteFile` の直後に終わり、完了の待ちは外）。出力バッファは、読み取りより長く使われる配列。

## 3. 是正（コミットを参照）

- `fixed` ブロックを、完了の待ちまで広げた。
- 待ちが失敗したときは、`CancelIoEx` で取り消し、`GetOverlappedResult(…, bWait: true)` で完了を待ってから、関数を抜ける（バッファ・`ov`・イベントを有効に保つ）。
- ソースの形のガードテスト: `K7b1HidOverlappedIoGuardTests`。

実機の I/O は単体テストできないため、効果は、再現するかどうかの実機の観察で確かめる（§5）。

## 4. 確認の手順（ユーザー作業）

ダンプは、WinDbg Preview（Microsoft Store）か `dotnet-dump` で開く。

1. WinDbg Preview で、ダンプ（`DS4Windows.exe.9212.dmp` から）を開く。
2. `!analyze -v` を実行し、`EXCEPTION_RECORD`、`FAULTING_IP`、`STACK_TEXT`（落ちたスレッドの呼び出しの並び）を貼る。
3. `.ecxr` の後に `k`、続けて `!clrstack`（SOS が使えるとき）を貼る。
4. 7 個のうち、少なくとも 3 個で同じ作業をして、落ちた場所が毎回違うか（ヒープの破壊の特徴）を確認する。

ダンプが落ちた場所の例:
- `ReadFile`／`HidDevice` の中、または、その直後に配列を使う `DS4Device.performDs4Input` → 上の原因と整合。
- GC の最中（`gc_heap::`、`coreclr!` の GC 関連の関数）→ ヒープの破壊の結果。原因は別の場所の可能性が高い。

## 5. 是正後の実機確認

- 是正後のビルドで、これまで落ちていた操作（コントローラーを接続したまま、ColorPicker／Controller Readings を繰り返し開閉する、プロファイルの切替、長時間の使用）を行い、**3 日間ほど**、ダンプが増えないかを見る。
- 落ちなければ、K7b-1 の原因を特定できたとして閉じる（根拠: 是正の前後の発生頻度の比較）。
- 落ち続けるなら、ダンプで落ちた場所を確認し、残りの候補（`DS4Device` の `fixed` ブロック［`performDs4Input`、`PrepareOutputReportInner`］、`DS4SixAxis`、`HidHideAPIDevice`、`DS4OutDeviceExtras` の `GCHandle` の使い方）を、順に調べる。

## 6. ダンプの解析結果（2026-10-23。`DS4Windows.exe.9212.dmp`）

- 落ちた場所: GC のマーク処理（`coreclr!SVR::gc_heap::mark_object_simple1`）。型情報が null のオブジェクトを踏んだ（`0xc0000005`）。
- `!verifyheap`: 481,611 個中、エラー 5 件。古い世代（segment 5）の約 440 バイトに集中。4 個のオブジェクトが、別の領域（heap 12 の第 2 世代）の、壊れたアドレスを指す。指す先の 1 つは、型情報が `1`。
- **決定的な証拠**: 壊れたアドレス（`236a1442018`〜`236a1442067`）の生のバイト列が、**DS4 の Bluetooth 入力レポート（レポート ID 0x11）**だった。
  - `11 c0 00 7f 80 a5 56 08 00 78 …`（`11`＝レポート ID、`c0`＝ヘッダー、`7f 80 a5 56`＝スティック、`08`＝十字キーの中立値）
  - `80 00 00 00` の繰り返し（タッチパッドの、指が触れていない状態）
  - 末尾の 4 バイト（`6c 18 e8 cf`）＝ CRC32
- つまり、**HID の読み取り（カーネル）が、入力レポートを、GC ヒープの別のオブジェクトの上に書き込んだ**。読み取りのバッファ（`DS4Device.btInputReport`／`inputReport`）が、保留中の非同期 I/O の間に GC によって移動され、カーネルが古い位置へ書き込んだ、という §2.1 の原因と一致する。
- Server GC（`DS4WinWPF.csproj` の `ServerGarbageCollection`＝true。32 ヒープ）は、元からの設定。ヒープが多く、GC による移動の機会が多いため、症状が出やすい。設定は変えない（是正は `HidDevice` 側）。

結論: **K7b-1 の原因を特定した**（`HidDevice.ReadFile` の、保留中の読み取りのバッファが固定されていない）。是正は 7033089。是正後の実機確認（§5）で、ダンプが増えないことを確かめる。

## 7. 7 個のダンプの分類（2026-10-23）

| ダンプ | 日時（日本時間） | 種別 | 原因 | 状況 |
|---|---|---|---|---|
| 9212 | 10-23 5:29 | GC のマーク処理で落ちた（ヒープ破壊） | HID の読み取りが、移動した配列の古い位置へ書き込んだ（§6） | 7033089 で是正 |
| 15576 | 10-22 4:37 | 同上（`mark_object_simple1`、静的参照の列挙） | 同上と推定 | 7033089 で是正（様子見） |
| 34332 | 10-22 4:40 | 同上（`find_first_object`、カードのマーキング） | 同上と推定 | 7033089 で是正（様子見） |
| 17488 | 10-22 20:24 | `InvalidOperationException`（`Application.Windows` を、UI でないスレッドから） | **トレイの診断用の一時ログ `TrayDiag`**（5b9a3a0 で追加、84fb2fd で削除）が、コントローラーの切断（入力スレッド）で呼ばれていた | **解消済み**（診断コードは削除済み。ダンプは、診断を入れたビルドのもの） |
| 43304 | 10-22 20:18 | 同上 | 同上 | **解消済み** |
| 28824 | 10-21 12:42 | `NullReferenceException`（`ProfileEditor.GyroCalibration_Click`） | コントローラー未接続のスロットで、Gyro の校正ボタン。`DS4Controllers[slot]` が null | **是正**（null の確認を追加。`GyroCalibrationNullDeviceGuardTests`） |
| 57596 | 10-21 11:10 | `NullReferenceException`（`DS4Device.performDs4Input`、入力スレッド） | 未特定（落ちた行の特定には、`!pe` と `!clrstack -a` が要る） | 調査中 |

ヒープ破壊の 3 件（約 4 割）と、診断コードの 2 件、Gyro の校正 1 件が分かり、残りは `performDs4Input` の NullReferenceException 1 件。
