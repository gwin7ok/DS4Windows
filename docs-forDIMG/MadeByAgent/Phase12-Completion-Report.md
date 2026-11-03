# Phase12 完了報告書: 機能追加（ログ方針・ウィンドウのフォーカス・マクロのマウス移動）

作成日: 2026-11-03
対象: Phase12（Step1〜Step3）。リリース: **未定**（CHANGELOG の `[Unreleased]` に記載済み。版番号の決定と公開はユーザーが行う）
個別計画書: `Phase12-Step1-Plan.md`、`Phase12-Step2-Plan.md`、`Phase12-Step3-Plan.md`
関連: `Release-Backlog.md`（R-3・R-4）、`Log-Policy.md`、`Phase12-Step1-Log-Inventory.md`
進捗: `Phase12-Status.md`

---

## 1. 目的と結果

`Phase11-and-Beyond-Roadmap.md` の Phase12 のとおり、L1（ログの方針）→ R-4（Issue #3）→ R-3（Issue #2）を実施した。**計画した Step はすべて完了した。**

| Step | 内容 | 結果 |
|---|---|---|
| Step1（L1） | ログの方針の策定。`Log-Policy.md`、現状の実測 `Phase12-Step1-Log-Inventory.md`（666 か所・101 ファイル）、規約 §2.3 の改定（ログ整理の Phase に限り変更を許可。新規コードは最初から方針に沿う） | 完了。コード変更なし（一括整理は Phase13b） |
| Step2（R-4） | SA・Controls の割り当てに「ウィンドウのフォーカス」（`ActionTypeId.FocusWindow`）を追加 | 完了（2a〜2f、2h〜2j。**2g は見送り**）。DS4 実機で確認 |
| Step3（R-3） | マクロにマウスポインターの移動（絶対／ウィンドウ基準）、記録画面での記録・数値編集 | 完了（3a〜3f）。DS4 実機で確認 |

## 2. Step2（R-4）の内容

| 小 Step | 内容 |
|---|---|
| 2a | 仕様の決定と OSS の前面化の手法の比較（計画書 §5・§5b） |
| 2b-1 | 照合の部品 `WindowTargetMatcher` を `AutoProfileEntity` から取り出した（旧実装との差なし） |
| 2b-2 | `IWindowFocuser`／`IWindowSystem`（`DS4Windows/DI/`）、`WindowFocuser`／`Win32WindowSystem`（`DS4Control/Services/`）。ウィンドウの列挙と前面化 |
| 2c | 種別 `ActionTypeId.FocusWindow`（enum の末尾に追加）の保存・読込。古い版との互換を保つ |
| 2d | 実行（`FocusWindowAction`）。Fresh-press ガードあり |
| 2e | 編集画面、新種別の ViewModel、設定の部品 `WindowTargetEditor`、翻訳 |
| 2f | DS4 実機での確認（SA・Controls、最小化からの復元、対象なしで落ちない） |
| 2h | ウインドウ名の一致方法の一覧（`WindowTitleMatchMode`）と、実行ファイル名のみで判定するボタン |
| 2i | ターゲットマークのドラッグ＆ドロップで対象を取得 |
| 2j | フォーカスのあと、ウィンドウの中央へマウスポインターを移動するチェックボックス（`SpecialAction.centerCursor`。黄金ファイルを更新） |
| 2g | **見送り（ユーザー判断）**: Auto-Profile 画面を `WindowTargetEditor` へ置き換えない |

## 3. Step3（R-3）の内容と決定事項

