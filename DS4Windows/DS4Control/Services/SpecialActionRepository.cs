using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class SpecialActionRepository : ISpecialActionRepository
    {
        private readonly object _actionLock = new object();
        private readonly BackingStore _config;
        private readonly IPathService _pathService;

        public event EventHandler ActionsChanged;

        public SpecialActionRepository(BackingStore config, IPathService pathService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        private IPathService PathSvc => _pathService;

        public string ActionsPath
        {
            get
            {
                string baseDir = !string.IsNullOrEmpty(PathSvc.AppDataPath)
                    ? PathSvc.AppDataPath
                    : AppContext.BaseDirectory;
                return Path.Combine(baseDir, "Actions.xml");
            }
        }

        public IReadOnlyList<SpecialAction> Actions
        {
            get
            {
                lock (_actionLock)
                {
                    return _config != null && _config.actions != null
                        ? _config.actions.ToList().AsReadOnly()
                        : Array.Empty<SpecialAction>();
                }
            }
        }

        public List<SpecialAction> ActionList
        {
            get => _config != null ? _config.actions : null;
        }

        // Phase6-Step7-1（決定2＝L2）: Phase4-Step3（c06a1c9b）で作られたが App 側が未接続だった契約。
        // 従来は冒頭に「Actions.xml がなければ何もせず false を返す」判定があり、Global.LoadActions() と動作が違った
        // （BackingStore.LoadActions はファイルがなければ既定アクション「Disconnect Controller」を作って保存し true を返す）。
        // そのまま App から使うと、初回起動で CreateStandardActions が走り、全プロファイルの XML が書き換わってしまうため、
        // この判定を削除して Global と同じ動作にした（ファイルの有無の扱いは BackingStore.LoadActions に任せる）。
        // 残る差は、想定外の例外（XML・データ不正以外）も false にする点のみ（Global 側は例外を外へ投げる）。
        public bool LoadActions()
        {
            lock (_actionLock)
            {
                try
                {
                    bool result = _config.LoadActions();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.LoadActions: Actions.xml loaded via DI (result={result})");
                    OnActionsChanged();
                    return result;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SaveActions()
        {
            lock (_actionLock)
            {
                try
                {
                    if (_config != null)
                    {
                        _config.SaveActions();
                    }
                    else
                    {
                        Global.SaveActions();
                    }

                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace("[DI] SpecialActionRepository.SaveActions: Actions.xml saved via DI");

                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save Actions.xml: {ex.Message}", true);
                    return false;
                }
            }
        }

        public SpecialAction GetAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return null;

            lock (_actionLock)
            {
                return _config.actions.FirstOrDefault(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public int GetActionIndex(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return -1;

            lock (_actionLock)
            {
                return _config.actions.FindIndex(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ActionExists(string actionName)
        {
            return GetAction(actionName) != null;
        }

        public bool AddAction(SpecialAction action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.name) || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(action.name);
                if (index >= 0)
                {
                    _config.actions[index] = action;
                }
                else
                {
                    _config.actions.Add(action);
                }

                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] SpecialActionRepository.AddAction: Action '{action.name}' added via DI");
                OnActionsChanged();
                return true;
            }
        }

        public bool RemoveAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(actionName);
                if (index >= 0)
                {
                    _config.actions.RemoveAt(index);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.RemoveAction: Action '{actionName}' removed via DI");
                    OnActionsChanged();
                    return true;
                }
                return false;
            }
        }

        public bool ReplaceAction(string oldActionName, SpecialAction newAction)
        {
            if (string.IsNullOrWhiteSpace(oldActionName) || newAction == null || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(oldActionName);
                if (index >= 0)
                {
                    _config.actions[index] = newAction;
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.ReplaceAction: Action '{oldActionName}' replaced via DI");
                    OnActionsChanged();
                    return true;
                }
                return false;
            }
        }

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.CreateStdActions への薄い委譲。プロファイル XML の書き換えと読み込み直しは Global 側が行う。
        public void CreateStandardActions()
        {
            Global.CreateStdActions();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] SpecialActionRepository.CreateStandardActions: standard actions created via DI");
        }

        // ---- Phase6-Step8-1: ProfileEditor.xaml.cs の Global 直接参照解消 ----
        // Global.GetAction（m_Config.GetAction）と同じ実体 BackingStore.GetAction への薄い委譲。
        // 本リポジトリの _config は通常 Global.store（= m_Config）なので結果は Global.GetAction と一致する。
        public SpecialAction GetActionOrPlaceholder(string actionName)
        {
            if (_config == null)
                return new SpecialAction("null", "null", "null", "null");

            return _config.GetAction(actionName);
        }

        // ---- Phase6-Step10-3-1: スペシャルアクション編集画面の Global 直接参照解消 ----
        // Global.SaveAction / Global.RemoveAction への薄い委譲。これらは、実行中のアクション状態のクリア、
        // Actions.xml への書き込み、読み直しと検証（SaveAction ではキー種別のランタイム状態のリセットも）を行う。
        // メモリ上のリストだけを変える AddAction/RemoveAction とは動作が異なるため、別メソッドにしている。
        // 対象は Global.store（Global.m_Config と同じ実体）。本リポジトリの _config が Global.store 以外の場合でも、
        // Global と同じ実体に対して動作する。
        public void SaveAction(string name, string controls, int mode, string details, bool edit,
            double delayTime = 0.0, string extras = "")
        {
            Global.SaveAction(name, controls, mode, details, edit, delayTime, extras);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] SpecialActionRepository.SaveAction: Action '{name}' saved via DI (mode={mode}, edit={edit})");
            OnActionsChanged();
        }

        public bool RemoveActionAndSave(string actionName)
        {
            bool existed = ActionExists(actionName);
            Global.RemoveAction(actionName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] SpecialActionRepository.RemoveActionAndSave: Action '{actionName}' removed and saved via DI (existed={existed})");
            OnActionsChanged();
            return existed;
        }

        protected virtual void OnActionsChanged()
        {
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
