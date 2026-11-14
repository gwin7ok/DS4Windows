# Phase38 計画書: `UdpServer`（Cemuhook のモーションサーバー）の、通信と書式の分離

作成日: 2026-11-12
状態: **38-0b（型の移動）と 38-0 の調査まで完了（2026-11-15）。38-1 以降は、パケットの黄金を作ってから行うため未着手**
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 3）、`02-Layers-and-Placement.md` の 2-4、`00-Summary.md` §6（`UdpMotionService` の毎フレームの経路は未調査）
前提: UDP の型 6 つを `Input/` から移す作業（旧 `Phase36-Plan.md` の 36-A2）を、本 Phase の 38-0b として引き取った（2026-11-13 統合）。同じ型に触れる作業を 1 か所で決めるため。

## 目的
`Output/UdpServer.cs`（882 行）を、(1) ソケットの送受信とクライアントの管理、(2) 受け取った要求の解釈、(3) 送るパケットの書式、に分ける。書式と解釈を、ソケットなしで単体テストできるようにする。送受信するバイト列は変えない。

## 現状（調査。2026-11-12 の実測）
- メソッド 17 個。長い順に、`ProcessIncoming` 150 行（受信した要求の解釈と応答）、`NewReportIncoming` 135 行（入力 1 フレームぶんを、登録済みのクライアントへ送る）、`ReportToBuffer` 117 行と `ReportToBufferDataRsp` 115 行（パケットの書式）、`CompletedSynchronousSocketEvent` 61 行、`Start` 58 行。
- 上流（Travis Nickles、GPL v3 のヘッダーつき）から、ほぼそのまま持ってきた型である。
- 窓口は Phase9（9-E E-3）で作ってある: `IUdpMotionService`（開始・停止、ハンドラー、スムージング）、`IUdpMotionServer`／`UdpMotionServerAdapter`、`IUdpMotionServerFactory`。中身（`UdpServer`）だけが上流のまま。
- パッドの情報の型（`DualShockPadMeta`、`PadDataRspPacket`、`DsState`、`DsBattery`、`DsConnection`、`DsModel`）は `Input/` にあり、使うのは UDP の側だけ（2-4）。
- 毎フレームの経路である（UDP サーバーが有効で、クライアントがいる間）。送信のたびに、パケットのバッファー（`new byte[…]`。`UdpServer.cs:193`）と、`SocketAsyncEventArgs`（同 `203`）を作っている箇所がある。**毎フレームかどうか、使い回しの仕組み（同 `55`〜`60` のバッファーの列）との関係は、未確認**（38-0 で確かめる）。
- テスト: `UdpServer` に触れるテストのファイルは 6。

## 方針（決定事項の案）
1. **送受信するバイト列は変えない**。Cemuhook の規格（DSU）に従うクライアント（エミュレーターなど）との互換が目的の機能であり、1 バイトの違いで動かなくなる。パケットの黄金（要求と応答、パッドの情報、モーションのデータ）を先に作る（38-0）。
2. 書式（パケットを組み立てる）と解釈（要求を読む）は、状態を持たない部品にし、`Span<byte>` で、割り当てなしに書く・読む。
3. ソケットとクライアントの管理は、`UdpServer` に残す（名前は変えない）。
4. 上流のファイルに近い形を保つ利点（上流の修正を取り込みやすい）と、分ける利点（テストできる）を、D1 で比べて決める。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢 |
|---|---|---|
| D1 | 実施するか | (A) 38-0（調査と黄金）だけ行い、毎フレームの割り当てなどの実害が見つかれば、続きを行う／(B) すべて行う／(C) 行わない（上流に近い形を保つ。その場合、38-0 の割り当ての確認だけを `Phase36-Plan.md` の C 部へ移す） |
| D2 | 分けた部品の置き場所 | (A) `Output/Udp/`（新しいフォルダー。`UdpServer` もここへ移す。規則 R1 の更新が要る）／(B) `Output/` の直下と `Output/Services` のまま |
| D3 | パッドの情報の型（2-4）の移動 | (A) 本 Phase で、分けた部品と一緒に置き場所を決める（38-0b。`Phase36-Plan.md` の 36-A2 から、この分を外した。2026-11-13 に統合済み）。本 Phase を行わない（D1 が C）ときは、36-A2 で `Core/Model` へ移す／(B) 見送る |

