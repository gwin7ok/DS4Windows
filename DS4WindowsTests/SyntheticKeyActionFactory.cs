using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step8 8a: Controls タブの Key の識別用 <c>SpecialAction</c>（名前 <c>Synthetic_Key_{device}_{control}</c>。状態の鍵）を、テストが作るための部品。
    /// 旧 <c>Mapping.GetOrCreateSyntheticKeyAction</c>（旧経路の合成と、キャッシュ）は、旧経路の削除で <c>Mapping</c> から削除した。新経路の束縛は、同じ名前の識別用 SA を作る
    /// （<c>ControlBindingBuilder</c>）ため、状態は名前で引ける。
    /// </summary>
    internal static class SyntheticKeyActionFactory
    {
        public static SpecialAction Create(int device, int control, uint outputKey, bool toggle, bool useScan)
        {
            var sa = new SpecialAction($"Synthetic_Key_{device}_{control}", "Synthetic", "Key", "Key", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Key;
            sa.type = "Key";
            sa.details = outputKey.ToString();

            DS4KeyType kt = (DS4KeyType)0;
            if (useScan) kt |= DS4KeyType.ScanCode;
            if (toggle) kt |= DS4KeyType.Toggle;
            sa.keyType = kt;
            return sa;
        }
    }
}
