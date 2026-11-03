# Phase11 個別計画書: Debouncing（デバウンス）機能の削除（K-W6-10）

作成日: 2026-11-03
関連: `Phase11-Plan.md` §13.4 の K-W6-10、copilot-instructions §2.2（機能廃止の例外）
状態: **D1〜D4 を推奨案で承認（2026-11-03）。E1〜E4 を実装済み（同日）。E5（実機確認）も完了（S9-1 ○、2026-11-03）。GitHub Issue は作らない（ユーザー判断）**。GitHub Issue は未作成（外部への公開になるため、作成はユーザーの判断に委ねる）。

実装メモ（2026-11-03）: E1〜E3 は、依存の連鎖が小さいため 1 コミットにまとめた。`ControlButtonInputAction`／`ComboTriggerInputAction`／`MultiTapInputAction`／`TwoStageTriggerProcessor` は、設定を使わなくなったが、呼び出し側とテストの変更を避けるため、コンストラクター引数は残した（理由のコメントつき。後続の整理の候補）。黄金ファイル（`Golden/Phase7Step10c`）の `<DebouncingMs>` と `debouncingMs` の行は、意図した変更として更新した（差分はその行のみ）。テスト: `DebouncerSetDurationTests` と、`DebouncingMs` を延長する 2 件のテストを削除し、XML は「保存しない・古いタグは読み捨て」のテストに差し替えた。Debug ALL 警告 0・エラー 0、Actions 2,803 件・Standalone 11 件合格。

## 1. 背景

- 実機（S7 の P9E6b-2。2026-11-02）で、Debouncing の数値を大きくして（500 ms）Cross を押すと、ボタンを放しても押したままの判定が続き、少し後に離した判定になる（キーが連続して入力され、少し後に止まる）。
- ユーザー決定（2026-11-02、2026-11-03）: 挙動の修正は大変なので、**デバウンス機能そのものを削除する**。Phase11 に入れる。copilot-instructions §2.2 の例外（ユーザーが明示的に承認した機能廃止）に当たるため、別 Issue 化と本書の承認を経て実施する。

## 2. 調査結果: DebouncingMs の使われ方（コードの読み。2026-11-03）

`DebouncingMs`（プロファイルの設定。XML の `<DebouncingMs>`、既定値は 0）は、**2 種類の使われ方**をしている。削除では、この 2 つを区別する必要がある。

### 2.1 デバウンスのフィルター本体（機能そのもの）
- `DS4Windows/DS4Control/Debouncer.cs`（`Debouncer`、入れ子の `DebouncerInstance`）。
- `DS4Device`: プロパティ `Debouncer`（95 行）、`SetupDebouncer()`（1478 行。ボタン 23 個に `AddDebouncer`）。呼び出し: `DS4Device.cs:971`、`DS3Device.cs:105`、`SwitchProDevice.cs:361`、`DualSenseDevice.cs:502`、`JoyConDevice.cs:649`。
- `ControlService.cs:879`: `cState = device.Debouncer.ProcessInput(cState);`（入力処理の入口）。
- `DeviceConnectionSetup.cs:54`: 適用時に `Debouncer?.SetDuration(...)`。
- `IDeviceLayerSettings.GetDebouncingMs(int)` と `DeviceLayerSettings.cs:27`（第 1 層が設定を読むための窓口。ほかの用途は無い）。
- UI: `ProfileEditor.xaml`（4205 行付近の Debouncing グループ）、`ProfileSettingsViewModel`（`DebouncingMs`、`HasDebouncingMsChanged`、`DebouncingMsChanged`）。

### 2.2 タイミング計算の一項としての参照（デバウンスのフィルターではない）
以下は、`DebouncingMs` を「時間に足す／引く」項として読んでいる。**値が 0（既定）のときは影響がない**が、0 以外のプロファイルでは、削除すると動きが変わる。
- `ControlButtonInputAction.cs:96`、`ComboTriggerInputAction.cs:104`: 遅延（`delaySeconds`）の基準時刻の判定（`utcNow - 100ms + DebouncingMs`）。
- `MultiTapInputAction.cs:62`: MultiAction（マルチタップ）の `debounce` 時間。
- `TwoStageTriggerProcessor.cs:113・173`: 2 段階トリガーの `hipFireMS + DebouncingMs`。

### 2.3 保存・読込・テスト
- `BackingStore.debouncingMs`、`IProfileSettingsService.DebouncingMs`、`ProfileSettingsService.cs:493`。
- `LegacyProfileXmlSerializer.cs:265`（読込）・`405`（保存）: XML の `<DebouncingMs>`。
- テスト: `DebouncerSetDurationTests`（11 件）、`ComboTriggerInputActionTests`、`Phase10Step4W4ControlSpecialTests`、`Phase10Step6W8ControlMultiActionTests`、`Phase8Step77bW1MultiActionDualPathTests`、`Phase8Step77bW2MultiTapTests`、`Phase9MapCustomTwoStageDifferentialTests`、`ProfileLegacyXml*Tests`、`FakeDeviceLayerSettings`、ガードテスト（`Step10FourGlobalRatchetGuardTests` のコメント、`Step12DeadShimRemovalGuardTests` の名前リスト）。

