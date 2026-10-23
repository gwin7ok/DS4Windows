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
