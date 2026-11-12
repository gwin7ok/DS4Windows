# コードレビュー（2026-11）追補: 外部レビュー（行数の多いファイル上位 20 件）の照合

作成日: 2026-11-12
対象: ユーザーから渡された外部のレビュー（行数・サイズの多い `.cs` 上位 20 件を 4 つに分類し、問題点と改善案を述べたもの。「MVVM パターンの形骸化」の解説を含む）。
方法: 指摘を 1 つずつ、`master`（コミット `070095f9` の時点のコード）で実測して確かめた。コードは変更していない。
結果の行き先: 既存の計画書（Phase26・30・33・34）への統合と、新しい計画書（Phase35〜38）。対応は §4。

## 1. 総評
- **方向は妥当**で、本レビュー（`00`〜`06`）や `Structure-Review-2026-11.md` と重なる指摘が多い。ただし、外部レビューは行数と一般的な型（MVVM、God Class）から述べており、すでに済んでいる分割（Phase9・15・16・25）を前提にしていない箇所がある。
- **例として挙がったコードは、実在しない説明用のもの**である（`SaveButton_Click`、`AppSettingsService.Instance.SaveProfile`、`gyroSettingsPanel` など）。このリポジトリに静的な `Instance` はなく、画面の生成も多くは工場（`IEditorWindowFactory`）を通る。問題の型（イベントで直接処理する、部品を名前で直接いじる、画面が画面を作る）そのものは、実測で確認できた（§2 の 2）。
- 新しく分かったのは、**画面のコマンドが 1 つも使われていない**こと（§2 の 2）と、**`TouchpadMouse` に 200〜300 行のメソッドが 3 つある**こと（§2 の 3）。この 2 つは、既存の計画に入っていなかった。

## 2. 指摘ごとの照合

### カテゴリ 1: 自動生成コード
| 指摘 | 結果 | 扱い |
|---|---|---|
| `Strings.Designer.cs`（4,239 行）、`Resources.Designer.cs`（1,677 行）は対応不要 | **そのとおり** | 対応しない。静的解析からの除外の設定（`.editorconfig` の `generated_code`）は、ルートに `.editorconfig` があるので、設定の有無だけ確かめる（`Phase34-Plan.md` の 34-A7） |

### カテゴリ 2: WPF の View（コードビハインド）
実測（XAML のイベント属性の数、コマンドの数、code-behind のメソッドの数、部品のプロパティへの直接の代入の数）:

| 画面 | `.xaml.cs` の行数 | XAML のイベント属性 | `Command="{…}"` | code-behind のメソッド（うちイベント処理らしい名前） | 部品への直接の代入 |
|---|---|---|---|---|---|
| `ProfileEditor` | 1,735 | 24 | 0 | 90（30） | 56 |
| `MainWindow` | 1,836 | 41 | 0 | 100（42） | 47 |
| `BindingWindow` | 950 | 6 | 0 | 20（10） | 23 |
| `RecordBox` | 900 | 26 | 0 | 48（25） | 32 |
| `AssignmentEditor` | 702 | 10 | 0 | 17（11） | 22 |