## 3. 決定してもらう事項

- **D1: 2.2 の「タイミング項」の扱い**。
  - 案 A（推奨）: **項を取り除く**（`+ DebouncingMs` を消し、値 0 と同じ動きにする）。既定値 0 のプロファイル（ほぼすべて）には影響がない。0 以外のプロファイルは、MultiAction・遅延・2 段階トリガーの判定が、わずかに短く（値の分）なる。
  - 案 B: 内部の定数として 0 を残す（コードに残すだけで、機能は取り除く）。差分は小さいが、不要な項が残る。
- **D2: XML の `<DebouncingMs>`**。
  - 案 A（推奨）: **読込は無視する**（タグがあっても読み捨て。古いプロファイル、他のユーザーのプロファイルの互換を保つ）。**保存はしない**（タグを書かない）。
  - 案 B: 読込も保存も残す（値を持ち続けるが、使わない）。「死んだ設定」が残るため非推奨。
- **D3: UI**。`ProfileEditor.xaml` の Debouncing グループを削除する。レイアウトの隙間が出ないことを実機で確認する（C5）。翻訳キー（`Debouncing` 系）は、使われなくなれば削除する（なければ残す。リソースのキー対応のテストを維持）。
- **D4: 進め方**。本書の承認 → GitHub Issue の作成（ユーザーが行う、または承認のうえで作成）→ Step ごとに 1 コミット。

## 4. 作業（承認後。各 Step ごとに単体テストと `Debug ALL`［警告 0・エラー 0］を確認し、1 コミット）

| Step | 内容 |
|---|---|
| E0 | 現状固定: 削除前に、2.2 の各箇所が `DebouncingMs = 0` のとき、削除後と同じ結果になることを示すテスト（既存の 0 前提のテストの確認）を整理する |
| E1 | フィルター本体の削除: `Debouncer`／`DebouncerInstance`（`Debouncer.cs`）、`DS4Device.Debouncer`・`SetupDebouncer` と 4 つのデバイスの呼び出し、`ControlService` の `ProcessInput` の行、`DeviceConnectionSetup` の `SetDuration`、`IDeviceLayerSettings.GetDebouncingMs` と実装、`FakeDeviceLayerSettings`。テスト `DebouncerSetDurationTests` を削除 |
| E2 | タイミング項の取り除き（D1）: `ControlButtonInputAction`、`ComboTriggerInputAction`、`MultiTapInputAction`、`TwoStageTriggerProcessor`。各クラスの `profileSettings` を `DebouncingMs` のためだけに受けていれば、その依存も整理する（コンストラクターの変更は、呼び出し側とテストを同時に更新） |
| E3 | 設定・XML・UI の削除: `BackingStore.debouncingMs`、`IProfileSettingsService.DebouncingMs`、`ProfileSettingsService`、`LegacyProfileXmlSerializer`（D2: 読込は読み捨て、保存は書かない）、`ProfileSettingsViewModel` の `DebouncingMs` ほか、`ProfileEditor.xaml` の Debouncing グループ（D3）、翻訳キー |
| E4 | ガードテスト・文書の更新: `Step10FourGlobalRatchetGuardTests`／`Step12DeadShimRemovalGuardTests` の基準値と名前リスト、`Phase7Step10eW5RatchetEqualityGuardTests`（一致検査）、モデル図 `docs-forDIMG/Model-Diagram/` の該当記述、CHANGELOG（機能廃止の明記） |
| E5 | 実機確認: プロファイル編集画面で Debouncing グループが消えていること、Cross の押し離しが正常なこと、古いプロファイル（`<DebouncingMs>` つき）が読めること、MultiAction・2 段階トリガーの動作が変わらないこと |

## 5. 受入条件

- Debouncing の UI・設定・フィルターが無くなり、ボタンの押し離しが入力どおりに処理される（遅れて押したままにならない）。
- `<DebouncingMs>` を含む既存のプロファイルが、エラーなく読み込める（D2 案 A）。
- `DebouncingMs` が 0 の既存の動作（MultiAction、遅延、2 段階トリガー）が変わらない。
- `Debug ALL` が警告 0・エラー 0、全テストがグリーン。既存のログ・ログレベルは維持する（2.3）。ラチェットの基準値は、減らした分だけ同時に更新する（3.1）。

## 6. リスク

- 2.2 の「タイミング項」は、`DebouncingMs` を 0 以外にしていたユーザーの動きを変える（D1）。ただし、この値の主な用途は削除する機能そのもので、値を大きくするとフィルターの不具合（本件）が出るため、実際に 0 以外にしているユーザーは少ないと考えられる。
- 他のデバイス（DualSense、Switch Pro、Joy-Con、DS3）も `Debouncer` を持つ。ボタンの一覧が違っても、削除すれば同じ経路になる（入力の `ProcessInput` を通らなくなるだけ）。
