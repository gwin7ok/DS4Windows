#nullable enable

using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 廃止された「シフト」機能の設定（<c>ShiftControl</c>）の読込。
    /// シフトの設定は解釈せず、実行にも使わない。<c>&lt;ShiftControl&gt;</c> ノードの内容を、そのまま <see cref="RemovedShiftSettings"/> に保持するだけ
    /// （編集画面でユーザーが「残す」を選んだとき、保存で、そのまま書き出す）。項目がなければ、保持を空にする。
    /// </summary>
    internal sealed class ProfileButtonShiftLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            XmlNode? shiftControl = context.Xdoc.SelectSingleNode("/" + context.RootName + "/ShiftControl");
            context.Store.removedShiftSettings[context.Device].Capture(shiftControl);
        }
    }
}
