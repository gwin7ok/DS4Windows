# PhaseX3 用語集（案）: 日本語表示の翻訳

作成日: 2026-11-16
状態: **承認済み（2026-11-16。利用者が「案を承認して自動で進める」を選んだ。X3-2 完了）**。「Special Action」は **スペシャルアクション**に統一した（アプリ・計画書・ドキュメントの全体を置換済み）
関連: `PhaseX3-Localization-Plan.md`

## 使い方
- 承認した用語を、日本語の翻訳（X3-3）で必ず使う。承認後に用語を変えるときは、この表を先に直す。
- 表の左は英語（画面に出ている原文）、右は日本語。「既存」は、`Strings.ja.resx`・`Resources.ja.resx` に、すでにある訳（短い文言 175 件。自動で抜き出した）。

## 割れていた用語の決定
| 用語 | 既存の訳の例 | 案 |
|---|---|---|
| Special Action | 「スペシャルアクション」（既存のリソース）／「スペシャルアクション」（この計画書群・コードのコメント） | **スペシャルアクション**に統一（画面の文言の既存の「スペシャルアクション」も直す）。**要承認** |
| Light（振動の弱） | 「弱振動」（Test Light）／テーマの「ライト」 | 振動は「弱」、テーマは「ライト」と、文脈で分ける |
| Dynamic／Permanent | 既存は「予約（Dynamic／固定の種類）」 | 「動的」「固定」へ。見出しは「予約（動的／固定の種類）」（列挙の表示は訳す、で承認済み。保存する値は変えない） |

## B. 新しく決める用語（案）
### 仮想コントローラーと出力スロット
| 英語 | 日本語（案） |
|---|---|
| Output Slots | 出力スロット |
| Plug／Unplug | 接続／取り外し |
| Accept | 確定 |
| Virtual Controller | 仮想コントローラー |
| Emulated Controller | エミュレートするコントローラー |
| Dynamic／Permanent | 動的／固定 |
| Requested／Current | 予約／現在の種類 |
| Disable virtual controller | 仮想コントローラーを無効にする |
| Passthru | パススルー |

### スティック・トリガー
| 英語 | 日本語（案） |
|---|---|
| Dead Zone／Max Zone／Anti-dead Zone | デッドゾーン／最大ゾーン／アンチデッドゾーン |
| Axial／Radial | 軸ごと／円形 |
| Output Curve／Output Mode | 出力カーブ／出力モード |
| Linear／Quadratic／Cubic | リニア／2 次／3 次 |
| Easeout Quad／Easeout Cubic | イーズアウト（2 次）／イーズアウト（3 次） |
| Enhanced Precision | 高精度（Enhanced Precision） |
| Sensitivity／Rotation／Fuzz | 感度／回転／ファズ |
| Anti Snapback／Outer Bind | アンチスナップバック／アウターバインド |
| Delta Acceleration／Flick Stick／Square Stick | デルタ加速／フリックスティック／スクエアスティック |
| Max Output／Max Travel／Min Travel | 最大出力／最大移動量／最小移動量 |
| Trigger Effect／Two Stage Mode | トリガー効果／2 段モード |
| Analog／Buttons／Default | アナログ／ボタン／既定 |

### ジャイロ・タッチパッド
| 英語 | 日本語（案） |
|---|---|
| Gyro／Touchpad／Trackball | ジャイロ／タッチパッド／トラックボール |
| Yaw／Roll／Pitch | ヨー／ロール／ピッチ |
| Tilt Up／Down／Left／Right | 上／下／左／右に傾ける |
| Jitter Compensation／Min Threshold | ジッター補正／最小のしきい値 |
| Smoothing／Weighted Average | スムージング／加重平均 |
| Invert／Toggle／Triggers | 反転／トグル／トリガー |
| Eval Cond.（And／Or） | 条件の評価（かつ／または） |
| Mouse Joystick／Mouse Acceleration | マウススティック／マウスの加速 |

