using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7c W2: <c>Mapping.MapCustom</c>／<c>MapCustomAction</c>（特殊アクションのトリガー成立→実行）を実走行させる最小のハーネス。
    /// <see cref="MappingCharacterizationScope"/> の専用デバイス添字（<see cref="Device"/>）に対し、プロファイルのアクション一覧
    /// （<c>profileActions</c>／<c>profileActionDict</c>／<c>profileActionIndexDict</c>／<c>profileActionCount</c>）と
    /// <c>BackingStore.actions</c> へ、テスト用の <see cref="SpecialAction"/> を登録する。終了時にすべて元へ戻す。
    /// <c>MapCustomAction</c> は <c>async void</c> だが、初期化待ちが完了済みの <c>Task</c> のため、最初の実際の <c>await</c>
    /// （タップ判定の <c>Task.Delay</c>、プロファイル適用の <c>Task.Run</c>）までは <c>RunMapCustom</c> の中で同期的に走る。
    /// </summary>
    public sealed class SpecialActionTestScope : IDisposable
    {
        private readonly MappingCharacterizationScope _inner = new MappingCharacterizationScope();
        private readonly BackingStore _store = TestGlobals.Store;
        private readonly List<string> _savedNames;
        private readonly Dictionary<string, SpecialAction> _savedDict;
        private readonly Dictionary<string, int> _savedIndexDict;
        private readonly int _savedCount;
        private readonly List<SpecialAction> _added = new List<SpecialAction>();

        public int Device => MappingCharacterizationScope.Device;
        public MappingCharacterizationScope Scope => _inner;
        public Mapping Mapping { get; }

        /// <summary>Phase8-Step4 W4: ホストの <c>Mapping</c> が使うエンジン（抑制の集合の所有者）。</summary>
        public DS4Windows.Actions.ITriggerDispatchEngine Engine { get; } = TestHost.GetService<DS4Windows.Actions.ITriggerDispatchEngine>();

        /// <summary>Phase8-Step4 W4: このデバイスの遅延の基準時刻（旧 <c>Mapping.nowAction</c>／<c>oldnowAction</c>）。</summary>
        public DS4Windows.Actions.DelayClock Delay
        {
            get
            {
                var field = typeof(Mapping).GetField("_triggerInputs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var inputs = (DS4Windows.Actions.TriggerInput[])field.GetValue(Mapping);
                return inputs[Device].Delay;
            }
        }

        /// <summary>デバイスのプロファイルのアクション名の一覧（実体。<c>Mapping</c> が生で読む <c>List&lt;string&gt;</c> と同一）。</summary>
        public List<string> ProfileActionNames => _store.profileActions[Device];

        public SpecialActionTestScope()
        {
            Mapping = _inner.Mapper as Mapping;
            if (Mapping == null)
                throw new InvalidOperationException("IMapping の実体が Mapping ではない");

            _savedNames = _store.profileActions[Device];
            _savedDict = new Dictionary<string, SpecialAction>(_store.profileActionDict[Device]);
            _savedIndexDict = new Dictionary<string, int>(_store.profileActionIndexDict[Device]);
            _savedCount = _store.profileActionCount[Device];

            _store.profileActions[Device] = new List<string>();
            _store.profileActionDict[Device].Clear();
            _store.profileActionIndexDict[Device].Clear();
            _store.profileActionCount[Device] = 0;
            Engine.OnDeviceDisconnected(Device); // 抑制の集合を空にする（旧: suppressedTriggerButtons のクリア）
        }

        /// <summary>特殊アクションを 1 件登録する（<c>BackingStore.actions</c> の末尾に足し、その添字をプロファイル側の索引にする）。</summary>
        public SpecialAction Add(SpecialAction action)
        {
            int index = _store.actions.Count;
            _store.actions.Add(action);
            _added.Add(action);
            _store.profileActions[Device].Add(action.name);
            _store.profileActionDict[Device][action.name] = action;
            _store.profileActionIndexDict[Device][action.name] = index;
            _store.profileActionCount[Device] = _store.profileActions[Device].Count;
            return action;
        }

        /// <summary>ボタン種別の特殊アクション（Press。出力は X360 の A ボタン＝DS4 の Cross）を作って登録する。</summary>
        public SpecialAction AddButtonPress(string triggers)
        {
            string name = "p7c_btn_" + Guid.NewGuid().ToString("N");
            return Add(new SpecialAction(name, triggers, "Button", ((int)X360Controls.A).ToString(), 0, "Press\n"));
        }

        /// <summary>キー種別の特殊アクション（出力キーは 65）を作って登録する。<paramref name="toggle"/> が偽なら Press。</summary>
        public SpecialAction AddKey(string triggers, bool toggle)
        {
            string name = "p7c_key_" + Guid.NewGuid().ToString("N");
            return Add(new SpecialAction(name, triggers, "Key", "65", 0, (toggle ? "Toggle" : "Press") + "\n"));
        }

        /// <summary><c>BackingStore.actions</c> での添字（<c>Mapping</c> が <c>UntriggerIndex</c> に使う値）。</summary>
        public int IndexOf(SpecialAction action) => _store.actions.IndexOf(action);

        /// <summary>
        /// Phase8-Step1 W2: ホストの <c>DefaultProfileSwitcher</c>（アクションと <c>Mapping</c> の両方が使う Singleton）が持つ
        /// <c>IProfileApplicationService</c> を、記録用の実装に差し替える（終了時に元へ戻す）。実際のプロファイルの読込・適用は走らない。
        /// </summary>
        public RecordingProfileApplicationService InstallProfileRecorder()
        {
            if (_switcherField != null)
                return _recorder;

            var switcher = TestHost.GetService<DS4Windows.Actions.IProfileSwitcher>() as DS4Windows.Actions.DefaultProfileSwitcher;
            if (switcher == null)
                throw new InvalidOperationException("IProfileSwitcher の実体が DefaultProfileSwitcher ではない");

            _switcherField = typeof(DS4Windows.Actions.DefaultProfileSwitcher).GetField("profileAppService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (_switcherField == null)
                throw new InvalidOperationException("DefaultProfileSwitcher.profileAppService が見つからない");

            _switcherInstance = switcher;
            _savedProfileApp = _switcherField.GetValue(switcher);
            _recorder = new RecordingProfileApplicationService();
            _switcherField.SetValue(switcher, _recorder);
            return _recorder;
        }

        private System.Reflection.FieldInfo _switcherField;
        private object _switcherInstance;
        private object _savedProfileApp;
        private RecordingProfileApplicationService _recorder;

        /// <summary>1 フレーム分の入力を評価する（<c>MapCustom</c> を 1 回実行して出力の状態を返す）。</summary>
        public DS4State RunFrame(DS4State input) => _inner.RunMapCustom(input);

        public static DS4State Neutral() => MappingCharacterizationScope.NeutralState();

        public ActionInstanceState StateOf(SpecialAction action)
        {
            return ActionManager.GetStateFor(action, Device);
        }

        public void Dispose()
        {
            if (_switcherField != null)
                _switcherField.SetValue(_switcherInstance, _savedProfileApp);

            Engine.OnDeviceDisconnected(Device); // 抑制の集合を空にする（旧: suppressedTriggerButtons のクリア）
            Mapping.deviceRuntime[Device].UntriggerAction = null;
            Mapping.deviceRuntime[Device].UntriggerIndex = -1;

            foreach (SpecialAction action in _added)
                _store.actions.Remove(action);

            _store.profileActions[Device] = _savedNames;
            _store.profileActionDict[Device].Clear();
            foreach (var kv in _savedDict) _store.profileActionDict[Device][kv.Key] = kv.Value;
            _store.profileActionIndexDict[Device].Clear();
            foreach (var kv in _savedIndexDict) _store.profileActionIndexDict[Device][kv.Key] = kv.Value;
            _store.profileActionCount[Device] = _savedCount;

            _inner.Dispose();
        }
    }
}
