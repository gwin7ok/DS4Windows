using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.InputDevices;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6d（Phase9-E6-Plan.md §3.2）: <see cref="JoyConLinker"/> の差分テスト。Joy-Con の実機がなくても、偽物のデバイスで確かめる。
    /// 旧 <c>ControlService.Start</c>／<c>HotPlug</c> の結合の手順を、このファイルの中に逐語でコピーし、全ての小さな組み合わせ
    /// （デバイスの種類 × 結合の可否 × 主／副 × 結合済みか、3 台の順序、結合モード）で、結合の結果（相手、状態）と、取り外し後の結果が同じことを確かめる。
    /// </summary>
    public class Phase9E6JoyConLinkerTests
    {
        private sealed class FakeJoy : IJoinableDevice
        {
            public string Name;
            public InputDeviceType DeviceType { get; set; }
            public bool PerformStateMerge { get; set; }
            public bool PrimaryDevice { get; set; }
            public int JointDeviceSlotNumber { get; set; } = -1;
            public FakeJoy JointDevice;
            public DS4State JointState = new DS4State();
            public event Action Removal;

            public void SetJointDevice(IJoinableDevice other) { JointDevice = (FakeJoy)other; }
            public void CopyJointStateFrom(IJoinableDevice other) { JointState = ((FakeJoy)other).JointState; }
            public void OnRemoval(Action handler) { Removal += handler; }
            public void FireRemoval() { Removal?.Invoke(); }
        }

        private static bool LeftOrRight(FakeJoy d) =>
            d.DeviceType == InputDeviceType.JoyConL || d.DeviceType == InputDeviceType.JoyConR;

        // ---- 旧 Start の結合（逐語）----
        private static void OldStart(IList<FakeJoy> devices, JoyConDeviceOptions.LinkMode mode)
        {
            FakeJoy tempPrimaryJoyDev = null;
            foreach (FakeJoy device in devices)
            {
                if (mode == JoyConDeviceOptions.LinkMode.Joined)
                {
                    if (LeftOrRight(device) && device.PerformStateMerge)
                    {
                        if (tempPrimaryJoyDev == null)
                        {
                            tempPrimaryJoyDev = device;
                        }
                        else
                        {
                            FakeJoy currentJoyDev = device;
                            tempPrimaryJoyDev.JointDevice = currentJoyDev;
                            currentJoyDev.JointDevice = tempPrimaryJoyDev;

                            tempPrimaryJoyDev.JointState = currentJoyDev.JointState;

                            FakeJoy parentJoy = tempPrimaryJoyDev;
                            tempPrimaryJoyDev.Removal += () => { currentJoyDev.JointDevice = null; };
                            currentJoyDev.Removal += () => { parentJoy.JointDevice = null; };

                            tempPrimaryJoyDev = null;
                        }
                    }
                }
            }
        }

        // ---- 旧 HotPlug の結合（逐語）----
        private static void OldHotPlug(IList<FakeJoy> devices, JoyConDeviceOptions.LinkMode mode)
        {
            FakeJoy tempPrimaryJoyDev = null;
            FakeJoy tempSecondaryJoyDev = null;

            if (mode == JoyConDeviceOptions.LinkMode.Joined)
            {
                tempPrimaryJoyDev = devices.Where(d => LeftOrRight(d) && d.PrimaryDevice && d.JointDeviceSlotNumber == -1).FirstOrDefault();
                tempSecondaryJoyDev = devices.Where(d => LeftOrRight(d) && !d.PrimaryDevice && d.JointDeviceSlotNumber == -1).FirstOrDefault();
            }

            foreach (FakeJoy device in devices)
            {
                if (mode == JoyConDeviceOptions.LinkMode.Joined)
                {
                    if (LeftOrRight(device) && device.PerformStateMerge)
                    {
                        if (device.PrimaryDevice && tempSecondaryJoyDev != null)
                        {
                            FakeJoy currentJoyDev = device;
                            tempSecondaryJoyDev.JointDevice = currentJoyDev;
                            currentJoyDev.JointDevice = tempSecondaryJoyDev;

                            tempSecondaryJoyDev.JointState = currentJoyDev.JointState;

                            FakeJoy secondaryJoy = tempSecondaryJoyDev;
                            secondaryJoy.Removal += () => { currentJoyDev.JointDevice = null; };
                            currentJoyDev.Removal += () => { secondaryJoy.JointDevice = null; };

                            tempSecondaryJoyDev = null;
                            tempPrimaryJoyDev = null;
                        }
                        else if (!device.PrimaryDevice && tempPrimaryJoyDev != null)
                        {
                            FakeJoy currentJoyDev = device;
                            tempPrimaryJoyDev.JointDevice = currentJoyDev;
                            currentJoyDev.JointDevice = tempPrimaryJoyDev;

                            tempPrimaryJoyDev.JointState = currentJoyDev.JointState;

                            FakeJoy parentJoy = tempPrimaryJoyDev;
                            tempPrimaryJoyDev.Removal += () => { currentJoyDev.JointDevice = null; };
                            currentJoyDev.Removal += () => { parentJoy.JointDevice = null; };

                            tempPrimaryJoyDev = null;
                        }
                    }
                }
            }
        }

        private static void NewStart(IList<FakeJoy> devices, JoyConDeviceOptions.LinkMode mode)
        {
            var linker = new JoyConLinker();
            IJoinableDevice pending = null;
            foreach (FakeJoy device in devices)
            {
                pending = linker.LinkOnStart(device, pending, mode);
            }
        }

        private static void NewHotPlug(IList<FakeJoy> devices, JoyConDeviceOptions.LinkMode mode)
        {
            var linker = new JoyConLinker();
            linker.FindPendingForHotPlug(devices, mode, out IJoinableDevice primary, out IJoinableDevice secondary);
            foreach (FakeJoy device in devices)
            {
                linker.LinkOnHotPlug(device, ref primary, ref secondary, mode);
            }
        }

        private static string Snapshot(IList<FakeJoy> devices)
        {
            var owners = new Dictionary<DS4State, string>();
            foreach (var d in devices) owners[d.JointState] = d.Name;
            var sb = new StringBuilder();
            foreach (var d in devices)
            {
                sb.Append(d.Name).Append(":joint=").Append(d.JointDevice?.Name ?? "-")
                  .Append(",state=").Append(owners.TryGetValue(d.JointState, out string o) ? o : "?").Append(';');
            }

            return sb.ToString();
        }

        private static List<FakeJoy> Build(int[] codes)
        {
            var list = new List<FakeJoy>();
            InputDeviceType[] types = { InputDeviceType.JoyConL, InputDeviceType.JoyConR, InputDeviceType.JoyConGrip, InputDeviceType.DS4 };
            for (int i = 0; i < codes.Length; i++)
            {
                int c = codes[i];
                list.Add(new FakeJoy
                {
                    Name = "D" + i,
                    DeviceType = types[c & 3],
                    PerformStateMerge = (c & 4) != 0,
                    PrimaryDevice = (c & 8) != 0,
                    JointDeviceSlotNumber = (c & 16) != 0 ? 0 : -1,
                });
            }

            return list;
        }

        private static string Run(int[] codes, JoyConDeviceOptions.LinkMode mode, Action<IList<FakeJoy>, JoyConDeviceOptions.LinkMode> link)
        {
            var devices = Build(codes);
            link(devices, mode);
            var sb = new StringBuilder(Snapshot(devices));
            for (int i = 0; i < devices.Count; i++)
            {
                devices[i].FireRemoval();
                sb.Append('|').Append(Snapshot(devices));
            }

            return sb.ToString();
        }

        private static IEnumerable<int[]> Sequences(int length)
        {
            int[] codes = new int[length];
            while (true)
            {
                yield return (int[])codes.Clone();
                int k = 0;
                while (k < length && ++codes[k] == 32)
                {
                    codes[k] = 0;
                    k++;
                }

                if (k == length) yield break;
            }
        }

        [Theory]
        [InlineData(JoyConDeviceOptions.LinkMode.Joined)]
        [InlineData(JoyConDeviceOptions.LinkMode.Split)]
        public void Start_MatchesTheOldCode_ForEverySmallCombination(JoyConDeviceOptions.LinkMode mode)
        {
            int checkedCount = 0;
            foreach (int length in new[] { 1, 2, 3 })
            {
                foreach (int[] codes in Sequences(length))
                {
                    Assert.Equal(Run(codes, mode, OldStart), Run(codes, mode, NewStart));
                    checkedCount++;
                }
            }

            Assert.True(checkedCount > 30000);
        }

        [Theory]
        [InlineData(JoyConDeviceOptions.LinkMode.Joined)]
        [InlineData(JoyConDeviceOptions.LinkMode.Split)]
        public void HotPlug_MatchesTheOldCode_ForEverySmallCombination(JoyConDeviceOptions.LinkMode mode)
        {
            foreach (int length in new[] { 1, 2, 3 })
            {
                foreach (int[] codes in Sequences(length))
                {
                    Assert.Equal(Run(codes, mode, OldHotPlug), Run(codes, mode, NewHotPlug));
                }
            }
        }

        [Fact]
        public void Start_JoinsTheFirstTwoMergingJoyCons_AndClearsThePartnerOnRemoval()
        {
            var left = new FakeJoy { Name = "L", DeviceType = InputDeviceType.JoyConL, PerformStateMerge = true, PrimaryDevice = true };
            var right = new FakeJoy { Name = "R", DeviceType = InputDeviceType.JoyConR, PerformStateMerge = true };
            var devices = new List<FakeJoy> { left, right };

            NewStart(devices, JoyConDeviceOptions.LinkMode.Joined);

            Assert.Same(right, left.JointDevice);
            Assert.Same(left, right.JointDevice);
            Assert.Same(right.JointState, left.JointState);

            left.FireRemoval();
            Assert.Null(right.JointDevice);
            Assert.Same(right, left.JointDevice);   // 取り外された側自身の相手は、旧コードでも解除されない
        }
    }
}