| 指摘 | 結果 | 補足 |
|---|---|---|
| 問題 1: `ICommand` を使わず、クリックのイベントで処理する | **そのとおり** | アプリ全体で、`Command="{…}"` はテーマのスクロールバーの 8 か所だけ。`ICommand` の実装（`RelayCommand` など）は 1 つもない |
| 問題 2: 結びつけを使わず、部品を名前で直接操作する | **一部そのとおり** | 結びつけは使っている（`MainWindow.xaml` に 113、`AssignmentEditor.xaml` に 52）。それでも、部品のプロパティへの直接の代入が、上の表のとおり残る |
| 問題 3: 画面が別の画面を直接作る | **一部そのとおり** | 編集の子画面（出力ピッカー、記録の窓など）は工場 `IEditorWindowFactory` を通る。直接作っているのは、`MainWindow` の `ChangelogWindow`・`ControllerRegisterOptionsWindow`・`RenameProfileWindow`・`UpdaterWindow`、各画面の `ColorPickerWindow`、ファイルのダイアログ、`MessageBox`。ダイアログの窓口（`IDialogService` に当たるもの）はない |
| `ProfileEditor` は、各タブの設定が同じ code-behind で処理される | **古い** | Phase15 で、8 つの領域（ジャイロ、タッチパッド、トリガー、スティック、ライトバー、出力、一般、6 軸）を、パネル（UserControl）とサブ ViewModel に分けた。残っているのは、Controls タブ（図とホバー）、特殊アクションの一覧、読み込み直し（`Reload` 96 行）、`SortSpecialActionsList`（105 行）、`EditActionBtn_Click`（91 行）など |
| ViewModel が値の入れ物になっている | **一部そのとおり** | Phase26 で、判断を部品へ出し始めた（`TouchpadSwipeProfileRule`、`RumbleTestController`、`UpdateCheckPolicy` など 5 つ）。大半はこれから |
| `App.xaml.cs` に、起動・単一起動・トレイ・例外処理・サービスの起動が集中 | **一部そのとおり** | トレイは `App` にない（`MainWindow` と `TrayAppearanceController`）。単一起動と IPC は Phase24 で部品へ出した。残るのは、起動の手順（`Application_Startup` 72 行ほか）、引数の処理（`CheckOptions` 73 行）、終了（`CleanShutdown` 61 行）、未処理例外 |
| `App.xaml.cs` は 100〜200 行にできる | **難しい** | 起動の手順は順序への依存が強く（`StartupOrderGuardTests`）、`App` は WPF の入口として、例外・セッション終了・電源のイベントを受ける。現実的な目標は、判断と手順を部品へ出して 400 行前後 |

### カテゴリ 3: 入力処理・デバイス制御・通信
| 指摘 | 結果 | 補足 |
|---|---|---|
| `TouchpadMouse`（1,810 行）は、複数の動作が 1 つの型で絡み合う | **そのとおり** | メソッド 30 個のうち、`TouchpadMouseStick` 297 行、`touchesEnded` 227 行、`SixMouseStick` 209 行、`TouchButtonCheckProcess` 134 行、`touchUnchanged` 122 行、`touchesMoved` 110 行。名前はタッチパッドだが、ジャイロの処理（マウス、マウススティック、トリガーの判定）も持つ。カーソル・ホイール・トラックボールは、すでに別の型（`MouseCursor`、`MouseWheel`、`FakeTrackball`） |
| `ITouchpadBehaviour` で動作ごとに分ける | **型はあるが、実装は 1 つ** | `Processing/Touchpad/ITouchpadBehaviour.cs` は、`TouchpadMouse` だけが実装する（`04-Duplication-and-Abstraction.md` の 4-6 の候補） |
| デバイスの型に、通信・解析・校正・電池・出力が混ざる | **一部そのとおり** | 解析は Phase16 で `Input/Parsers` へ、読み取りの流れは Phase18 で `InputLoopDevice` へ出した。残る大きな塊は、`DualSenseDevice.PrepareOutReport`（313 行。出力レポートの組み立て）、`SwitchProDevice.CalibrationData`（216 行。校正の読み出し）、`SetOperational`（76 行。初期化の手順） |
| 新しいコントローラー（Vader 4 Pro など）の追加が楽になる | **前提が違う** | Flydigi Vader 4 Pro のデバイスサポートは、承認のうえ廃止済み。単純な機種は、すでに `IDeviceProtocol`＋`ProtocolDevice` で足せる（Xbox が使う） |
| `InputDeviceCore`（1,148 行） | **大きいが、長いメソッドはない** | メソッド 65 個、最長 67 行。全機種の共通の状態とイベントの置き場。DS4 だけの分岐が 2 か所残る（`05-Extensibility.md` の 5-2） |
| `Mapping.cs` は巨大なスイッチ分岐 | **古い** | 869 行、メソッド 25 個、最長は `MapCustom` 124 行。種別ごとの分岐は、Phase8（統一トリガーディスパッチ）と Phase9 で部品へ出した |
| `UdpServer`（882 行）は、通信と書式が同居 | **そのとおり** | `ProcessIncoming` 150 行（受信の解釈）、`NewReportIncoming` 135 行、`ReportToBuffer` 117 行と `ReportToBufferDataRsp` 115 行（書式）。窓口（`IUdpMotionServer`、アダプター、工場）は Phase9 で作ってあるが、中身は上流のまま |

