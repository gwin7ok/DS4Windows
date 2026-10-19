using System;
using System.Collections.Generic;
using System.IO;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.4-W2（Q1＝A）: 統一ディスパッチ（エンジン）から使うときは、旧 <c>MapCustomAction</c> の Program の分岐が出していたログ
    /// （Trace の立ち上がり 4 行、Debug の「SpecialAction Program: …」）を、同じ文言・順序で出す。
    /// 旧経路のアダプター（<c>LaunchProcessActionAdapter</c>）は、<c>Mapping</c> が同じログを出すため、このログを出さない（添字つきのコンストラクターを使ったときだけ出す）。
    /// </summary>
    public class LaunchProcessAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IProcessLauncher _launcher;
        private readonly bool _logTrigger;
        private readonly int _actionIndex;
        private readonly ISpecialActionRepository _actionRepository;

        /// <summary>エンジン用: 旧経路のログ（立ち上がり）も出す。<paramref name="actionIndex"/> と <paramref name="actionRepository"/> は、ログの「index」「ActionEntries」用。</summary>
        public LaunchProcessAction(SpecialAction sa, IProcessLauncher launcher, int actionIndex, ISpecialActionRepository actionRepository)
            : this(sa, launcher)
        {
            _logTrigger = true;
            _actionIndex = actionIndex;
            _actionRepository = actionRepository;
        }

        public LaunchProcessAction(SpecialAction sa, IProcessLauncher launcher)
        {
            this.sa = sa;
            this._launcher = launcher ?? throw new ArgumentNullException(nameof(launcher)); // Phase6-Step10-4-4: 必須の依存
        }

        public string Id => sa?.name ?? "LaunchProcess";

        // 旧 MapCustomAction の Program の分岐と同じ順序: LogSpecialActionTrace（立ち上がり）→ LogActionDoneCountOnTrigger
        private void LogRising(IOutputContext ctx)
        {
            try
            {
                if (AppLogger.IsTraceEnabled)
                {
                    AppLogger.LogTrace($"Trigger detected for SA '{sa.name}' on device {ctx.Device}");
                    AppLogger.LogTrace($"SA '{sa.name}' maps to Type:{sa.type} Details:{sa.details}");
                    // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                    AppLogger.LogTrace($"Trigger keys for SA '{sa.name}': <none>");
                    var parts = new List<string>();
                    (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{sa.name}' output combo: {combo}");
                }

                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { }
                AppLogger.LogDebug($"SpecialAction Program: device={ctx.Device}, name={sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { }
        }

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (_logTrigger && sa != null && ctx != null) LogRising(ctx);

                if (sa == null || string.IsNullOrWhiteSpace(sa.details)) return;

                // Phase6-Step9b-3（F06）: DI 移行前（Mapping.cs の specActionLaunchProc、79170f73 の直前）の仕様へ戻した。
                // - extra に "$hidden"（大文字小文字不問）を含むときだけ、その語を 1 回取り除き、ウィンドウを非表示にして起動する。
                //   .bat/.cmd のときは %COMSPEC% /C "<details>" <引数> で起動する。
                // - それ以外は、OS の既定動作（UseShellExecute = true）で details を起動し、extra をそのまま引数に渡す
                //   （ショートカット・ドキュメント・関連付けによる起動を維持するため。非表示にしない）。
                string path = sa.details;
                string extra = sa.extra ?? string.Empty;
                string targetPath = path;
                string arguments = extra;
                bool hidden = false;

                int pos = extra.Length > 0 ? extra.IndexOf("$hidden", StringComparison.OrdinalIgnoreCase) : -1;
                if (pos >= 0)
                {
                    hidden = true;
                    string cmdArgs = extra.Remove(pos, "$hidden".Length);
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".bat" || ext == ".cmd")
                    {
                        targetPath = Environment.GetEnvironmentVariable("COMSPEC");
                        if (string.IsNullOrWhiteSpace(targetPath)) targetPath = "cmd.exe";
                        arguments = "/C \"" + path + "\" " + cmdArgs;
                    }
                    else
                    {
                        arguments = cmdArgs;
                    }
                }

                _launcher.Launch(targetPath, arguments, true, hidden);
                try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} target={targetPath} hidden={hidden}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"LaunchProcessAction.Execute failed: {ex}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