### 割り当て・マクロ・スペシャルアクション
| 英語 | 日本語（案） |
|---|---|
| Macro／Record／Insert Wait | マクロ／記録／待ち時間を挿入 |
| Repeat While Held／Play Once／Repeat (Toggle) | 押している間繰り返す／1 回だけ再生／繰り返す（トグル） |
| Hold for／Tap Trigger／Hold Trigger／Double Tap Trigger | 長押し／タップのトリガー／長押しのトリガー／ダブルタップのトリガー |
| Unload Trigger | 解除のトリガー |
| Launch a program／Load a profile／Check Battery Life | プログラムを起動／プロファイルを読み込む／電池残量を確認 |
| Disconnect from Bluetooth／Calibrate gyro | Bluetooth を切断／ジャイロを校正 |
| Output Key/Mouse/Button | キー・マウス・ボタンを出力 |
| Unbound／Scan Code | 未割り当て／スキャンコード |

### 設定・その他
| 英語 | 日本語（案） |
|---|---|
| Lightbar Mode／Solid Colour／Rainbow | ライトバーのモード／単色／虹色 |
| App Theme／Dark／Light／Black／White | アプリのテーマ／ダーク／ライト／黒／白 |
| Notification／Log + Notification | 通知／ログ＋通知 |
| Process priority | プロセスの優先度 |
| Idle Disconnect／Poll Rate | アイドル時の切断／ポーリングレート |
| Days／Hours | 日／時間 |
| Program／Task／Run As | プログラム／タスク／実行方法 |
| Utils／Extras／Credits／License | ユーティリティ／その他／クレジット／ライセンス |
| 「Special Action」を含む文言 | 「スペシャルアクション」を使う（例: スペシャルアクションを編集、スペシャルアクションのエクスポート） |

## C. 英語のまま残す語（案）
- 製品名・機種名・技術名: DS4、DualShock 4、DualSense、Switch Pro、Xbox 360、Xbox、Bluetooth、USB、HID、HidHide、HidGuardian、ViGEmBus、FakerInput、OSC、UDP、Cemuhook、DSU、XInput、Steam、1€ Filter（`1€ MinCutoff`、`1€ Beta` を含む）。
- キーボードのキー名（Esc、Tab、Caps、Enter、LShift、LCtrl など）と、軸・ボタンの略号（LX、LY、RX、RY、LS、RS、L2、R2、DPad）。
- 単位: ms、Hz、%、°、units（「units」は「単位」ではなく、スティックの座標の単位のため、原文のまま）。
- 設定ファイルの値・列挙の名前: `Trace`／`Debug` などのログレベル、`Dynamic`／`Permanent` の保存値（**保存する値は変えない。表示だけ訳す**）。