### カテゴリ 4: 状態保持・設定シリアライズ
| 指摘 | 結果 | 補足 |
|---|---|---|
| `BackingStore`（1,179 行）をサブモデルにまとめる | **進行中** | Phase25 で、スロットごとの設定を領域のオブジェクト（`GyroSlotSettings` など 8 つ）にした。残るのは、`ResetProfile`（140 行。各領域の既定値への戻しを 1 か所で書いている）、まだ配列のままの項目、読み込みのあとの処理 |
| `AppSettingsDTO`（1,126 行） | **大きいが、理由がある** | XML の形（`Profiles.xml`）を保つための入れ物で、コントローラー 8 台ぶんの同じ形の項目と、項目ごとの「書き出すかどうか」のメソッドが並ぶ。形を変えると、保存の互換に触れる |
| `SpecialActionsXmlSerializer`（689 行）を節に分ける | **そのとおり** | `LoadActions` 178 行、`SaveAction` 97 行、`SaveActionNew` 84 行。保存の入口が 2 つあり、種別を数（例: 6、11）で受ける |
| `TriggerDispatchEngine`（747 行）を、状態保持・シリアライズに分類 | **分類が違う** | 判定と実行のエンジン（第 2 層）。機器ごとのロックと、不変の配列の差し替えで作られており、問題は見つからなかった（`06-Performance.md` の良い点） |
| プロファイルの節（`ProfileSections`）の形を、特殊アクションとアプリの設定へ広げる | **妥当** | 特殊アクションは、種別ごとの組み立て部品（`ISpecialActionOutputBuilder` が 10 個。Phase28）の前例もある |

## 3. 外部レビューの優先順位について
外部レビューは、(1) `TouchpadMouse` の分割、(2) code-behind の削減、(3) デバイスの I/O とデータ処理の分離、の順を勧める。本レビューの見立ては次のとおり。
- 先に行うのは、**安全弁と毎フレームの割り当て（Phase29）**。`TouchpadMouse` の中にも、その対象（ジャイロのトリガーの判定の `Split`。6-10）がある。同じファイルを 2 回触らないよう、Phase29 のあとに分割する。
- code-behind の削減は、効果は大きいが、画面ごとの実機確認が要る。左右スティックの画面の一本化（Phase32）のあとに行うと、同じ XAML を 2 回作り直さずに済む。
- デバイスの分離は、DualSense・Switch Pro に実機がないため、差分テストの準備（Phase30 の 30-0）を共有できる Phase30 に統合する。

## 4. 対応（統合先と、新しい Phase）
| 外部レビューの項目 | 行き先 | 内容 |
|---|---|---|
| 自動生成コード | `Phase34-Plan.md` 34-A7（追加） | 静的解析からの除外の設定の確認だけ |
| code-behind（5 画面）、MVVM の 3 つの問題 | **`Phase35-Plan.md`（新設）** | コマンド、ダイアログの窓口、画面ごとの移行。Phase26 の残りを引き継ぐ |
| `App.xaml.cs` | `Phase33-Plan.md`（33-4 を拡張、33-4b を追加） | 起動後の処理と、引数の処理の切り出し。目標の行数を明記 |
| `TouchpadMouse` | **`Phase36-Plan.md`（新設）** | ジャイロの処理、タッチのマウススティック、タップとクリックの判定、タッチボタンを、別の型へ |
| デバイス（DualSense・Switch Pro）の出力レポートと校正 | `Phase30-Plan.md`（30-9〜30-11 を追加） | 出力レポートの組み立てと、校正の読み出しを、状態を持たない部品へ |
| `InputDeviceCore` | `Phase30-Plan.md`（既存の 30-4） | DS4 だけの分岐の解消。それ以上の分割は行わない |
| `Mapping.cs`、`TriggerDispatchEngine` | 対応しない | 指摘が古い、または分類が違う。`Mapping` の引数（15）は `03-Constructor-Arity.md` |
| `UdpServer` | **`Phase38-Plan.md`（新設）** | 通信と書式の分離 |
| `BackingStore`、`SpecialActionsXmlSerializer`、`AppSettingsDTO` | **`Phase37-Plan.md`（新設）** | 特殊アクションの保存・読み込みを種別ごとに、`ResetProfile` を領域へ、読み込みと保存の節を対に（5-5）。`AppSettingsDTO` は、形を保つ前提で判断から |
| `ControlBindingBuilder`（500 行。一覧の 17 位） | 対応しない | 最長のメソッドは `Add` 115 行。Controls の割り当ての区分ごとの解釈で、Phase8・10 で作った新しい型。外部レビューも指摘していない |

## 履歴
- 2026-11-12: 作成。コードは未変更。
