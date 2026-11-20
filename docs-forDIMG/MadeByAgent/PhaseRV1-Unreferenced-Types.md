# PhaseRV1: 未参照の型の調査記録（規則 2.4 の手順 1・2・4）

作成日: 2026-11-19
対象: `TriggerType`、`MappingDoneEventArgs`、`ViGEmBusInfo`（PhaseRV1 の RV1-5）。`PressActionController` は削除を承認済みで、RV6-1 で扱う。ほかの候補（約 28 件）は RV6-0 で調べる。
方法: `git log -S`（追加・削除したコミットの特定）、`docs-forDIMG/` の計画書、`grep`（`DS4Windows`・`DS4WindowsTests`・`StandaloneTests`・`DS4WindowsCli` の `.cs`・`.xaml`）。
この Phase では、調べて記録するだけ。**削除はしない**（削除は RV6 で、承認を得てから）。

## 結論

| 型 | 判断 | 参照 | 次の処置 |
|---|---|---|---|
| `TriggerType`（`Core/Settings/TriggerType.cs`） | **使うべきでない（削除候補）** | 0 件（宣言のみ） | RV6-0 の表に載せ、承認を得て削除（`RemovedApiRuleTests` の表へ 1 行） |
| `MappingDoneEventArgs`（`Processing/MappingDoneEventArgs.cs`） | **使うべきでない（削除候補）** | 0 件（宣言のみ） | 同上 |
| `ViGEmBusInfo`（`Core/Platform/EnvironmentService.cs` の入れ子） | **未参照ではない（計測の誤検出）** | 同じファイルの中で使われている（235、241、300、310、315 行） | なし。候補から外す |

## 1. `TriggerType`

**経緯**
- 2025-12-18、`5e231d18`（フォーク。「`TriggerContext` を導入」）で、`DS4Control/Trigger.cs` に `TriggerContext`・`TriggerType`・`ITrigger`・`SingleButtonTrigger` の 4 型が追加された。
- 2026-10-15、Phase6-Step10b W1（`9217b2c1`）の「1 ファイル 1 型」の分割で、`TriggerType.cs` が単独のファイルになった（内容は変えていない）。
- 2026-10-19、Phase8-Step1 の孤立コードの判断（`Archive/Phase8-Step1-Orphan-Decisions.md`）で、`ITrigger`・`SingleButtonTrigger` を「**使うべきでない**」（D2＝B1 の `ComboTriggerInputAction`［`IInputAction.Evaluate`］が同じ役割を担う並行実装）と判断し、Phase8-Step3 W4（`985f9fbb`）で削除した。この判断は、「`TriggerType`／`TriggerContext` は利用中のため残す」とも書いていた。
- 今は、`TriggerContext` も存在せず、`TriggerType` を使う箇所は、ソース・テスト・XAML のどこにもない。

**判断**: 使うべきでない（削除候補）。
- 残した理由（利用中）は、`ITrigger` 系が使っていたからで、その削除で前提がなくなった。
- トリガーの種類（単独のボタン、組み合わせ、長押し、合成）を表す役割は、Phase8-Step1 の判断の記述では、`ComboTriggerInputAction` が担うとされている。この列挙型を使う側を新しく作る計画は、計画書（`docs-forDIMG/`）にない（`grep` で、PhaseRV1・RV6 と Phase6-Step9b／10b の一覧以外に出てこない）。
- 保存する設定（XML）の値としても使われていない（参照 0）。**機能の廃止に当たらない**（規則 2.2 の例外の対象外）。
- **使うべきもの**には当たらない（上位層の具体クラスを抽象経由に置き換える契約でもない）ので、`TODO` を付けて残す必要もない。

## 2. `MappingDoneEventArgs`

**経緯**
- 2015-07-30、upstream の DS4Windows 1.4.266（`86079b02`）で、`DS4Control/ScpUtil.cs` に定義されていた（デバイス番号を持つ `EventArgs`）。
- 2026-10-15、Phase6-Step10b W5（`365b28ae`）で、`ScpUtil.cs` の分割により単独のファイルになった（内容は変えていない。ファイルの先頭の `using` は、`ScpUtil.cs` の冒頭を写したままで、ほとんど使われていない。RV1-1 で整理される）。
- 対応するイベント（`MappingDone`）は、今のソースに存在しない（`grep` で、型の宣言以外に `MappingDone` が 0 件）。

**判断**: 使うべきでない（削除候補）。
- 引数の型だけが残り、発火するイベントも、購読する側もない。
- この型を使う側を新しく作る計画は、計画書（`docs-forDIMG/`）にない（`grep` で、Phase6-Step10b の一覧と PhaseRV1・RV6 以外に出てこない）。
- **機能の廃止に当たらない**（呼び出し 0。利用者から見える動きは変わらない）。

## 3. `ViGEmBusInfo`

**判断**: 未参照ではない。`Measure-CodeReview.py` の `unref` が、「自分のファイル以外から参照されない型」を数える作りのため、**同じファイルの中だけで使う private の入れ子型**を誤って拾っていた。
- `EnvironmentService`（`Core/Platform/EnvironmentService.cs`）の `private class ViGEmBusInfo` は、同じファイルの 235 行（リスト）、241 行（生成）、300 行（走査）、310 行（最新の選択）、315 行（`PopulateFromViGEmBusInfo` の引数）で使われている。
- 経緯: upstream の 2021 年のコミット（`79b8c996`「Refresh internal data about ViGEmBus on service startup」、`39a3d08d`、`70fcadc6`）で追加された、ViGEmBus のドライバーの情報の読み取り用の入れ物。
- 候補から外す。`01-Metrics.md` §6 の「誤検出に注意」に、**private の入れ子型**を追記した（RV6-0 の約 28 件の候補にも、同じ型の誤検出が含まれている可能性がある。RV6-0 で 1 件ずつ確かめる）。

## 4. 後続への引き継ぎ

- RV6-0 の表に、`TriggerType`・`MappingDoneEventArgs` を「削除候補（根拠は本書）」として載せ、`PressActionController` とあわせて一括で承認を得る（`PhaseRV6-Plan.md` D1 の運用）。
- 削除するときは、`RemovedApiRuleTests` の表へ 1 行ずつ足し、`git rm` は 1 型 1 コミット。
- 削除による機能の変化はない（呼び出し 0 件）ので、実機確認は不要。
