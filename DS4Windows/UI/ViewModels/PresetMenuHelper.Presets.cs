using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>プリセットの選択に応じた割り当ての作成。</summary>
    public partial class PresetMenuHelper
    {
        public List<DS4Controls> ModifySettingWithPreset(int baseTag, int subTag)
        {
            List<object?> actionBtns = new List<object?>(5);
            List<DS4Controls> inputControls = new List<DS4Controls>(5);
            if (baseTag == 0)
            {
                actionBtns.AddRange(new object?[5]
                {
                    null, null, null, null, null,
                });
            }
            else if (baseTag == 1)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadUp, X360Controls.DpadDown,
                            X360Controls.DpadLeft, X360Controls.DpadRight, X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadDown, X360Controls.DpadUp,
                            X360Controls.DpadRight, X360Controls.DpadLeft, X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadUp, X360Controls.DpadDown,
                            X360Controls.DpadRight, X360Controls.DpadLeft, X360Controls.Unbound,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadDown, X360Controls.DpadUp,
                            X360Controls.DpadLeft, X360Controls.DpadRight, X360Controls.Unbound,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadRight, X360Controls.DpadLeft,
                            X360Controls.DpadUp, X360Controls.DpadDown, X360Controls.Unbound,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.DpadLeft, X360Controls.DpadRight,
                            X360Controls.DpadDown, X360Controls.DpadUp, X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 2)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LYNeg, X360Controls.LYPos,
                            X360Controls.LXNeg, X360Controls.LXPos, X360Controls.LS,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LYPos, X360Controls.LYNeg,
                            X360Controls.LXPos, X360Controls.LXNeg, X360Controls.LS,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LYNeg, X360Controls.LYPos,
                            X360Controls.LXPos, X360Controls.LXNeg, X360Controls.LS,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LYPos, X360Controls.LYNeg,
                            X360Controls.LXNeg, X360Controls.LXPos, X360Controls.LS,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LXPos, X360Controls.LXNeg,
                            X360Controls.LYNeg, X360Controls.LYPos, X360Controls.LS,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.LXNeg, X360Controls.LXPos,
                            X360Controls.LYPos, X360Controls.LYNeg, X360Controls.LS,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 3)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RYNeg, X360Controls.RYPos,
                            X360Controls.RXNeg, X360Controls.RXPos, X360Controls.RS,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RYPos, X360Controls.RYNeg,
                            X360Controls.RXPos, X360Controls.RXNeg, X360Controls.RS,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RYNeg, X360Controls.RYPos,
                            X360Controls.RXPos, X360Controls.RXNeg, X360Controls.RS,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RYPos, X360Controls.RYNeg,
                            X360Controls.RXNeg, X360Controls.RXPos, X360Controls.RS,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RXPos, X360Controls.RXNeg,
                            X360Controls.RYNeg, X360Controls.RYPos, X360Controls.RS,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.RXNeg, X360Controls.RXPos,
                            X360Controls.RYPos, X360Controls.RYNeg, X360Controls.RS,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 4)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.Y, X360Controls.A, X360Controls.X, X360Controls.B, X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.B, X360Controls.X, X360Controls.Y, X360Controls.A, X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.X, X360Controls.B, X360Controls.A, X360Controls.Y, X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 5)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object?[5]
                        {
                            VkLetter('W'), VkLetter('S'),
                            VkLetter('A'), VkLetter('D'),
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            VkLetter('D'), VkLetter('A'),
                            VkLetter('W'), VkLetter('S'),
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            VkLetter('A'), VkLetter('D'),
                            VkLetter('S'), VkLetter('W'),
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 6)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object?[5]
                        {
                            VkUp, VkDown,
                            VkLeft, VkRight,
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            VkRight, VkLeft,
                            VkUp, VkDown,
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            VkLeft, VkRight,
                            VkDown, VkUp,
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 7)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.Unbound,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.Unbound,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.Unbound,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object?[5]
                        {
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 8)
            {
                actionBtns.AddRange(new object?[5]
                {
                    X360Controls.Unbound, X360Controls.Unbound,
                    X360Controls.Unbound, X360Controls.Unbound,
                    X360Controls.Unbound,
                });
            }


            switch (highlightControl)
            {
                case ControlSelection.DPad:
                    inputControls.AddRange(new DS4Controls[4]
                    {
                        DS4Controls.DpadUp, DS4Controls.DpadDown,
                        DS4Controls.DpadLeft, DS4Controls.DpadRight,
                    });
                    break;
                case ControlSelection.LeftStick:
                    inputControls.AddRange(new DS4Controls[5]
                    {
                        DS4Controls.LYNeg, DS4Controls.LYPos,
                        DS4Controls.LXNeg, DS4Controls.LXPos, DS4Controls.L3,
                    });
                    break;
                case ControlSelection.RightStick:
                    inputControls.AddRange(new DS4Controls[5]
                    {
                        DS4Controls.RYNeg, DS4Controls.RYPos,
                        DS4Controls.RXNeg, DS4Controls.RXPos, DS4Controls.R3,
                    });
                    break;
                case ControlSelection.FaceButtons:
                    inputControls.AddRange(new DS4Controls[4]
                    {
                        DS4Controls.Triangle, DS4Controls.Cross,
                        DS4Controls.Square, DS4Controls.Circle,
                    });
                    break;
                case ControlSelection.None:
                default:
                    break;
            }

            // GetDefaultButtonMapping() は複製を返すため、ループの前に 1 回だけ取得する
            X360Controls[] defaultButtonMapping = profileSettings.GetDefaultButtonMapping();
            int idx = 0;
            foreach (DS4Controls dsControl in inputControls)
            {
                DS4ControlSettings setting = profileSettings.GetDS4CSetting(deviceNum, dsControl)!;   // 設定の表は、全ボタン分ある
                setting.Reset();
                if (idx < actionBtns.Count && actionBtns[idx] != null)
                {
                    object outAct = actionBtns[idx]!;   // 直前の条件で null でないと確かめた
                    X360Controls defaultControl = defaultButtonMapping[(int)dsControl];
                    if (!(outAct is X360Controls) || defaultControl != (X360Controls)outAct)
                    {
                        setting.UpdateSettings(outAct, null, DS4KeyType.None);
                        profileSettings.RefreshActionAlias(setting);
                    }
                }

                idx++;
            }

            return inputControls;
        }
    }
}
