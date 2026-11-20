using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DS4Windows.Core.Logging;

namespace DS4Windows.Composition
{
    public static class AppHost
    {
        private static readonly System.Collections.Generic.HashSet<System.Type> _loggedResolvedTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static IHost? _host;
        private static readonly object _syncLock = new object();

        public static IHost? Host => _host;

        /// <summary>
        /// ホストがどの経路で作られたかの記録。
        /// ホスト構築の時点では、ログの最小レベルがまだ反映されておらず Trace が捨てられるため、
        /// App.xaml.cs が、ロガー初期化後に、Info で出力する。
        /// GetService による暗黙の構築を廃止したため、値は
        /// 「explicit CreateHost(...) created the host」か「host already existed」の 2 種類だけになる
        /// （呼び出し元スタックの組み立ては削除した。プロパティとログは、ユーザー決定により維持する）。
        /// </summary>
        public static string? CreationNote { get; private set; }

        // 本体の起動は CreateHost(configuration, parser) を使う。引数なしの本メソッドはテストが明示的に呼ぶ
        // （GetService は暗黙にホストを作らない。テストは TestHost 経由で共有のホストを作る）。
        public static IHost CreateHost()
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                    return _host;

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        DS4Windows.Composition.ServiceRegistration.RegisterServices(services);
                    });

                _host = builder.Build();
                PreallocateActionEntries();
                CreationNote = "explicit CreateHost() created the host";
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized and all services registered");
                return _host;
            }
        }

        /// <summary>
        /// 起動引数つきでホストを作る（本体の起動と -driverinstall 分岐が使う）。
        /// コンテナの構築より前に作ったブートストラップのサービスを渡すと、同じインスタンスをコンテナへ登録する
        /// （Pre-Host の起動処理と、コンテナから解決するサービスが同じインスタンスになる）。ホストが構築済みのときは、登録は変えられない。
        /// </summary>
        public static IHost CreateHost(IConfiguration configuration, DS4Windows.Core.Utilities.ArgumentParser parser,
            DS4Windows.Core.Abstractions.IPathService? bootstrapPathService = null,
            DS4Windows.Core.Abstractions.IEnvironmentService? bootstrapEnvironmentService = null,
            DS4Windows.Core.Settings.BackingStore? bootstrapBackingStore = null)
        {
            lock (_loggedResolvedTypes) { _loggedResolvedTypes.Clear(); }
            lock (_syncLock)
            {
                if (_host != null)
                {
                    // 構築済みのコンテナには登録を追加できないため、
                    // 登録済みの IStartupArguments へ起動引数を設定する。
                    // 通常の起動では、App.xaml.cs の明示的な CreateHost(config, parser) がホストを作る
                    // （Global の静的初期化は、ホストを作らない）。
                    // この分岐に来るのは、それより前に GetService が呼ばれた場合。
                    // -driverinstall 分岐も、設定の読込の前に、この CreateHost(config, parser) を明示的に呼ぶ
                    // ようにした（旧: Global.Load() の中の GetService が、引数なしでホストを暗黙に作っていた。K7-1 の残存経路）。
                    CreationNote = CreationNote ?? "host already existed (creator not recorded)";
                    ApplyStartupArguments(parser, hostAlreadyCreated: true);
                    return _host;
                }

                var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        // ブートストラップの BackingStore は、登録の中で（IProcessInspector などの配線つきで）コンテナへ登録する
                        DS4Windows.Composition.ServiceRegistration.RegisterServices(services, bootstrapBackingStore);
                        services.AddSingleton(parser);
                        // 後から登録した実装が、同じ型の解決で優先される（既定の登録を、ブートストラップのインスタンスで置き換える）
                        if (bootstrapPathService != null)
                            services.AddSingleton<DS4Windows.Core.Abstractions.IPathService>(bootstrapPathService);
                        if (bootstrapEnvironmentService != null)
                            services.AddSingleton<DS4Windows.Core.Abstractions.IEnvironmentService>(bootstrapEnvironmentService);
                    });

                _host = builder.Build();
                PreallocateActionEntries();
                CreationNote = "explicit CreateHost(config, parser) created the host";
                ApplyStartupArguments(parser, hostAlreadyCreated: false);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppHost.CreateHost: Host initialized with runtime parser");
                return _host;
            }
        }

        /// <summary>
        /// 起動引数を、登録済みの <see cref="DS4Windows.Core.Abstractions.IStartupArguments"/> へ設定する。
        /// ControlService の生成（ServiceRegistration）はここで設定された値を使う。
        /// </summary>
        private static void ApplyStartupArguments(DS4Windows.Core.Utilities.ArgumentParser parser, bool hostAlreadyCreated)
        {
            if (parser == null)
                return;

            var startupArguments = _host?.Services.GetService<DS4Windows.Core.Abstractions.IStartupArguments>();
            if (startupArguments == null)
                return;

            startupArguments.SetParser(parser);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppHost.CreateHost: startup arguments applied to IStartupArguments (host {(hostAlreadyCreated ? "already created" : "newly created")})");
        }

        private static void PreallocateActionEntries()
        {
            try
            {
                var manager = _host?.Services.GetService<DS4Windows.Processing.Actions.DefaultActionManager>();
                manager?.PreallocateEntries();
            }
            catch { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
        }

        // ホストが未構築（または Dispose 済み）のときは、暗黙にホストを作らず null を返す。
        public static T? GetService<T>() where T : class
        {
            if (_host == null)
            {
                LogNoHost(typeof(T));
                return default;
            }

            var service = _host?.Services.GetService<T>();
            if (AppLogger.IsTraceEnabled)
            {
                lock (_loggedResolvedTypes)
                {
                    if (_loggedResolvedTypes.Add(typeof(T)))
                    {
                        AppLogger.LogTrace($"[DI] AppHost.GetService: Resolved {typeof(T).Name}");
                    }
                }
            }
            return service;
        }

        public static object? GetService(Type serviceType)
        {
            if (_host == null)
            {
                LogNoHost(serviceType);
                return null;
            }

            var service = _host?.Services.GetService(serviceType);
            if (AppLogger.IsTraceEnabled)
            {
                lock (_loggedResolvedTypes)
                {
                    if (_loggedResolvedTypes.Add(serviceType))
                    {
                        AppLogger.LogTrace($"[DI] AppHost.GetService: Resolved {serviceType.Name}");
                    }
                }
            }
            return service;
        }

        private static void LogNoHost(Type serviceType)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppHost: no host (not created or already disposed); returning null for {serviceType.Name}");
        }

        public static void Dispose()
        {
            lock (_syncLock)
            {
                if (_host != null)
                {
                    try
                    {
                        _host.Dispose();
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace("[DI] AppHost.Dispose: Host disposed");
                    }
                    catch { /* 診断の出力の失敗は無視してよい */ }
                    finally
                    {
                        _host = null;
                    }
                }
            }
        }
    }
}
