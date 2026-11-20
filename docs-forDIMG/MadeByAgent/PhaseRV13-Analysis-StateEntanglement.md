# 分析メモ: TouchpadMouse・GyroOutputProcessor の状態の絡みを解く（PhaseRV13 の素案）

作成日: 2026-11-21
状態: **分析のみ（コードは未変更）。ユーザーの依頼（「状態が密に絡んでいること自体が問題。シンプルで理想的な構造にするために何ができるか、あとで分析して」）への回答。計画書にする前に、方針の承認が要る。**
対象: `Processing/Touchpad/TouchpadMouse*.cs`（束ねて 740 行）、`Processing/Gyro/GyroOutputProcessor*.cs`（束ねて 573 行）。

## 1. 今の絡みの実態（読んで分かったこと）

### 1.1 TouchpadMouse
| 絡み | 内容 | 問題 |
|---|---|---|
| 持ち物が多い | 内部に `MouseCursor`・`MouseWheel`・`GyroOutputProcessor`・`TouchStickOutput`・`TouchButtonDetector`・`TapClickDetector`・`TouchMouseTrackball` を自分で `new` して持つ（コンストラクターの中で生成）。 | 差し替え・単独のテストができない。生成の順序が暗黙の依存になる。 |
| 公開の可変フィールド | `swipeLeft/Right/Up/Down`、`priorSwipe*`、`swipeLeftB…`、`slideleft/right`、`priorLeftDown…` など約 30 個が `public` のフィールド。外から読まれ、中で書かれる。 | 誰がいつ書くかが型で表せない。状態の持ち主が曖昧。 |
| 「今の入力」の差し替え | `private ControllerState s` を、イベントのたびに機器の現在の状態へ差し替えて読む。 | 一時的な結合（イベントの順序に依存）。どのメソッドが `s` の更新後に呼ばれる前提かが、コードからは分からない。 |
| 前回値の手持ち | `priorLeftDown`、`priorSwipe*` など「前回」を自分で持ち、毎回手で更新する。 | 前回値の更新の漏れがバグの温床。「現在・前回」の組を持つ型があれば 1 か所で更新できる。 |
| 共有の配列 | 出力は `StickMotionState` の「デバイス番号で引く配列」（`GyroStickX[deviceNum]` など）へ書く。 | デバイスごとの状態が、デバイスの外の共有の配列にある。持ち主がデバイスでなく、番号で引く規則に依存する。 |
| 仮想メンバー | `TouchesBegan/Moved/Ended` など `virtual`（テスト用の継承の名残）。 | 継承による差し替えは、合成による差し替えより絡みが強い。 |

### 1.2 GyroOutputProcessor
| 絡み | 内容 | 問題 |
|---|---|---|
| 判定の隠れた出力 | `IsTriggerActive(mode)` が、戻り値のほかに、フィールド `useReverseRatchet`・`triggeractivated`・`previousTriggerActivated`・`currentToggleGyroControls` を書き換え、呼び出し側（`SixaxisMoved`）が `useReverseRatchet` を読む。 | 戻り値で返すべき情報（逆ラチェットか）が、副作用のフィールド越しに渡っている。呼ぶ順序を変えると壊れる。 |
| `s` の差し替え | こちらも `s = _device.GetCurrentStateRef()` を、分岐ごとに代入してから使う。公開の `State` セッターもある。 | 同上（一時的な結合）。 |
| 3 つのモードが 1 つの型 | Controls／Mouse／MouseJoystick の 3 つの出力の種類の処理と、それぞれのトリガーのキャッシュ（`gyroTriggerCache[0..2]`）、フィルター（`OneEuroFilterPair`）を 1 つの型が持つ。 | モードごとに状態が別なのに、1 つの型にまとめているため、フィールドの意味がモードをまたいで混ざる。 |
| 不審な 1 行（要確認） | `_stickMotion.GyroStickX[deviceNum] = _stickMotion.GyroStickX[deviceNum] = 128;`（`SixaxisMoved` の MouseJoystick の枝）。X を 2 回代入しており、Y（`GyroStickY`）が初期化されない書き方に見える。 | 誤記の可能性。**挙動が変わるため、分析では直さない。** 実機・テストで意図を確かめてから扱う。 |
| 機器の具象を参照 | コンストラクターで `InputDeviceCore`（具象）を受ける。 | 規約（第 3 層は狭い契約）とは別の層だが、必要なのは `GetCurrentStateRef()` だけ。狭い契約（「今の状態を返す口」）で足りる。 |

## 2. 目指す構造（案）

共通の考え方: **「状態の持ち主を 1 か所にし、計算を状態から分け、イベントの順序への依存をなくす」。**

