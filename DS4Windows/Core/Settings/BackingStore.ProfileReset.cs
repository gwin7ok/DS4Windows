using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>プロファイルの既定値への戻しと、読み込み後の後処理。</summary>
    public partial class BackingStore
    {
        // プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込も使う）
        public void ResetMouseProperties(int device, IProfileLoadTarget control)
        {
            if (device < ControllerLimits.MaxControllerCount &&
                    control.GetTouchPad(device) != null)
            {
                control.GetTouchPad(device)?.ResetToggleGyroModes();
            }
        }

        // プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込も使う）
        public void ResetProfile(int device)
        {
            // 領域ごとの設定は、各領域のオブジェクトが自分を既定へ戻す。ここは、順に呼ぶのと、領域に入っていない配列の項目の既定値だけ
            removedShiftSettings[device].Clear();
            buttonMouseInfos[device].Reset();


            basicSlots[device].ResetForProfileLoad();
            rumbleSlots[device].ResetForProfileLoad();
            outputTypeSlots[device].ResetForProfileLoad();
            gyroSlots[device].ResetForProfileLoad();
            touchpadSlots[device].ResetForProfileLoad();
            stickSlots[device].ResetForProfileLoad();
            triggerSlots[device].ResetForProfileLoad();
            sixAxisSlots[device].ResetForProfileLoad();
            lightbarSlots[device].ResetForProfileLoad();

            Behavior.ds4Mapping = false;
        }

        // プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込も使う）
        public void CheckOldDevicestatus(int device, IProfileLoadTarget control,
            OutContType oldContType, out bool xinputPlug, out bool xinputStatus)
        {
            xinputPlug = false;
            xinputStatus = false;

            if (device < _environmentService.ControllerSlotLimit)
            {
                bool oldUseDInputOnly = ProfileSettings.UseDInputOnlyArray[device];
                IConfigurableDevice tempDevice = control.GetController(device);
                bool exists = tempBool = (tempDevice != null);
                bool synced = tempBool = exists ? tempDevice!.Synced : false;   // exists が true のときだけ評価される
                bool isAlive = tempBool = exists ? tempDevice!.IsAlive() : false;   // 同上
                if (basicSlots[device].DinputOnly != oldUseDInputOnly)
                {
                    if (basicSlots[device].DinputOnly == true)
                    {
                        xinputPlug = false;
                        xinputStatus = true;
                    }
                    else if (synced && isAlive)
                    {
                        xinputPlug = true;
                        xinputStatus = true;
                    }
                }
                else if (!basicSlots[device].DinputOnly &&
                    oldContType != outputTypeSlots[device].ContType)
                {
                    xinputPlug = true;
                    xinputStatus = true;
                }
            }
        }

        // プロファイルの読込を担う LegacyProfileXmlSerializer が呼ぶため公開にした（プリセットの読込も使う）
        public void PostLoadSnippet(int device, IProfileLoadTarget control, bool xinputStatus, bool xinputPlug)
        {
            IConfigurableDevice tempDev = control.GetController(device);
            if (tempDev != null && tempDev.Synced)
            {
                tempDev.QueueEvent(() =>
                {
                    //tempDev.IdleTimeout = idleDisconnectTimeout[device];
                    //tempDev.SetBTPollRate(btPollRate[device]);
                    if (xinputStatus)
                    {
                        if (xinputPlug)
                        {
                            if (control.HasOutputDevice(device))
                            {
                                //Global.activeOutDevType[device] = OutContType.None;
                                control.UnplugOutDev(device, tempDev);
                            }

                            OutContType tempContType = outputTypeSlots[device].ContType;
                            control.PluginOutDev(device, tempDev);
                            //Global.useDInputOnly[device] = false;
                        }
                        else
                        {
                            //Global.activeOutDevType[device] = OutContType.None;
                            control.UnplugOutDev(device, tempDev);
                        }
                    }

                    //tempDev.RumbleAutostopTime = rumbleAutostopTime[device];
                    //tempDev.SetRumble(0, 0);
                    //tempDev.LightBarColor = Global.getMainColor(device);
                    control.CheckProfileOptions(device, tempDev, true);
                });

                //Program.rootHub.touchPad[device]?.ResetTrackAccel(touchpadSlots[device].TrackballFriction);
            }
        }
    }
}