## 決定の記録（2026-11-13。ユーザーの回答）
D1＝A（まず 38-0 だけ。実害が見つかれば続き）、D2＝A（`Output/Udp/` を新設）、D3＝A（UDP の型の移動を 38-0b で行う）。
- D1＝A でも、38-0b（型の移動）は D2・D3 に従って行う。移し先は `Output/Udp/`。規則 R1 に `Output/Udp` を足す。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 38-0 | 調査と守りの準備: (1) 送信の経路（`NewReportIncoming` → `ReportToBuffer` → `SendPacket`）の、毎フレームの割り当てとロックを読む。`UdpMotionService` のハンドラーとスムージングも読む。(2) パケットの黄金を作る（バージョンの要求と応答、ポートの情報、パッドのデータの登録、モーションのデータ。CRC を含む）。(3) 上流との差分（このリポジトリで変えた箇所）を一覧にする | 黄金の追加 | 未着手 |
| 38-0b | （2026-11-13 統合。旧 36-A2）UDP の型 6 つ（`DualShockPadMeta`、`PadDataRspPacket`、`DsState`、`DsBattery`、`DsConnection`、`DsModel`）を `Input/` から移す。移し先は D2 の置き場所に合わせる（D2 が A なら `Output/Udp/`、B なら `Core/Model`）。`git mv` と名前空間の書き換えを別コミットに（規則 R7） | `NamespaceFolderGuardTests`、`LayerDependencyRuleTests` | 未着手 |
| 38-1 | 送るパケットの書式（`ReportToBuffer`、`ReportToBufferDataRsp`、ヘッダーと CRC）を、状態を持たない部品へ | 黄金が不変。割り当て 0 | 未着手 |
| 38-2 | 受け取った要求の解釈（`ProcessIncoming` のうち、読む部分）を、状態を持たない部品へ。応答の送信と、クライアントの登録は `UdpServer` に残す | 黄金が不変。壊れた要求（短い、CRC が合わない）の扱いを固定する | 未着手 |
| 38-3 | クライアントの管理（`clients` の辞書と、時刻による登録の切れ）を、小さな型へ。時刻は `TimeProvider` で受ける | 登録・更新・期限切れのテスト | 未着手 |
| 38-4 | 38-0 で見つかった、毎フレームの割り当てを直す（使い回しのバッファー、送信用のオブジェクトの使い回し） | 割り当て 0 | 未着手 |
| 38-5 | 文書: `01-Structure.md`、規則 R1（D2 が A のとき）、`Code-Review-2026-11/00-Summary.md` の未調査の行を更新 | `ModelDiagramGuardTests`、`NamespaceFolderGuardTests` | 未着手 |

## 確認の方法
- 各 Step のあと、パケットの黄金と、UDP のテスト。
- 実機: UDP サーバーを有効にし、Cemuhook に対応するクライアント（エミュレーターや、モーションの確認用のツール）で、コントローラーの検出と、モーション・タッチの値が届くこと。スムージングの有無の両方。複数のコントローラー。

## リスク
- 中。外部のクライアントとの互換が、パケットのバイト列で決まる。黄金が前提。
- 利用者が少ない機能と見られ、効果は限られる。D1 の (A)（調査から）を勧める。

## 履歴
- 2026-11-13: 決めること（D 項目）のユーザーの回答を「決定の記録」に記録した。
- 2026-11-13: Phase 番号の付け替えに伴い、旧 Phase36 の 36-A2（UDP の型の移動）を 38-0b として引き取った。
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。