## A. 既存の訳（自動で抜き出し。この訳を踏襲する）
| 英語 | 日本語（既存） |
|---|---|
| Accel | 加速度 |
| Action | アクション |
| Add Directory | ディレクトリを追加 |
| Add Mouse Move | マウス移動を追加 |
| Add Programs | プログラムを追加 |
| Add Steam Games | Steamゲームを追加 |
| Administrator permission required | 管理者権限が必要です |
| All | すべて |
| Apply | 適用 |
| Auto Profiles | 自動プロファイル |
| Battery | バッテリー ／ バッテリー残量 |
| Bluetooth Settings | Bluetooth設定 |
| Browse | 参照 |
| Cancel | キャンセル |
| Changelog | 更新履歴 |
| Charge the battery | バッテリーを充電 |
| Charging | 充電 |
| Check Battery | バッテリー残量を確認 |
| Clear | クリア |
| Close | 閉じる |
| Close Minimizes | 閉じるボタンで最小化 |
| Closed UDP server | UDP サーバーを閉じました |
| Color | 色 ／ 色を指定 |
| Confirm... | 確認... |
| Contains | 部分一致 |
| Control Panel | コントロールパネル |
| Controller 1 | コントローラー1 |
| Controller 2 | コントローラー2 |
| Controller 3 | コントローラー3 |
| Controller 4 | コントローラー4 |
| Controller 5 | コントローラー5 |
| Controller 6 | コントローラー6 |
| Controller 7 | コントローラー7 |
| Controller 8 | コントローラー8 |
| Controller/Driver Setup | コントローラー/ドライバーのセットアップ |
| Controllers | コントローラー |
| Current Type | 現在の種類 |
| Custom Exe Name | カスタム実行ファイル名 |
| Delete | 削除 |
| Delete Profile? | プロファイルを削除しますか? |
| Description | 説明 |
| Device Support | デバイスサポート |
| Disconnect BT | Bluetoothを切断 |
| DS4Windows Update Available! | DS4Windows の更新を利用可能です! |
| Duplicate | 複製 |
| Edit | 編集 |
| Edit Action | アクションを編集 |
| Empty | 空の色 |
| Ends with | 後方一致 |
| Exact match | 完全一致 |
| Exclusive Access | 排他アクセス |
| Export | エクスポート |
| Fall Back | フォールバック |
| First Launch | 初回起動 |
| Flash at | 点滅（この残量以下） |
| Flick Threshold | フリックのしきい値 |
| Flick Time | フリックの時間 |
| Focus a window | ウインドウを前面に出す |
| Found Controller: | コントローラーを検出しました: |
| Full | 満タン ／ 満充電 |
| Gamepad | ゲームパッド |
| Gamepad Test | ゲームパッド入力テスト |
| Gyro | ジャイロ |
| Gyro Calibration Failed | ジャイロの較正に失敗しました |
| Hide Unchecked | 未選択を隠す |
| HidGuardian Access | HidGuardian アクセス |
| HidHide Access | HidHide アクセス |
| HidHide Configuration Client | HidHide 設定クライアント |
| HidHide: not installed | HidHide: 未インストール |
| Import | インポート |
| In window | ウィンドウ内の座標 |
| Input Controller ID | 入力コントローラー ID |
| Input Slot # | 入力スロット # |
| Install Complete | インストール完了 |
| Install Updater | Updaterをインストール |
| Installing... | インストール中... |
| Invalid Special Action | 無効なスペシャルアクション |
| KB+M Controls | KB+M コントロール |
| Language applied successfully | 言語設定が適用されました |
| Left Stick | 左スティック |
| Lightbar Color | ライトバーの色 |
| Log | ログ |
| Log Settings | ログ設定 |
| Macro | マクロ |
| Max Archive Files: | アーカイブファイル数: |
| Min Angle Threshold | 最小角度のしきい値 |
| Min Log Level: | 最小ログレベル: |
| Minimize to Taskbar | タスクバーに最小化 |
| Move Down | 下に移動 |
| Move Up | 上に移動 |
| Mutli-Action Button | マルチアクションボタン |
| N/A | 不明 |
| Name | 名前 |
| New | 新規 |
| New Action | 新規アクション |
| New Profile | 新規プロファイル |
| No, cancel | いいえ、キャンセル |
| None | なし |
| none | なし |
| Normal | 通常のまま |
| Not valid | 無効 |
| Open release page | リリースページを開く |
| Open Release Page | リリースページを開く |
| OSC LISTENER STOPPED | OSC リスナーを停止しました |
| OSC SENDER STOPPED | OSC 送信を停止しました |
| Output Slot # | 出力スロット # |
| Path | パス |
| Presets | プリセット |
| Profile | プロファイル |
| Profile Folder | プロファイルフォルダ |
| Profiles | プロファイル |
| Program path | プログラムのパス |
| Pulse | 明滅 |
| Pulse at | 明滅（この残量以下） |
| Quick Charge | 急速充電 |
| Rainbow | 虹色 |
| Record | 記録 |
| Record mouse movement | マウスの移動を記録する |
| Remove | 削除 |
| Remove Action | アクションを除去 |
| Reservation | 予約（Dynamic／固定の種類） |
| Reset window size | ウィンドウサイズ初期化 |
| Right Stick | 右スティック |
| Rumble | 振動 |
| Run At Startup | システム起動時に開始 |
| Running as Admin | 管理者として実行中です |
| Running as User | ユーザーとして実行中です |
| Save | 保存 |
| Save Where | 保存先の選択 |
| Scan Code | コードをスキャン |
| Screen position | 画面の座標 |
| Scroll | スクロール |
| Searching for controllers... | コントローラーを検索中... |
| sec | 秒 |
| secs/cycle | 秒で一周 |
| Select a macro | マクロを選択 |
| Settings | 設定 |
| Shared Access | 共有アクセス |
| Show Notifications | 通知を表示 |
| Slide | スライド |
| Start | 開始 |
| Start Minimized | 最小化の状態で起動 |
| Starting... | 開始中... |
| Starts with | 前方一致 |
| Status | 状態 |
| Stop | 停止 |
| Stop Heavy | 強振動を停止 |
| Stop Light | 弱振動を停止 |
| Stopped DS4Windows | DS4Windows を停止しました |
| Tap | タップ |
| Test | テスト |
| Test Heavy | 強振動をテスト |
| Test Light | 弱振動をテスト |
| Tilt Down | 下に傾ける |
| Tilt Left | 左に傾ける |
| Tilt Right | 右に傾ける |
| Tilt Up | 上に傾ける |
| Trigger | トリガー |
| Unassigned | 未割り当て |
| Updater installation failed | Updaterのインストールに失敗しました |
| Updater not installed | Updaterがインストールされていません |
| Using Exclusive Mode | 排他モードで使用 |
| Using Shared Mode | 共有モードで使用 |
| Warnings only | 警告のみ |
| Welcome to DS4Windows | DS4Windowsへようこそ |
| While charging | 充電中の表示 |
| Window Title | ウインドウ名 |
| Xbox Controller Support | Xbox コントローラーのサポート |
| Xbox Device Support | Xbox デバイスのサポート |
| Yes, elevate | はい、権限を付与 |

