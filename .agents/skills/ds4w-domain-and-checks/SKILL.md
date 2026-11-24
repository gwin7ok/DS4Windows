---
name: ds4w-domain-and-checks
description: DS4Windows 固有の画面名・日本語用語・既知不具合台帳（KI-xx）・実機検証の制約と確認仕様を扱うときに参照する
---

# ドメイン用語・既知不具合・実機検証スキル

## 1. 画面呼称と日本語用語の厳格な統一
- **画面の呼び名**:
  - Controls / SpecialActions の種類（Key / Launch Program / Macro 等）を選ぶ編集画面 → **割り当て編集画面**（`AssignmentEditor` / 旧 `SpecialActionEditor`）
  - 上記内の「Output Key/Mouse/Button」メニューから開くキー選択ウィンドウのみ → **出力ピッカー**（`BindingWindow`）
  ※両者を取り違えない。曖昧なときは推測せず確認する。
- **Special Action の訳語**: 画面・計画書・文書・コメントすべてで **「スペシャルアクション」** を使う（「特殊アクション」は禁止）。
- **用語集正本**: `docs-forDIMG/MadeByAgent/PhaseX3-Glossary.md`。製品名・キー名・単位・軸略号は英語のまま（日本語 `.resx` に行を書かない）。列挙の表示名は訳すが保存値は変えない。
- **入力・出力の仕様補足**: 「連打モード」という機能は無くキー自動リピートのみ存在する。キー入力方式（FakerInput / SendInput）は自動選択で UI 切替はない。

## 2. 既知不具合台帳（`Known-Issues-Ledger.md`）の扱い
- 新しい行を足すときは既存の最大 ID の次を使い、解消済み行と番号が衝突しないよう確認する（KI-20〜22 は改番済み）。
- **既知不具合・未解決として提示禁止（2026-11-16 指示）**:
  - **KI-7**（割り当て編集画面の画像とボタン名が全機種で DS4 表示）と **KI-14**（カスタム実行ファイル名）は PhaseX 計画書（`PhaseX-Input-Device-Visuals-Plan.md`、`PhaseX4-Custom-Exe-Name-Plan.md`）に計画済みのため、残課題一覧として挙げない（聞かれたときだけ説明）。
- **誤認防止**:
  - **KI-24**（マルチフィンガータッチ押下の右クリック瞬間切れ）：再確認で非再現のため確定バグ扱いしない。
  - **KI-25**（トレイ右クリックメニュー即閉じ）：インストール版 4.3.2 にもある既存挙動で RV 変更起因ではない。
  - **KI-2**（PS+Down 切断時の Alt+Tab 誤表示）：再現条件待ちで保留中。再報告時は該当ログとマクロ SA の残存・`Mapping.HandleDeviceDisconnect` を確認する。

## 3. 実機確認とハードウェア制約
- **手元実機**: 基本的に **DS4（1台）と Xbox 入力** のみ検証可能。DualSense・Switch Pro・UDP (Cemuhook) クライアントは実機がない前提で扱う。
- **実機未確認のまま残っている主な仕様変更（RV40 等）**:
  - DualSense バッテリー計算（`cap*10+5`、byte 53 上位ニブル）と Bluetooth 出力フレーミング（`0x10` タグ位置修正）。
  - Switch Pro の振動エンコーディング（`0x60` vs `0x40`）や BT 動作・ファーム差異。
  - プロファイルインポート 3 択ダイアログ（同名上書き / キャンセル / 別名新 ID）と上書き時ホットリロードの実機動作。
- **確定仕様**:
  - DS4 バッテリーは 0–8 段階（12.5% 刻み、充電中ジャンプあり）。
  - Xbox BLE の Bluetooth 切断は Windows 側制約により非対応（`SupportsBluetoothDisconnect == false`、KI-6）。
  - Xbox の自動停止なし（KI-22）は仕様確定。
  - CLI 実行は `-command Name.arg.arg`（ドット区切り）。対話 cmd からの待ち合わせは `start "" /wait DS4Windows.exe -command ...`。