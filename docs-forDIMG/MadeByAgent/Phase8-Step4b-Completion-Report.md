# Phase8-Step4b 完了報告書: プロファイルの読込・保存の節ごとの分解、機械的な整理 2 件

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step4b-Plan.md`（Q1〜Q8 を推奨どおり承認）、監査: `Phase8-Step4b-Audit.md`  
結果: **W1〜W10 のビルド・テスト成功（ユーザー実行）。W11（長さのガードと本報告書）は実施済み、ビルド・テストは実行待ち。挙動・XML の書式は変えていない（既存のゴールデンの文面は不変）**

---

## 1. 成果

| 項目 | 内容 |
|---|---|
| 4b-0（W1） | 監査（`Phase8-Step4b-Audit.md`）と、足りない節のゴールデン 7 件の追加（gap／legacy／minimal。`Phase8Step4bProfileGoldenGapTests`）。旧形式の読込専用の要素と、欠けた設定の追記も固定した |
| 4b-1 読込（W2〜W5） | `LoadProfile` を 15 節に分解（`DS4Control/Services/ProfileSections/`）。基盤は `IProfileLoadSection`、`ProfileLoadContext` |
| 4b-2 保存（W6） | `SaveProfile` を 7 節に分解（`IProfileSaveSection`、`ProfileSaveContext`）。計画の W6〜W8 を 1 回で実施した |
| 4b-3a（W9） | `getDS4ControlsByName`／`getX360ControlsByName` の本体を `DS4ControlNames`／`X360ControlTables` へ移し、`BackingStore` の関数を削除、呼び出し 18 件を付け替えた |
| 4b-3b（W10） | ホバーの表を作る 4 関数（約 385 行）を `ProfileEditor.HoverTables.cs`（同じ部分クラス）へ移した（Q7 の再相談で、部分クラスの案を承認） |
| 4b-4（W11） | 長さのガード（`Phase8Step4bW11LengthGuardTests`）、本報告書 |

### 行数の変化

| 対象 | 分解の前 | 分解の後 |
|---|---|---|
| `LegacyProfileXmlSerializer.cs` | 3,093 行 | 378 行 |
| `LoadProfile` | 約 2,320 行 | 164 行（準備・節の呼び出し・後始末） |
| `SaveProfile` | 約 560 行 | 55 行 |
| 節のクラス（最大） | - | 353 行（`ProfileStickAxialLoadSection`）。すべて 500 行以下 |
| `ProfileEditor.xaml.cs` | 2,522 行 | 2,136 行 |
| `BackingStore.cs` | 1,710 行 | 1,515 行（W9 のほか、前の Step の変更を含む） |

### 節の一覧

- 読込（`Profile…LoadSection`）: General、TouchpadBasic、TriggerStickBasic、StickAxial、MotionSensitivity、Misc、GyroControls、GyroSwipe、StickOutput、TriggerCurve、TouchpadMouse、OutputController、**Actions**、ButtonControl、ButtonShift。
- 保存（`Profile…SaveSection`）: General、TriggerStick、Motion、**Actions**、OutputCurve、OutputController、Button。
- `Actions`（`ProfileActions`）は、Step5 がビルダーの入力に使う節として、読込・保存とも単独のクラスにした。

## 2. 設計の記録

1. **節は `internal sealed`、`LegacyProfileXmlSerializer` が自分で生成する**（Q3＝A。DI 登録なし）。状態を持たず、1 回の読込・保存の状態は文脈（`ProfileLoadContext`／`ProfileSaveContext`）に持つ。
2. **本文は移動のみ**。節の先頭で `store`／`m_Xdoc`／`rootname`／`device`／`missingSetting` などの局所変数に文脈から取り出す形にして、本文の記述は変えていない。`catch { missingSetting = true; }` の握りつぶしも変えていない。
3. **節をまたぐ値は文脈に持つ**: `ShiftModifier`（雑多の節が書き、シフトのボタン割り当てが読む）、`UseTPforControls` の有無（雑多の節が書き、`TouchpadOutputMode` を読む節が読む）、`xinputPlug`／`xinputStatus`（出力コントローラーの節が書き、読込の最後の `PostLoadSnippet` が読む）、`MissingSetting`。`lightbarSettings`／`lightInfo` は、`ResetProfile` の前に取得した参照を文脈に渡す（旧コードと同じ）。
4. **追加設定（`LoadLegacyExtraSettings`／`SaveLegacyExtraSettings`）は節の間で呼ぶ**: 全般の節の直後。要素の順序を保つため、指揮役に残した。
5. **名前引きの補助関数は、使う節の局所関数にした**: `GetLightbarModeString`、`axisOutputCurveString`、`OutContDeviceString`、`axisOutputCurveId`、`OutContDeviceId`、`PortOldGyroSettings`。他に使う箇所がないことを確認してから、`LegacyProfileXmlSerializer` から削除した。
6. **右スティックのデルタ加速が左スティックの要素を見る不具合は、そのまま残した**（旧 `LoadProfile` の `if (lsDeltaAccelElement != null)` が、右スティックの読込を囲んでいる）。挙動を変えない方針のため直していない。左右を同じ節（`ProfileStickAxialLoadSection`）に置き、コメントに記した。**修正案は別 Step の候補**（下の「次の Step への引き継ぎ」）。
7. **W1 の legacy ゴールデンは、最初の案では gap と同じ結果になり、検査にならなかった**。新形式の要素（`GyroMouseSmoothingSettings`、ShiftControl の `Trigger` 属性）を取り除いて、旧形式の経路を通すように直した。

## 3. 検証結果

- 各 Wave のビルド・テストが成功（W5 の警告［未使用の `Item`］、W9 のテスト期待［`sbnShare` は列挙体の解析で例外］は、同じ Wave 内で是正）。
- ゴールデン（既存の `profile-varied.xml` ほか、W1 の gap／legacy／minimal）の文面は、分解の前後で不変。
- 順序の検査: `Phase8Step4bW2LoadFoundationTests`（読込・保存の節を、旧コードの順序で呼ぶこと）。
- 長さのガード: `Phase8Step4bW11LengthGuardTests`（`LoadProfile` 200 行、`SaveProfile` 80 行、節のファイル 500 行、`LegacyProfileXmlSerializer.cs` 450 行）。
- 名前引き 2 関数の値の対応: `Phase8Step4bW9NameFunctionsTests`。

## 4. 完了条件の確認

- [x] W1〜W10 のビルド・テストが成功（W11 は実行待ち）。ゴールデンの既存の文面は不変。
- [x] `LoadProfile`／`SaveProfile` が指揮役になった（164 行／55 行）。節のクラスは 1 ファイル 1 型で `DS4Control/Services/ProfileSections/` にある。
- [x] 差分比較（読込結果と保存結果）が分解の前後で一致（ゴールデン）。
- [x] 4b-3 の 2 件が完了。
- [x] 本報告書、`Phase8-Status.md` の更新。

## 5. 次の Step への引き継ぎ・注意

- **Step5（ビルダーと `Replace`）**: 特殊アクションの読込は `ProfileActionsLoadSection`、保存は `ProfileActionsSaveSection` に単独で置いてある。ビルダーの呼び出し（読込・適用の完了後に `TriggerBinding` を組み立てて `Replace`）は、`LoadProfile` の後始末か、`ProfileSlotApplier` の適用の後に足す。`OnProfileApplied` の接続（適用時に抑制の集合が空になる）は、Step5 の計画書で判断する（Step4 完了報告書 §5）。
- **右スティックのデルタ加速の読込の不具合**（§2-6）: 右スティックの要素が、左スティックの要素の有無で読まれる。左のデルタ加速の要素がない古いプロファイルで、右スティックの値が既定値のままになりうる。直すと挙動が変わるため、**ユーザーの承認を得て、別の Step で扱う**。
- **モデル図の更新案**（図の変更は、ユーザーの指示があるまで行わない）: `LegacyProfileXmlSerializer` の下に、節のクラス群（`ProfileSections`）と文脈を足す。