## X3-8 での見直し結果（2026-11-22。上の表より優先する）
利用者が X3-3 の訳 528 件を 1 件ずつ見直した。詳しくは `PhaseX3-8-Plan.md`。上の表と食い違うものは、次のとおり。

### 英語表示に統一した用語（日本語リソースから削除。英語の値が出る）
| 分類 | 英語のまま表示する用語 |
|---|---|
| 出力スロット | Plug／Unplug／Dynamic／Permanent／Program／Task／Run As |
| 感度カーブ | Linear／Quadratic／Cubic／Easeout Cubic／Easeout Quad／Enhanced Precision／Custom |
| デッドゾーンの種類 | Axial／Radial |
| トリガーのモード | Hip Fire／Hip Fire Exclusive／Hip Fire Delay／Exclusive／Hair Trigger／Trigger Effect／Full Btn Pull |
| ボタン名のメニュー（Disable Invert） | Circle／Cross／Square／Triangle／Up／Down／Left／Right／Options／Share／Side L／Side R／Function Left／Function Right／Touchpad Click／Finger on Touchpad／2 Fingers on Touchpad／Bottom Left Paddle／Bottom Right Paddle |
| その他 | Utils／UDP Server／Accept 以外の Load Preset／Save Preset／Outer Btn／Outer Btn Dead／Flick Stick／Rotation／Square Stick／Six Axis (Accel)／Easing Duration／Touchpad／Special Actions／Controls／Axis Config／Other／Finished／Skip Version／Confirm／OSC の見出しと Port／Output Slots のタブ見出し |

### 上の表から変えた訳
| 英語 | 日本語 |
|---|---|
| Light（振動の弱。BindingWin.RumbleLight に分離） | 弱振動 |
| Light（テーマ） | 英語表示（Light） |
| Arrow Keys | 十字キー |
| Rename Profile | プロファイル名の変更 |
| Emulated Controller | 仮想コントローラーの出力タイプ |
| Inverse motors | 強弱振動モーターを入れ替える |
| Right click to disconnect wireless | 右クリックで Bluetooth 接続を切断 |
| Hip Fire Exclusive の説明文 | 「Hip Fire モードに似ています…」（モード名の表記に合わせた） |

### 方針（今後の訳し方）
- 画面の他の部分が英語表記のゲーム用語・設定名（Hip Fire、Axial など）は、説明文の中の名前も英語表記に合わせる。
- 記号（○×□△）は使わず、ボタン名は英語で出す。
- 未使用のキーは、リソースから削除する。

