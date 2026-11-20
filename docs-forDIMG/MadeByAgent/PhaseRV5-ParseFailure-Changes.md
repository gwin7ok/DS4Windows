# PhaseRV5-3b: 解析に失敗した要素の読み込みの変更一覧（承認待ち）

作成日: 2026-11-20。出典: `ProfileLoadMutationGoldenTests` のゴールデンの差分（変更前 → 変更後）。保存の形は変わらない。

D2＝(B): 数値（整数・実数）の要素が、解析に失敗したとき（空の要素を含む）、0 を代入せず、何もしない（既定値のまま）にする。

## 変え方: unparsable（8 要素）

| 要素 | 変更前（既定値との差） | 変更後 |
|---|---|---|
| `/DS4Windows/LSAxialDeadOptions/MaxZoneX` | + $.stickSlots[8].LSMod.xAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].LSMod.xAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxZoneY` | + $.stickSlots[8].LSMod.yAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].LSMod.yAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxOutputX` | + $.stickSlots[8].LSMod.xAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].LSMod.xAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxOutputY` | + $.stickSlots[8].LSMod.yAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].LSMod.yAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxZoneX` | + $.stickSlots[8].RSMod.xAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].RSMod.xAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxZoneY` | + $.stickSlots[8].RSMod.yAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].RSMod.yAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxOutputX` | + $.stickSlots[8].RSMod.xAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].RSMod.xAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxOutputY` | + $.stickSlots[8].RSMod.yAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].RSMod.yAxisDeadInfo.maxOutput = 0 | 既定値のまま |

## 変え方: empty（8 要素）

| 要素 | 変更前（既定値との差） | 変更後 |
|---|---|---|
| `/DS4Windows/LSAxialDeadOptions/MaxZoneX` | + $.stickSlots[8].LSMod.xAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].LSMod.xAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxZoneY` | + $.stickSlots[8].LSMod.yAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].LSMod.yAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxOutputX` | + $.stickSlots[8].LSMod.xAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].LSMod.xAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/LSAxialDeadOptions/MaxOutputY` | + $.stickSlots[8].LSMod.yAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].LSMod.yAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxZoneX` | + $.stickSlots[8].RSMod.xAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].RSMod.xAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxZoneY` | + $.stickSlots[8].RSMod.yAxisDeadInfo.MaxZone = 0 + $.stickSlots[8].RSMod.yAxisDeadInfo.maxZone = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxOutputX` | + $.stickSlots[8].RSMod.xAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].RSMod.xAxisDeadInfo.maxOutput = 0 | 既定値のまま |
| `/DS4Windows/RSAxialDeadOptions/MaxOutputY` | + $.stickSlots[8].RSMod.yAxisDeadInfo.MaxOutput = 0 + $.stickSlots[8].RSMod.yAxisDeadInfo.maxOutput = 0 | 既定値のまま |

合計 16（要素 × 変え方）。変わるのは、スティックの軸別の不感帯の MaxZone・MaxOutput（左右・X/Y）だけで、それ以外の数値の要素は、既定値が 0 のため結果が同じ。

## 実機のプロファイルの走査（読み取りのみ。46 個）

空の要素は `LaunchProgram`・`*OutputCurveCustom`・`ShiftControl`・`Control`・`ProfileActions` の文字列型だけで、数値の要素の解析失敗は 0 件。したがって、実機のプロファイルの読み込み結果は変わらない。

## 追記: RV5-4（General 領域を定義の並びへ）で変わる要素

`ProfileGeneralSettings`（`ProfileSetting` の並び）へ移した、真偽値・byte の要素も、解析に失敗したときは何もしない（既定値のまま）になる。変わるのは次の 5 要素（解析できない・空・範囲外［byte の 99999］の 3 通りで、計 15 件）。

| 要素 | 変更前 | 変更後 |
|---|---|---|
| `touchToggle`、`outputDataToDS4`、`touchpadJitterCompensation` | 解析に失敗すると `false` を代入 | 既定値のまま |
| `RumbleBoost`、`touchSensitivity`（byte） | 解析に失敗（範囲外の 99999 を含む）すると `0` を代入 | 既定値のまま |

実機のプロファイル 46 個には、これらの要素の解析失敗は 0 件（上の走査のとおり）。
