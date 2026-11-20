
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    public static class StandaloneTestHelpers
    {
        public static SpecialAction CreateToggleAction(ushort logicalKey, string name = "test-toggle-action")
        {
            var sa = new SpecialAction(name, "-1", "Key", logicalKey.ToString());
            sa.KeyButtonSwitchMode = SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            sa.typeID = SpecialAction.ActionTypeId.Key;
            return sa;
        }

        public static SpecialAction CreateKeyAction(string name, string controls, string details)
        {
            var sa = new SpecialAction(name, controls, "Key", details);
            sa.typeID = SpecialAction.ActionTypeId.Key;
            return sa;
        }
    }
}
