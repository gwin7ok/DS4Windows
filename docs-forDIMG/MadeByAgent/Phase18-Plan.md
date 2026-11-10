# Phase18 計画書: DS4 を読み取りの骨組みへ載せ、DS4Device の基底を整理する

作成日: 2026-11-10
前提: `Phase17-5-Plan.md` §5。DualSense・Switch Pro・`ProtocolDevice` は `InputLoopDevice`（読み取りの骨組み）に載った。DS4 だけが、全機種の基底 `DS4Device`（約 2,000 行）の自前ループ（`performDs4Input`）のままである。

## 1. 目的と終わりの状態

- DS4 の読み取りループを、`InputLoopDevice` の差し込み口（`ReceiveFrame`／`IsExpectedReport`／`ProcessFrame`／`ProcessIdle`／`WriteOutputFrame`）で書く具象クラス `DualShock4Device` にする。
- `DS4Device` は、全機種が共有する基底として残す（外部の参照 約 330 件を変えない）。基底は抽象クラスになり、DS4 のループを持たない。DS4 だけの動作は、可能な範囲で `DualShock4Device` へ移す。
- 守るもの: 動作の完全維持（2.2）、ログの文言（2.3）。

## 2. 設計の決定

| 項目 | 決定 | 理由 |
|---|---|---|
| 型の名前 | 基底は `DS4Device` のまま、DS4 の具象を `DualShock4Device` とする | `DS4Device` 型の参照は約 330 件。名前を変えると差分が大きく、動作の確認が難しくなる。名前の整理は別の機械的な Step にできる |
| 基底のループ | `DS4Device.StartUpdate` が呼ぶ `performDs4Input` を、抽象メソッド `RunInputLoop` に置き換える。`InputLoopDevice` が `ReadInput` を実装する | 4 機種（DualSense、Switch Pro、`ProtocolDevice`、DS4）が同じ骨組みで動く |
| 骨組みの拡張 | (1) ループを即座に終える結果 `Terminate`（DS4 は切断時に `exitInputThread` を立てず、スレッドを抜ける）、(2) フレームの時刻を機種が決める `GetFrameTicks`（DS4 は読み取り直後の時刻を使う） | 旧実装の挙動を変えないため |

## 3. 段階

| 段階 | 内容 | 守り |
|---|---|---|
| 18-1 | 計画書。DS4 の BT（0x11 レポート＋CRC）・ドングル（SONYWA）の特性化テストを足す | 新しいテストが旧実装で合格する |
| 18-2 | 骨組みの拡張（`Terminate`、`GetFrameTicks`）。`DualShock4Device` を作り、ループを移す。基底を抽象にし、`new DS4Device` を `new DualShock4Device` に置き換える | USB・BT・ドングルの特性化テストが不変 |
| 18-3 | DS4 だけの動作（出力レポートの組み立て、校正、BT の補助スレッド）を、基底から `DualShock4Device` へ移せる範囲で移す | 全テスト、特性化テスト |
| 18-4 | 文書（モデル図 01・02、規則）、検査（基底がループを持たないこと）、基準値の更新 | |

## 4. リスク

- BT・ドングルの台本は、実機の挙動ではなく旧実装の挙動を固定するもの。実機のある DS4 での確認（BT 接続、切断、ライトバー）が必要。
- 基底から具象へ移す動作は、外部のクラスが `DS4Device` 型経由で呼ぶものがある。呼ばれるものは基底に仮想メソッドとして残す。

## 5. 進捗

- 2026-11-10: 計画書を作成。
- 2026-11-10 **18-1〜18-3 完了**: DS4 の BT（CRC つき）とドングルの特性化テストを追加。骨組みに `Terminate`（機種が切断の手順を済ませてループを終える）と `GetFrameTicks`（フレームの時刻を機種が決める）を足し、DS4 の読み取りを `DualShock4Device` に移した。`DS4Device` は抽象クラスになり、`performDs4Input` はなくなった。DS4 固有の初期化（`PostInit`）、校正、出力レポートの組み立て（`sendOutputReport`ほか）、DS4 の設定（コピーキャット）、BT の補助スレッドも `DualShock4Device` へ移した。`DS4Device` は約 1,650 行から約 1,150 行になった。全 3,378 件合格。
- 18-4: 文書（モデル図 01・02）と検査（`DeviceLoopStructureTests`: 基底が抽象で、全ての具象が骨組みに載ること）を更新した。
- **未了（実機）**: DS4 の USB／BT／ドングルでの接続・切断・ライトバー・振動の確認。BT・ドングルの特性化テストは旧実装の挙動を固定したもので、実機の挙動の保証ではない。
- **見送り**: 基底の名前 `DS4Device` の変更（参照 約 330 件。機械的な置換で別の Step にできる）。
