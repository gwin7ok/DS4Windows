using System;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W2（Phase8-Step4b-Plan.md §3.2、Q4＝A）: プロファイル 1 回分の読込で、節をまたいで共有する状態。
    /// 節ごとの局所変数（<c>Item</c> など）は、各節が自前で持つ。
    /// <see cref="LightbarSettings"/>／<see cref="LightInfo"/> は、<c>store.ResetProfile</c> の前に取得した参照のまま使う（旧コードと同じ）。
    /// </summary>
    internal sealed class ProfileLoadContext
    {
        public ProfileLoadContext(BackingStore store, XmlDocument xdoc, string rootName, int device, ControlService control,
            LightbarSettingInfo lightbarSettings, LightbarDS4WinInfo lightInfo)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Xdoc = xdoc ?? throw new ArgumentNullException(nameof(xdoc));
            RootName = rootName ?? throw new ArgumentNullException(nameof(rootName));
            Device = device;
            Control = control;
            LightbarSettings = lightbarSettings;
            LightInfo = lightInfo;
        }

        public BackingStore Store { get; }
        public XmlDocument Xdoc { get; }
        public string RootName { get; }
        public int Device { get; }
        public ControlService Control { get; }
        public LightbarSettingInfo LightbarSettings { get; }
        public LightbarDS4WinInfo LightInfo { get; }

        /// <summary>読込の引数 <c>launchprogram</c>（プロファイルの起動プログラムを、読込時に起動するか）。</summary>
        public bool LaunchProgram { get; set; }

        public DS4Windows.DI.IProfileSettingsService ProfileSettings { get; set; }

        /// <summary><c>ShiftModifier</c>（旧形式。全体で 1 つ）の値。雑多の節が書き、シフトのボタン割り当ての節が読む。</summary>
        public int ShiftModifier { get; set; }

        /// <summary><c>UseTPforControls</c> 要素の有無。雑多の節が書き、<c>TouchpadOutputMode</c> を読む節が読む。</summary>
        public bool TpForControlsPresent { get; set; }

        /// <summary>要素が欠けていた、または読めなかったとき true（読込の最後で、欠けた設定を追記して保存し直す判断に使う）。</summary>
        public bool MissingSetting { get; set; }
    }
}
