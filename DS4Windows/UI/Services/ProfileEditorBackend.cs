#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// プロファイル編集画面（<see cref="ProfileEditor"/>）が使う、業務のサービスへの窓口。プロファイルの読み込み、
    /// スペシャルアクションの参照の差し替え、実機の振動・入力・較正の操作を包み、画面が業務のサービスの型を直接参照しない（V2）ようにする。
    /// 各操作の判断（スロットの範囲、未接続のときは何もしない）は、従来の画面のコードのまま。
    /// </summary>
    public sealed class ProfileEditorBackend
    {
        private readonly IProfileRepository profileRepository;
        private readonly IControllerSlotCommands slotCommands;
        private readonly IRuntimeFeatureSwitch runtimeFeatures;
        private readonly IEnvironmentService environmentService;

        public ProfileEditorBackend(IProfileRepository profileRepository, IControllerSlotCommands slotCommands,
            IRuntimeFeatureSwitch runtimeFeatures, IEnvironmentService environmentService)
        {
            ArgumentNullException.ThrowIfNull(profileRepository);
            this.profileRepository = profileRepository;
            ArgumentNullException.ThrowIfNull(slotCommands);
            this.slotCommands = slotCommands;
            ArgumentNullException.ThrowIfNull(runtimeFeatures);
            this.runtimeFeatures = runtimeFeatures;
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
        }

        /// <summary>実機（<paramref name="targetDevice"/>）が指定され、かつ有効なコントローラースロットの範囲内であるか。</summary>
        public bool HasTargetDevice(int targetDevice) => targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;

        /// <summary>振動のテストの制御（判断は <see cref="RumbleTestController"/>）。</summary>
        public RumbleTestController CreateRumbleTest() => new RumbleTestController(slotCommands, environmentService);

        /// <summary>スロットが有効な範囲なら、振動を止める（範囲外では何もしない）。</summary>
        public void StopRumble(int slot)
        {
            if (slot < environmentService.ControllerSlotLimit)
            {
                runtimeFeatures.SetRumble(0, 0, slot);
            }
        }

        /// <summary>いま入力されているボタン（コントローラーの入力で割り当て編集を開くために読む）。</summary>
        public DS4Controls GetActiveInputControl(int slot) => runtimeFeatures.GetActiveInputControl(slot);

        /// <summary>ジャイロの較正をやり直す。未接続のスロットでは何もしない。</summary>
        public void ResetGyroCalibration(int slot)
        {
            if (slot < environmentService.ControllerSlotLimit)
            {
                slotCommands.ResetGyroCalibration(slot);
            }
        }

        /// <summary>プロファイルを、編集用の作業スロットへ読み込む。</summary>
        public void LoadProfileForEditing(int editSlot, string profileName)
        {
            profileRepository.ProfilePath[ControllerLimits.TestProfileIndex] = profileName;
            profileRepository.LoadProfile(editSlot, profileName);
        }

        /// <summary>プロファイルの読み込み後の、有効なスペシャルアクションの名前（「/」区切り）。なければ空文字。</summary>
        public string GetEnabledActionsText(int editSlot)
        {
            List<string>? actions = profileRepository.ProfileActions != null && profileRepository.ProfileActions.Length > editSlot
                ? profileRepository.ProfileActions[editSlot]
                : null;
            return actions != null ? string.Join("/", actions) : string.Empty;
        }

        /// <summary>編集中のプロファイル（メモリ上）の、スペシャルアクションの参照を、旧名から新名へ差し替える。</summary>
        public void RenameActionInEditedProfile(int editSlot, string? oldName, string newName)
        {
            var actions = profileRepository.ProfileActions;
            if (actions != null && actions.Length > editSlot && actions[editSlot] is { } list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == oldName)
                    {
                        list[i] = newName;
                    }
                }
            }
        }

        /// <summary>すべてのプロファイルの XML の参照を新名へ置換し、書き換えたプロファイルの名前を返す。</summary>
        public IReadOnlyList<string> ReplaceActionReference(string oldName, string newName)
            => profileRepository.ReplaceActionReference(oldName, newName);

        /// <summary>そのアクションを使っている、適用中のプロファイルの名前。</summary>
        public IReadOnlyList<string> GetAppliedProfilesUsingAction(string actionName)
            => profileRepository.GetAppliedProfilesUsingAction(actionName);

        /// <summary>すべてのプロファイルの XML から、アクションの参照を取り除き、書き換えたプロファイルの名前を返す。</summary>
        public IReadOnlyList<string> RemoveActionReference(string actionName)
            => profileRepository.RemoveActionReference(actionName);
    }
}