## 実施の記録
### 2026-11-15
- 38-0b（完了）: `git mv` のコミットと、名前空間の書き換えのコミットを分けて行った（R7）。`UdpServer` は `Output/Udp/`（名前空間 `DS4Windows.Output.Udp`）へ。UDP の型 6 つ（`DualShockPadMeta`、`PadDataRspPacket`、`DsState`、`DsBattery`、`DsConnection`、`DsModel`）は、`Output/Udp/` ではなく **`Core/Model`** へ移した。理由: `Processing/Abstractions`（`IUdpMotionServer`、`UdpPadDetailProvider`）の契約が `DualShockPadMeta` を使うため、第 2 層が第 3 層の型を直接参照する形（依存の向きに反する）を避け、規則 R2「層をまたいで共有する値の型は `Core` に置く」に従った（決定 D3 の「分けた部品と一緒に置き場所を決める」の結論）。`LayerDependencyRuleTests` の基準値は不変。規則 R1 に `Output/Udp` を追記した。
- 38-0（調査。毎フレームの経路）: `NewReportIncoming`（入力 1 フレームごと）は、(1) クライアントの一覧用の `new List<IPEndPoint>`、(2) 期限切れ判定用の `new List<IPEndPoint>`、(3) `PadMacAddress.GetAddressBytes()`（配列の割り当て）、(4) **クライアントごとに `new SocketAsyncEventArgs`**（`args.Completed +=` のデリゲートも）を作る。`SendPacket`（要求への応答。毎フレームではない）も、`new byte[]` と `new SocketAsyncEventArgs` を作る。バッファー本体は使い回し（`dataBuffers`）だが、`SocketAsyncEventArgs` は使い回していない。クライアントがいる間は、1 フレームあたりクライアント数ぶんの `SocketAsyncEventArgs`（大きめのオブジェクト）が作られるため、実害はある（UDP サーバーが有効でクライアントがいるときだけ）。直す場合は 38-4（`SocketAsyncEventArgs` のプール化、一覧の再利用、`GetAddressBytes` の回避）。送信するバイト列は変えない。
- 次の作業: (1) パケットの黄金（要求と応答、パッドの情報、モーションのデータ、CRC）を先に作る。(2) 38-1（書式の部品化）・38-2（要求の解釈）・38-3（クライアント管理）・38-4（割り当ての削減）。外部のクライアントとの互換が前提のため、黄金が先。

### 2026-11-15（38-1・38-4）
- 黄金: `UdpPacketGoldenTests`（パッドのデータ［全入力あり／モーションなし］、バージョンの応答、ポート情報）を、旧実装から採取したバイト列で先に作った。
- 38-1（完了）: 書式を状態を持たない `DsuPacketFormatter`（`Output/Udp`）へ移した（ヘッダー、パッド情報、入力の 1 フレーム、CRC、構造体のコピー）。呼び出しのない旧い `ReportToBuffer`（117 行）は削除（規則 2.4: 正規の経路と重複するだけの並行実装）。黄金のバイト列は不変。`PadDataRspPacket` は、テストから呼ぶため `public` にした。
- **発見した欠陥と修正**: `CalculateBasicHash` が使う高速テーブルは、`Crc32Algorithm` を別の場所で作るまで空で、その状態で最初のパケットを作ると CRC が `FFFFFFFF`（不正）になる。`DsuPacketFormatter` の静的コンストラクターでテーブルを初期化して直した（実運用と同じ、テーブル初期化済みの CRC が黄金）。実機確認の項目に追加（台帳 §3）。
- 38-4（完了。一部）: `SocketAsyncEventArgs` を 80 個ぶん使い回す（毎フレーム・クライアントごとの `new` を廃止。コンストラクターで作って捨てていた 80 個も廃止）。クライアントの一覧は入力のスレッドごとに使い回し、削除用の一覧は必要なときだけ作る。構造体のコピーは箱に入れない形へ（`GCHandle` の割り当てを廃止）。MAC の `GetAddressBytes()` は、`TryWriteBytes` が対象の .NET にないため残した（6 バイトの割り当て）。送信は従来どおり 100 バイト固定（短いパケットの後ろは前の内容が残る。互換のため変更していない）。
- 38-2（要求の解釈）・38-3（クライアント管理の部品化）は、バイト列の挙動に影響がなく、効果が小さいため見送り（構造の整理のみ。必要になったら別 Step）。

### 2026-11-15（38-3）
- 38-3（完了）: クライアントの登録簿（要求の記録、5 秒の期限、死んだクライアントの除去）を `DsuClientRegistry`（`TimeProvider` を受ける）へ切り出し、時間を進めるテスト（`DsuClientRegistryTests` 5 件）を足した。`UdpServer` のコンストラクターは、省略可の `TimeProvider` を受ける。挙動は同じ（MAC が null のときに例外にならない点だけ安全側）。
- 38-2（要求の解釈の部品化）: 見送りのまま。`ProcessIncoming` は、バイト列の読み取りと応答の送信が密に絡み、切り出しても得るものが小さいため、必要になったら別 Step。
