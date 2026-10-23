/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{
    [XmlRoot("Actions")]
    public class ActionsDTO : IDTO<BackingStore>
    {
        [XmlElement("Action")] // Use XmlElement here to skip container element
        public List<SpecialActionSerializer> actionSerializers;

        /// <summary>Phase10-Step4d W2: <see cref="MapTo"/> で、ID のない（または形式が不正な）SA に、新しい ID を割り当てたか。</summary>
        [XmlIgnore]
        public bool AssignedNewIds { get; private set; }

        public ActionsDTO()
        {
            actionSerializers = new List<SpecialActionSerializer>();
        }

        public void MapFrom(BackingStore source)
        {
            foreach(SpecialAction action in source.actions)
            {
                SpecialActionSerializer actionSerializer = new SpecialActionSerializer();
                switch(action.typeID)
                {
                    case SpecialAction.ActionTypeId.Macro:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        // R-2: Execute Mode は、独立した要素へ書く。Extras には、古い形式の Repeat を書かない（読込のみ受け付ける。MapTo）
                        actionSerializer.Extras = RemoveLegacyRepeatToken(action.extra);
                        actionSerializer.MacroExecuteMode = MacroExecuteModes.ToXmlValue(MacroExecuteModes.FromKeyType(action.keyType));
                        break;
                    case SpecialAction.ActionTypeId.Program:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        actionSerializer.Arguments = action.extra;
                        actionSerializer.Delay = action.delayTime;
                        break;
                    case SpecialAction.ActionTypeId.Profile:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        actionSerializer.UnloadTrigger = action.extra;
                        actionSerializer.ProfileId = action.profileId ?? string.Empty;   // Phase10-Step4d W3
                        break;

                    case SpecialAction.ActionTypeId.Key:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        // Phase10-Step1 W4（実機確認で発見。以前からの不具合）: action.details は、コンストラクターが「Scan Code」を除いたキーの値だけ
                        // （keyType の ScanCode ビットに移してある）。そのまま書くと、Scan Code が Actions.xml に残らず、開き直すと外れていた。
                        // 読み込み側（SpecialAction のコンストラクター）は、Details に「Scan Code」を含めば ScanCode を立てる（元の DS4Windows と同じ書式）
                        actionSerializer.Details = action.keyType.HasFlag(DS4KeyType.ScanCode) ? action.details + " Scan Code" : action.details;
                        if (!string.IsNullOrEmpty(action.extra))
                        {
                            string[] exts = action.extra.Split('\n');
                            // New name: SwitchMode (values: Press/Toggle). Keep compatibility by storing in SwitchMode.
                            actionSerializer.SwitchMode = exts.Length > 0 ? exts[0] : string.Empty;
                            actionSerializer.UnloadTrigger = exts.Length > 1 ? exts[1] : string.Empty;
                        }

                        break;

                    case SpecialAction.ActionTypeId.Button:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = "Button";
                        actionSerializer.Details = action.details;
                        // Phase6-Step10-3c: SwitchMode（Press／Toggle）を保存する（Key と同じ。従来は書き出していなかった）
                        if (!string.IsNullOrEmpty(action.extra))
                        {
                            string[] btnExts = action.extra.Split('\n');
                            actionSerializer.SwitchMode = btnExts.Length > 0 ? btnExts[0] : string.Empty;
                        }
                        break;

                    case SpecialAction.ActionTypeId.DisconnectBT:
                    case SpecialAction.ActionTypeId.BatteryCheck:
                    case SpecialAction.ActionTypeId.MultiAction:
                    case SpecialAction.ActionTypeId.GyroCalibrate:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        if (action.typeID == SpecialAction.ActionTypeId.BatteryCheck)
                        {
                            actionSerializer.Details = string.Join("|", action.details.Split(","));
                        }
                        else
                        {
                            actionSerializer.Details = action.details;
                        }

                        break;
                    case SpecialAction.ActionTypeId.None:
                    default:
                        actionSerializer = null;
                        break;
                }

                if (actionSerializer != null)
                {
                    actionSerializer.Id = action.id;   // Phase10-Step4d W2
                    actionSerializer.OutputExtras = action.outputExtras ?? string.Empty;   // Phase10-Step6 I5a
                    actionSerializer.Strength = action.analogStrength;   // Phase10-Step6 後の Step5（Button だけ、100 以外のときに書く）
                    actionSerializers.Add(actionSerializer);
                }
            }
        }

        /// <summary>R-2: Macro の Extras（<c>/</c> 区切り）から、古い形式の <c>Repeat</c> のトークン（完全一致）を取り除く。</summary>
        public static string RemoveLegacyRepeatToken(string extras)
        {
            if (string.IsNullOrEmpty(extras)) return extras;
            return string.Join("/", extras.Split('/').Where(token => token != "Repeat"));
        }

        public void MapTo(BackingStore destination)
        {
            foreach(SpecialActionSerializer actionSerializer in actionSerializers)
            {
                SpecialAction tempAction = null;
                switch(actionSerializer.TypeString)
                {
                    case "Macro":
                        tempAction = new SpecialAction(actionSerializer.Name,
                            actionSerializer.Trigger, actionSerializer.TypeString,
                            actionSerializer.Details, 0, actionSerializer.Extras);
                        // R-2: 独立した MacroExecuteMode 要素があれば、それを使う（古い形式の Extras の Repeat［SpecialAction が RepeatMacro にする］より優先）。
                        // ない（古いファイル、または Once）ときは、Extras の Repeat がそのまま効く
                        if (MacroExecuteModes.TryParse(actionSerializer.MacroExecuteMode, out MacroExecuteMode executeMode))
                            tempAction.keyType = MacroExecuteModes.ApplyTo(tempAction.keyType, executeMode);
                        break;
                    case "Program":
                        {
                            string extras = actionSerializer.Arguments;
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, actionSerializer.Delay,
                                extras
                            );
                        }

                        break;
                    case "Profile":
                        {
                            string extras = actionSerializer.UnloadTrigger;
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0, extras);
                        }

                        break;
                    case "Key":
                        {
                            string tempExtras = string.Empty;
                            // Prefer new SwitchMode if present.
                            // For legacy files where only UnloadStyle exists, treat that as the old toggle-style marker
                            // (map legacy UnloadStyle -> "Toggle" to preserve previous behavior).
                            string mode;
                            if (!string.IsNullOrEmpty(actionSerializer.SwitchMode))
                                mode = actionSerializer.SwitchMode;
                            else if (!string.IsNullOrEmpty(actionSerializer.UnloadStyle))
                                mode = "Toggle"; // legacy UnloadStyle indicates toggle behavior
                            else
                                mode = "Press"; // default to Press when neither SwitchMode nor legacy UnloadStyle present
                            if (!string.IsNullOrEmpty(mode) || !string.IsNullOrEmpty(actionSerializer.UnloadTrigger))
                            {
                                tempExtras = $"{mode}\n{actionSerializer.UnloadTrigger}";
                            }

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0, tempExtras);
                        }

                        break;
                        case "Button":
                            // Phase6-Step10-3c: SwitchMode（Press／Toggle）を読み込む。無い場合は従来どおり Press
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0,
                                string.IsNullOrEmpty(actionSerializer.SwitchMode) ? string.Empty : $"{actionSerializer.SwitchMode}\n");
                            break;
                    case "DisconnectBT":
                        {
                            double delayTime = actionSerializer.Delay;
                            string details = actionSerializer.Details;
                            double.TryParse(details, System.Globalization.NumberStyles.Float, DS4Windows.ConfigFileCulture.DecimalCulture, out delayTime);

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                details, delayTime);
                        }

                        break;
                    case "BatteryCheck":
                        {
                            double delayTime = actionSerializer.Delay;
                            string details = actionSerializer.Details;
                            if (double.TryParse(details.Split('|')[0], System.Globalization.NumberStyles.Float, DS4Windows.ConfigFileCulture.DecimalCulture, out delayTime))
                            {
                            }
                            else if (double.TryParse(details.Split(',')[0], System.Globalization.NumberStyles.Float, DS4Windows.ConfigFileCulture.DecimalCulture, out delayTime))
                            {
                            }

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                details, delayTime);
                        }

                        break;

                    case "MultiAction":
                        tempAction = new SpecialAction(actionSerializer.Name,
                            actionSerializer.Trigger, actionSerializer.TypeString,
                            actionSerializer.Details, actionSerializer.Delay,
                            actionSerializer.Extras);
                        break;
                    case "SASteeringWheelEmulationCalibrate":
                        // Phase9 Steering 削除 W1: 廃止した種別。読み込まずに取り除く（tempAction は null のまま＝一覧に入らない。次の保存でファイルから消える）
                        AppLogger.LogToGui($"[DI] 廃止された割り当て種別 'SASteeringWheelEmulationCalibrate'（Special Action: {actionSerializer.Name}）を、読み込み時に取り除きました。", false);
                        break;
                    case "GyroCalibration":
                        double.TryParse(actionSerializer.Details, System.Globalization.NumberStyles.Float, DS4Windows.ConfigFileCulture.DecimalCulture, out var delay);
                        tempAction = new SpecialAction(actionSerializer.Name, actionSerializer.Trigger,
                            actionSerializer.TypeString, actionSerializer.Details, delay);
                        break;
                    default:
                        break;
                }

                if (tempAction != null)
                {
                    // Phase10-Step4d W2: ファイルの ID を引き継ぐ。ない（古いファイル）・形式が不正なら、SpecialAction が作った新しい ID のままにして、呼び出し側へ知らせる
                    if (tempAction.typeID == SpecialAction.ActionTypeId.Profile && !string.IsNullOrEmpty(actionSerializer.ProfileId))
                        tempAction.profileId = actionSerializer.ProfileId;   // Phase10-Step4d W3

                    // Phase10-Step6 I5a: SA に付けた Extras。ない（古いファイル）・空白だけのときは、付けない
                    if (!string.IsNullOrWhiteSpace(actionSerializer.OutputExtras)) tempAction.outputExtras = actionSerializer.OutputExtras.Trim();

                    // Phase10-Step6 後の Step5: アナログな出力の強さ。ない（古いファイル）ときは 100。範囲外は 0〜100 に丸める
                    if (tempAction.typeID == SpecialAction.ActionTypeId.Button)
                        tempAction.analogStrength = Math.Max(0, Math.Min(100, actionSerializer.Strength));

                    if (!string.IsNullOrEmpty(actionSerializer.Id) && Guid.TryParseExact(actionSerializer.Id, "N", out _))
                        tempAction.id = actionSerializer.Id;
                    else
                        AssignedNewIds = true;

                    destination.actions.Add(tempAction);
                }
            }
        }
    }
}
