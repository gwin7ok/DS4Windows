using System;
using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// SA トリガー・SA ステアリング・ジャイロの制御・ジャイロのマウススティック（SATriggers〜GyroMouseStickSmoothingSettings）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileGyroControlsLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            int device = context.Device;
            IProfileLoadTarget control = context.Control;
            string path(string name) => "/" + context.RootName + "/" + name;

            void PortOldGyroSettings(int device)
            {
                if (store.gyroSlots[device].OutputMode == GyroOutMode.None)
                {
                    store.gyroSlots[device].OutputMode = GyroOutMode.Controls;
                }
            }

            context.ReadLegacy(path("SATriggers"), t => store.gyroSlots[device].MouseTriggers = t, () => store.gyroSlots[device].MouseTriggers = "-1");
            context.ReadLegacy(path("SATriggerCond"), t => store.gyroSlots[device].MouseTriggerCondAnd = BackingStore.SaTriggerCondValue(t), () => store.gyroSlots[device].MouseTriggerCondAnd = true);

            // 古いプロファイルに要素が残っていても、読み込まずに無視する（再保存すると、書かれなくなる）。

            context.ReadLegacy(path("GyroOutputMode"), t =>
            {
                // 廃止された DirectionalSwipe のモードは、なかったことにして（None）、保持だけする
                store.gyroSlots[device].RemovedSwipe.CaptureGyroOutputMode(t);
                Enum.TryParse(t, out GyroOutMode parsedMode);
                store.gyroSlots[device].OutputMode = parsedMode;
            }, () => PortOldGyroSettings(device));

            XmlNode? xmlGyroControlsElement = m_Xdoc.SelectSingleNode(path("GyroControlsSettings"));
            if (xmlGyroControlsElement != null)
            {
                XmlNode? Item = xmlGyroControlsElement.SelectSingleNode("Triggers");
                if (Item != null)
                {
                    store.gyroSlots[device].Controls.triggers = Item.InnerText;
                }

                Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                if (Item != null)
                {
                    store.gyroSlots[device].Controls.triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                }

                context.ReadBool(xmlGyroControlsElement, "TriggerTurns", v => store.gyroSlots[device].Controls.triggerTurns = v);

                context.ReadBool(xmlGyroControlsElement, "Toggle", v => store.SetGyroControlsToggle(device, v, control));
            }

            context.ReadLegacy(path("GyroMouseStickTriggers"), t => store.gyroSlots[device].MouseStickTriggers = t, () => store.gyroSlots[device].MouseStickTriggers = "-1");
            context.ReadLegacy(path("GyroMouseStickTriggerCond"), t => store.gyroSlots[device].MouseStickTriggerCondAnd = BackingStore.SaTriggerCondValue(t), () => store.gyroSlots[device].MouseStickTriggerCondAnd = true);
            context.ReadLegacy(path("GyroMouseStickTriggerTurns"), t => { bool.TryParse(t, out bool parsedValue); store.gyroSlots[device].MouseStickTriggerTurns = parsedValue; }, () => store.gyroSlots[device].MouseStickTriggerTurns = true);
            context.ReadLegacy(path("GyroMouseStickHAxis"), t => { int.TryParse(t, out int temp); store.gyroSlots[device].MouseStickHorizontalAxis = Math.Min(Math.Max(0, temp), 1); }, () => store.gyroSlots[device].MouseStickHorizontalAxis = 0);
            context.ReadLegacy(path("GyroMouseStickDeadZone"), t => { int.TryParse(t, out int temp); store.gyroSlots[device].MouseStick.deadZone = temp; }, () => store.gyroSlots[device].MouseStick.deadZone = 30);
            context.ReadLegacy(path("GyroMouseStickMaxZone"), t => { int.TryParse(t, out int temp); store.gyroSlots[device].MouseStick.maxZone = Math.Max(temp, 1); }, () => store.gyroSlots[device].MouseStick.maxZone = 830);

            context.ReadEnum<GyroMouseStickInfo.OutputStick>(m_Xdoc, path("GyroMouseStickOutputStick"), v => store.gyroSlots[device].MouseStick.outputStick = v);

            context.ReadEnum<GyroMouseStickInfo.OutputStickAxes>(m_Xdoc, path("GyroMouseStickOutputStickAxes"), v => store.gyroSlots[device].MouseStick.outputStickDir = v);

            context.ReadLegacy(path("GyroMouseStickAntiDeadX"), t => { double.TryParse(t, out double temp); store.gyroSlots[device].MouseStick.antiDeadX = temp; }, () => store.gyroSlots[device].MouseStick.antiDeadX = 0.4);
            context.ReadLegacy(path("GyroMouseStickAntiDeadY"), t => { double.TryParse(t, out double temp); store.gyroSlots[device].MouseStick.antiDeadY = temp; }, () => store.gyroSlots[device].MouseStick.antiDeadY = 0.4);
            context.ReadLegacy(path("GyroMouseStickInvert"), t => uint.TryParse(t, out store.gyroSlots[device].MouseStick.inverted), () => store.gyroSlots[device].MouseStick.inverted = 0);
            context.ReadLegacy(path("GyroMouseStickToggle"), t => { bool.TryParse(t, out bool temp); store.SetGyroMouseStickToggle(device, temp, control); }, () => store.SetGyroMouseStickToggle(device, false, control));
            context.ReadLegacy(path("GyroMouseStickMaxOutput"), t => store.gyroSlots[device].MouseStick.maxOutput = Math.Min(Math.Max(double.Parse(t), 0.0), 100.0), () => store.gyroSlots[device].MouseStick.maxOutput = 100.0);
            context.ReadLegacy(path("GyroMouseStickMaxOutputEnabled"), t => { bool.TryParse(t, out bool temp); store.gyroSlots[device].MouseStick.maxOutputEnabled = temp; }, () => store.gyroSlots[device].MouseStick.maxOutputEnabled = false);
            context.ReadLegacy(path("GyroMouseStickVerticalScale"), t => int.TryParse(t, out store.gyroSlots[device].MouseStick.vertScale), () => store.gyroSlots[device].MouseStick.vertScale = 100);

            // 平滑化の設定。グループがないときは、欠けた設定の印だけを付ける
            string smoothing = path("GyroMouseStickSmoothingSettings");
            XmlNode? xmlGyroMStickSmoothingElement = m_Xdoc.SelectSingleNode(smoothing);
            if (xmlGyroMStickSmoothingElement != null)
            {
                context.ReadLegacy(smoothing + "/UseSmoothing", t => { bool.TryParse(t, out bool tempSmoothing); store.gyroSlots[device].MouseStick.useSmoothing = tempSmoothing; }, () => store.gyroSlots[device].MouseStick.useSmoothing = false);

                // 方法の要素がないときは既定の方法を使い、欠けた設定にはしない。解釈に失敗したときだけ、平滑化を初期へ戻す
                try
                {
                    XmlNode? method = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                    string temp = method?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                    store.gyroSlots[device].MouseStick.DetermineSmoothMethod(temp);
                }
                catch
                {
                    store.gyroSlots[device].MouseStick.ResetSmoothing();
                    context.MissingSetting = true;
                }

                context.ReadLegacy(smoothing + "/SmoothingWeight", t =>
                {
                    int.TryParse(t, out int temp);
                    store.gyroSlots[device].MouseStick.smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                }, () => store.gyroSlots[device].MouseStick.smoothWeight = 0.5);

                context.ReadLegacy(smoothing + "/SmoothingMinCutoff", t =>
                {
                    double.TryParse(t, out double temp);
                    store.gyroSlots[device].MouseStick.minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                }, () => store.gyroSlots[device].MouseStick.minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF);

                context.ReadLegacy(smoothing + "/SmoothingBeta", t =>
                {
                    double.TryParse(t, out double temp);
                    store.gyroSlots[device].MouseStick.beta = Math.Min(Math.Max(0.0, temp), 1.0);
                }, () => store.gyroSlots[device].MouseStick.beta = GyroMouseStickInfo.DEFAULT_BETA);
            }
            else
            {
                context.MissingSetting = true;
            }
        }
    }
}
