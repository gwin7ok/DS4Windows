# PhaseRV17 計画書: 初回画面の導入の流れと、起動の組み立ての整理

作成日: 2026-11-21
状態: **完了（2026-11-21）。D1＝A、D2＝A、D3＝RV17 の後にまとめて 1 回、D4＝A。`ExternalComponentInstaller`・`PostHostServices` を作った。`ResolvePostHostService` 23→9。Pure DI 化 A−→A、共有部品化 A−→A。実機確認は Device-Check-Checklist の RV17（初回画面の導入 3 本、起動と終了）**
出典: `Score-CodeReview.py` の現在の指標（RV16 完了時点）、`WelcomeDialog.Installers.cs`・`App*.cs` の読み取り。
動き: **挙動・ログ・保存の形は変えない**。

## 目的
点数の低い指標のうち、RV15・RV16 で扱っていない**具体的な指標**を片づける。

| 指標（現在値） | 点 | 内容 |
|---|---|---|
| 外部コンポーネントの取得・導入の流れが 1 つの部品に集約されている: いいえ | **0.00** | `WelcomeDialog.Installers.cs`（338 行）に、ViGEm・HidHide・FakerInput の同型の 3 本（ダウンロード → 起動 → 完了の監視）がある |
| `App.xaml.cs` の `ResolvePostHostService` 23 | 0.74 | 起動の段階で、サービスを 1 つずつ引いて、`App` の private フィールドへ入れている |
| 500 行以上の型 30 | 0.71 | `App` 983、`WelcomeDialog` 567 を含む |

見送るもの（理由）:
- 「実装 1・偽物なしのインターフェース 75」は、規約 3.1 により、**同じ層の中の計算の部品は具象でよく、既存のものはこの基準だけでは撤去しない**ため、触らない。
- 「`.Result`・`.Wait()`・`Thread.Sleep` 34」の主な場所は、Switch Pro の起動手順（`SwitchProSession` 12・`SwitchProCommandChannel` 5。機器の応答待ち）で、実機でしか確かめられない。
- 「公開の可変フィールド 623」は、多くがパケット・入力状態の構造体（`PadDataRspPacket` 47、`ControllerState` 31 ほか）で、ワイヤー形式の定義。

## 読んで分かった実態
- `WelcomeDialog` は、3 つの導入（ViGEm・HidHide・FakerInput）が、同じ形の処理を 3 回書いている: 一時ファイルの削除 → ダウンロード（進捗の表示）→ 完了したら導入プログラムを起動 → タイマーで終了を監視 → 導入の確認（`IDriverEnvironment`）→ 画面の文言を更新。違いは、ファイル名・URL・確認の関数・ボタン・確認の細部だけ。
- 状態（`monitorProc`・`monitorTimer`）は、画面のフィールド。部品へ分けると、**ダウンロードと終了の監視を単体で試験できる**（偽の `HttpClient` のハンドラーと偽のプロセスで）。実機がなくても、振る舞いを固定できる。
- `App` は、起動の段階ごとに、`ResolvePostHostService<T>()` を 12 回前後（`App.Bootstrap.cs`）呼び、結果を private フィールドに持つ。起動後の処理が、それらを使う。

## 決定してほしい事項

### D1 導入の流れ（WelcomeDialog）
- **(A) `ExternalComponentInstaller` を作る（推奨）**: 状態を持つ部品（ダウンロード → 起動 → 終了の監視。時計は `TimeProvider`、プロセスの起動は抽象）。3 本は、定義（`ExternalComponentSpec`: ファイル名・URL・導入の確認・文言のキー）の違いだけにする。画面は、進捗と結果を表示するだけ。特性化テストを先に書く。
- (B) 3 本の共通部分だけを、画面内の private メソッドへ寄せる（型は増えない。指標は上がらない可能性）。
- (C) やらない。