1. **入力は引数で渡す（`s` の差し替えをやめる）**: `SixaxisMoved(arg)` が、機器の現在の状態を `IInputStateSource.Current`（狭い契約）から取り、**ローカルの変数としてサブ処理へ渡す**。フィールドの `s` と公開の `State` セッターを廃止する。
2. **判定は値を返す**: `IsTriggerActive` は `GyroTriggerResult { bool Active; bool ReverseRatchet; }` を返し、隠れた出力のフィールド（`useReverseRatchet` ほか）をなくす。トグルの記憶（`currentToggleGyroControls`、`previousTriggerActivated`）は、トリガー判定の部品（`GyroTriggerEvaluator`）の中の私的な状態にする。
3. **モードごとに別の型（戦略）**: `GyroControlsOutput`・`GyroMouseOutput`・`GyroMouseStickOutput` を作り、各モードが自分の状態（フィルター、キャッシュ）だけを持つ。`GyroOutputProcessor` は「今のモードを選んで呼ぶ」薄い司令塔にする。
4. **「現在・前回」の組を持つ型**: `TouchpadMouse` の `prior*` 群を、`Edge<T>`（`Update(current)` で前回へ送る）のような小さな型へ。前回値の更新を 1 か所にし、書き漏れを型で防ぐ。
5. **公開の可変フィールドを読み取りの窓口へ**: `swipe*`・`slide*` は、`TouchpadGestureState`（読み取り専用の窓口＋内部で更新）にまとめ、外へは読み取りだけを出す。
6. **持ち物の生成を外へ（Composition 経由）**: `MouseCursor`・`MouseWheel`・`GyroOutputProcessor`・`TouchStickOutput` は、ファクトリー（`ITouchpadMouseFactory` 相当）か、コンストラクターで受ける形に。引数が増える場合は、**束ねるだけの箱にしない**（1 つの責務ごとに受ける。目標 12 以下）。
7. **デバイスごとの状態の持ち主をデバイス側へ**: `StickMotionState` の「デバイス番号で引く配列」は、デバイスごとの 1 オブジェクト（`DeviceStickMotion`）の配列へ変える。書く側は自分の 1 オブジェクトだけを持つ（番号の取り違えをなくす）。この変更は `Mapping` 側の読み手（`mapStickActionData` など）にも及ぶため、最後に回す。
8. **`virtual` の廃止**: 継承で差し替えているテストがあるかを調べ、あれば合成（偽物の注入）へ置き換えてから `virtual` を外す。

## 3. 進め方（案）

| Step | 内容 | リスク |
|---|---|---|
| A-0 | 特性化テストを足す（ジャイロ 3 モードのトリガー判定のトグル・逆ラチェット、タッチパッドのスワイプ・スライド・クリックの前回値の遷移）。不審な 1 行の意図を確かめる。 | なし（テストの追加） |
| A-1 | `IsTriggerActive` を値を返す形へ（上記 2）。`GyroTriggerEvaluator` を切り出す。 | 小。順序依存を壊さない |
| A-2 | `s` の差し替えを引数渡しへ（上記 1）。機器の具象を狭い契約へ。 | 小〜中 |
| A-3 | ジャイロの 3 モードを別の型へ（上記 3）。 | 中。フィルターの状態の移動 |
| B-1 | `Edge<T>` と `TouchpadGestureState`（上記 4・5）。 | 中。公開フィールドの外部の読み手が多い（`Mapping`・UI の表示） |
| B-2 | `TouchpadMouse` の持ち物の生成を外へ（上記 6）。 | 中。コンストラクターの引数 |
| C | `StickMotionState` をデバイスごとのオブジェクトへ（上記 7）。 | 大。`Mapping`・出力側まで影響。別 Phase 推奨 |
| D | `virtual` の廃止（上記 8）。 | 小 |

実機確認: タッチパッド（スワイプ、スライド、タップ、クリック領域、トラックボール）、ジャイロ（Controls・Mouse・MouseJoystick のトリガー、トグル、逆ラチェット）。`Device-Check-Checklist.md` へ追記する。

## 4. 効果（見込み）
- 500 行以上の型 2 件（`TouchpadMouse` 740、`GyroOutputProcessor` 573）が、型の単位で 500 行を下回る見込み（ジャイロの 3 モードを分けるだけで 573 → 約 250＋3 つの小さな型）。
- 評価の指標では、「公開の可変フィールド」「500 行以上の型」「構造の重複」が改善する見込み。
- 挙動を固定するテストが乏しい領域のため、A-0（特性化テスト）を必須とする。

## 5. 決めてほしいこと（承認のあと、別途ユーザーに確認）
1. 着手範囲: A（ジャイロ）だけ／A＋B（タッチパッドも）／C まで。
2. 不審な 1 行（`GyroStickX` の二重代入）の扱い: 意図を確かめて直す／現状維持。
3. 実機確認を挟む位置。
