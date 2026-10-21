using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

public class SpecialActionViewModel : NotifyDataErrorBase, IActionTypeEditor
{
    private double holdInterval;
    public double HoldInterval { get => holdInterval; set => holdInterval = value; }
    private int actionIndex;
    // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
    private readonly ISpecialActionRepository specialActionRepository;

    public SpecialActionViewModel(int actionIndex, ISpecialActionRepository specialActionRepository)
    {
        this.specialActionRepository = specialActionRepository ?? throw new System.ArgumentNullException(nameof(specialActionRepository));
        this.actionIndex = actionIndex;
    }

    public void LoadAction(SpecialAction action)
    {
        holdInterval = action.delayTime;
    }

    // Phase10-Step6 I6b: Controls のモード。押し続ける秒数だけを持つ種別（Disconnect BT、Calibrate gyro、Calibrate sixaxis wheel）の項目との往復
    public void LoadControlItem(ControlSpecialItem item)
    {
        if (item != null) holdInterval = item.HoldSeconds;
    }

    // 通常の割り当て（キー・マクロの手順）は残さない種別。秒数だけを項目に入れる
    public ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type) => new ControlSpecialItem { Type = type, HoldSeconds = holdInterval };

    public void SaveAction(SpecialAction action, bool edit = false)
    {
        specialActionRepository.SaveAction(action.name, action.controls, actionIndex, $"{holdInterval.ToString("#.##", ConfigFileCulture.DecimalCulture)}", edit, holdInterval, actionId: action.id);
    }

    public override bool IsValid(SpecialAction action)
    {
        ClearOldErrors();

        bool valid = true;
        List<string> holdIntervalErrors = new List<string>();

        if (holdInterval < 0 || holdInterval > 60)
        {
            valid = false;   // Phase10-Step4c W3: 範囲外の値は、保存を止める
            holdIntervalErrors.Add("Interval not valid");
            errors["HoldInterval"] = holdIntervalErrors;
            RaiseErrorsChanged("HoldInterval");
        }

        return valid;
    }

    public override void ClearOldErrors()
    {
        if (errors.Count > 0)
        {
            errors.Clear();
            RaiseErrorsChanged("HoldInterval");
        }
    }
}