### D2 起動の組み立て（App）
- **(A) 起動後の処理が使うサービスを、1 つの「起動後のサービス束」（`PostHostServices`）として、1 回で解決する（推奨）**: `ResolvePostHostService` の呼び出しを 23→数件へ。束は、コンテナで登録する値の入れ物（`ServiceRegistration.Ui`）。`App` のフィールドは、束のプロパティの委譲へ。
  - ただし、**何でも箱にしない**（RV14 の原則）: 起動の段階（ブートストラップ・初回起動・終了）ごとに別の束にする案を、Step 0 で検討する。
- (B) 触らない（`App` は、起動の順序が厳密で、挙動の変更が実機でしか見えないため）。

### D3 実機確認の順序
- **(A) RV17 の前に、後回しの実機確認（§9d〜§9l）を先に行う（推奨）**: RV13〜RV16 の分が積み上がっている。RV17 は、初回画面と起動に触れるため、先に現状が正しいことを確かめておくと、RV17 の確認を差分だけにできる。
- (B) RV17 の後にまとめて 1 回。

### D4 範囲
- **(A) D1（A）と D2（A）の両方（推奨）**。
- (B) D1 のみ（指標 0.00→1.00 だけを狙う。リスクが小さい）。

## Step（D1＝A、D2＝A の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV17-0 | `WelcomeDialog.Installers.cs`・`App*.cs` を読み、3 本の違い・起動の段階の表を作る（`PhaseRV17-Survey.md`）。承認 | 記録のみ |
| RV17-1 | `ExternalComponentSpec`・`ExternalComponentInstaller` の特性化テスト（偽の `HttpClient`・偽のプロセス・時計）。ViGEm の流れから置き換え | Debug ALL（`Run-DebugAll.ps1`） |
| RV17-2 | HidHide・FakerInput を同じ部品へ（確認の細部の違いは `Spec` へ） | 同上 |
| RV17-3 | `PostHostServices`（起動の段階ごと）。`App` の `ResolvePostHostService` を減らす | 同上、`RemovedApiRuleTests`・層の規則 |
| RV17-4 | 再採点（`RV17.json`）、`02-Scorecard.md`、計画書の完了記録、`Device-Check-Checklist.md`（初回画面の導入の 3 本・起動） | `ModelDiagramGuardTests` |

ブランチは `PhaseRV17`。コミットは Step ごと、完了で `--no-ff` マージ（push なし）。

## 予想（03-Rubric.md §5）
- 「導入の流れの集約」0.00→1.00（採用手段・共有部品化の観点に効く）。
- `ResolvePostHostService` 23→数件（0.74→約 0.95）。
- 500 行以上の型: `WelcomeDialog` 567→約 330（500 未満）。`App` 983→約 900（変わらず）。30→29。
- 等級の変化: 共有部品化 A−（84.9%）が **約 87%**（A− のまま、境界に近づく）、採用手段 A（92.2%）が約 93%。冗長処理 A−（88.8%）が約 90%（**A への昇格の可能性**）。

## リスク
- 導入の流れは、実際にドライバーを入れる経路で、実機（管理者・ネットワーク）でしか完全には確かめられない。**特性化テストで、ダウンロード・起動・監視・確認の順序と、失敗のときの画面の文言を固定する**。実機では、導入済みの環境で、導入ボタンを押す確認（キャンセルできる段階まで）を行う。
- 既存の文言（`Properties.Resources` の `Downloading`・`Installing`・`InstallComplete`・`InstallFailed`）は変えない。
- 起動の組み立て（D2）は、順序依存が強い。`PostHostServices` は、解決の**順序を変えない**（現在の呼び出しの順に、束の生成で解決する）。変えると、ホストの構築と初期化の順序がずれうる。

## 実機確認
`Device-Check-Checklist.md` に追記: 初回画面（WelcomeDialog）の 3 本の導入ボタン（導入済み環境で、起動する導入プログラムまで）、起動と終了（トレイ・設定の保存）。

## 次の Phase（見込み）
RV18: 文字列を読むテストの置き換えの続き、または MVVM（コードビハインドの部品への直接代入 248）。画面の型は、実機確認が済んでから。