| ID | 決定 | 結果 |
|---|---|---|
| D1 | 値の体系: 1 ステップ＝整数 1 個のまま。`1,300,000,000 + モード × 2^28 + (X+オフセット) × 2^14 + (Y+オフセット)`。絶対は −4,096〜12,287、ウィンドウ基準は −8,192〜8,191（範囲外は端に丸める）。保存形式（XML）は変えない | 計画どおり（`MacroPointerMove`） |
| D2 | ウィンドウ基準の原点は、前面ウィンドウの外枠の左上。前面なし・最小化のときは移動しない | 計画どおり |
| D3 | 記録は「Record mouse movement」（既定オフ）。`WH_MOUSE_LL` の低レベルフック。自分のウィンドウの上は記録しない | 計画どおり（`IMouseInputHook`／`LowLevelMouseHook`／`MacroPointerRecorder`） |
| D4 | 5 px 以上かつ 20 ms 以上で間引き。ボタンの直前に位置を入れる。1 回の上限 2,000 ステップ | 計画どおり |
| D5 | 移動は `SetCursorPos`。効かない場合のみ `SendInput` へ | 実機で動作を確認したため、切り替え不要 |
| D6 | 失敗は例外にせず Debug ログのみ（`[MacroMove]`） | 計画どおり |

小 Step: 3b（値の体系）、3c（実行。`MacroStepExecutor` が `IWindowSystem` を受ける）、3d（Add Mouse Move と数値編集）、3e（記録）、3f（文書）。

追加要望（ユーザー）として、記録画面のコマンド一覧の編集（Ctrl／Shift による複数選択、Delete での一括削除、ドラッグ＆ドロップの並べ替え、選択がないときは末尾に追加。計画書 §10）と、右側ボタンの並び順の変更・Add Mouse Move が基準の一覧の選択に合わせる変更（計画書 §11）を実装した。

- **古い版との互換**: マウス移動を含むマクロを古い版に戻すと、移動の値はライトバーの色として解釈される（色が崩れるだけ）。
- **不具合と修正**: 3d で、編集部品の ComboBox の SelectionChanged が一覧のハンドラへ伝わり、編集部品の差し替えでクラッシュした。一覧自身の選択の変更だけを扱うよう直した。
- **差分テストの更新**: `Phase9CMacroStepExecutorDifferentialTests` のライトバーの値を、1,300,000,000 未満（最大 1,255,255,255）に改めた（既存の値の動作は変わらない）。

## 4. 規約の順守

- **1 ファイル 1 型・ファイル名＝型名**: 新規の型（`WindowTargetMatcher`、`IWindowFocuser`、`MacroPointerMove`、`MacroPointerMoveMode`、`IMouseInputHook`、`LowLevelMouseHook`、`MacroPointerRecorder` ほか）は個別のファイル。
- **配置（R1・R2）**: インターフェースは `DS4Windows/DI/`、実装は `DS4Control/Services/`。
- **Pure DI**: `AppHost.Services` を持ち込まない。`IWindowFocuser`、`IMouseInputHook` は `ServiceRegistration` に登録し、コンストラクターで受ける。ラチェットの基準値は増やしていない。
- **ログ**: 新規のログは `Log-Policy.md` に沿う。既存のログは変えていない（Phase13b）。

## 5. 検証

- `Debug ALL`: 警告 0・エラー 0。DS4WindowsTests 3,043 件、StandaloneTests 11 件がすべて合格（Phase11 の完了時は 2,837 件）。
- 実機確認（DS4）: Step2 は 2f・2h・2i・2j、Step3 は 3d・3e・一覧の編集・ボタンの並び順を確認し、すべて合格。ゲームでの効き方（`SetCursorPos` を拾わないゲーム）は未確認だが、`SendInput` への切り替え手段を残してある（D5）。

## 6. 残した項目・後続の作業

- **リリース**: 版番号（4.3.0 など）の決定、`Directory.Build.props` の更新、Release ビルド、GitHub への公開、Issue #2・#3 のクローズ（ユーザーが行う）。CHANGELOG の `[Unreleased]` は記入済み。
- **先送り**（`Release-Backlog.md` §6）: S9-17（電池表示の定期更新の実機確認）、最終の Release ビルドの確認。
- 2g（Auto-Profile 画面の置き換え）は見送り。
- ログの一括整理は Phase13b。`ControlService` の整理とトリガー系 4 クラスの引数整理は Phase13。
- Issue #2 の本文は、トークンの権限が足りず取得できなかった。台帳のタイトルを正として実装した。
