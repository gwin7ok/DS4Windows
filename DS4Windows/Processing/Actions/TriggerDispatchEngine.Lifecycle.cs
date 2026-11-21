using System.Collections.Generic;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Actions
{
    /// <summary>プロファイルの適用・切断・束縛の再構築・抑止の管理。</summary>
    public partial class TriggerDispatchEngine
    {
        public void AddSuppressed(int device, IReadOnlyList<DS4Controls> controls)
        {
            if (controls == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                for (int i = 0; i < controls.Count; i++)
                    rt.Suppressed.Add(controls[i]);
            }
        }

        public void ReleaseSuppressed(int device, ITriggerInput input)
        {
            if (input == null || (uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                DeviceRuntimeLifecycle.ReleaseSuppressed(rt, input);
            }
        }



        public void OnProfileApplied(int device)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            // 適用スレッドは、入力スレッドのものである状態を直接書かない。次の処理の先頭でリセットする
            _devices[device].ResetRequested = true;
        }

        public void OnDeviceDisconnected(int device)
        {
            if ((uint)device >= (uint)_devices.Length) return;

            DeviceRuntime rt = _devices[device];
            lock (rt.Lock)
            {
                _registry.Clear(device);
                DeviceRuntimeLifecycle.ResetOutputs(rt, rt.Bindings, null);   // 7.8e（Q6＝B）: 押したままの切断で、効果が残らないように、出力を元に戻す
                DeviceRuntimeLifecycle.ResetState(rt, null);
                rt.Pending = null;   // 物理切断では、復帰の合図も消す（適用では消さない。ResetState は触らない）
                rt.ResetRequested = false;
                rt.Bindings = null!; // 次の Prepare が必ず作り直す。 次の処理で、空の一覧から作り直す
            }
        }

        // 登録簿の一覧が差し替わっていたら実行時の配列を作り直し、リセットの要求があれば状態を初期化する
        private void Prepare(DeviceRuntime rt, int device, ITriggerInput? input)
        {
            IReadOnlyList<IActionBinding> snapshot = _registry.GetBindings(device);
            if (!ReferenceEquals(snapshot, rt.Bindings))
            {
                DeviceRuntimeLifecycle.ResetOutputs(rt, rt.Bindings, input);   // 7.8e（Q6＝B）: 差し替え前の出力が、押したまま有効にした効果を元に戻す
                DeviceRuntimeLifecycle.Rebuild(rt, snapshot);
            }

            if (rt.ResetRequested)
            {
                rt.ResetRequested = false;
                DeviceRuntimeLifecycle.ResetOutputs(rt, rt.Bindings, input);
                DeviceRuntimeLifecycle.ResetState(rt, input);
            }
        }






    }
}
