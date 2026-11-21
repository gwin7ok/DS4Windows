#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DS4WindowsTests
{
    /// <summary>
    /// 任意のインターフェースの偽物を作る。呼ばれたメソッドの名前と引数を記録し、戻り値は型の既定値（文字列は空）を返す。
    /// 転送だけをする部品の検査で、本物の相手の代わりに差す（モックのライブラリを足さずに済ませる）。
    /// </summary>
    public class RecordingProxy : DispatchProxy
    {
        private readonly List<(string Name, object?[] Args)> _calls = new List<(string, object?[])>();

        public IReadOnlyList<(string Name, object?[] Args)> Calls => _calls;

        /// <summary>呼び出しを記録した直後に、メソッド名を渡して呼ぶ。呼び出された時点の状態の検査や、例外の注入に使う。</summary>
        public Action<string>? OnCall { get; set; }

        /// <summary>プロパティの値を指定する（<c>get_Xxx</c> が返す）。</summary>
        public Dictionary<string, object?> Returns { get; } = new Dictionary<string, object?>();

        public static T Create<T>() where T : class
        {
            return DispatchProxy.Create<T, RecordingProxy>();
        }

        public static RecordingProxy Of(object proxy) => (RecordingProxy)proxy;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            string name = targetMethod!.Name;   // DispatchProxy は常に対象のメソッドを渡す
            _calls.Add((name, args ?? Array.Empty<object?>()));
            OnCall?.Invoke(name);
            if (Returns.TryGetValue(name, out object? value)) return value;
            Type returnType = targetMethod.ReturnType;
            if (returnType == typeof(void)) return null;
            if (returnType == typeof(string)) return string.Empty;
            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }

        /// <summary>メソッド名ごとの呼び出しの引数の列。</summary>
        public IEnumerable<object?[]> ArgsOf(string name) => _calls.Where(c => c.Name == name).Select(c => c.Args);
    }
}
