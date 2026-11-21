#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// ショートカット（.lnk）の指す先を調べる。状態を持たず、OS（Windows Script Host と Windows Installer）を呼ぶだけ。
    /// 自動プロファイルの対象探索と、スタートアップの登録の確認が、同じ解決を使うために 1 か所へ集めた。
    /// </summary>
    public static class ShortcutResolver
    {
        // Windows Script Host Shell Object
        private static readonly Guid ShellClassId = new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8");

        /// <summary>Windows Installer のショートカットを先に試し、だめなら通常の .lnk の指す先を返す。</summary>
        public static string? ResolveTarget(string filePath)
        {
            return ResolveMsiTarget(filePath) ?? ResolveLnkTarget(filePath);
        }

        /// <summary>.lnk の指す先を返す。有効なショートカットでないときは null。</summary>
        public static string? ResolveLnkTarget(string filePath)
        {
            return ReadShortcut(filePath, includeArguments: false);
        }

        /// <summary>.lnk の指す先と引数を、空白で続けた文字列で返す。有効なショートカットでないときは null。</summary>
        public static string? ResolveLnkTargetAndArguments(string filePath)
        {
            return ReadShortcut(filePath, includeArguments: true);
        }

        /// <summary>Windows Installer が管理するショートカットの実体のパス。ローカルに入っていないときは null。</summary>
        public static string? ResolveMsiTarget(string file)
        {
            StringBuilder product = new StringBuilder(NativeMethods2.MaxGuidLength + 1);
            StringBuilder feature = new StringBuilder(NativeMethods2.MaxFeatureLength + 1);
            StringBuilder component = new StringBuilder(NativeMethods2.MaxGuidLength + 1);

            NativeMethods2.MsiGetShortcutTarget(file, product, feature, component);

            int pathLength = NativeMethods2.MaxPathLength;
            StringBuilder path = new StringBuilder(pathLength);

            NativeMethods2.InstallState installState = NativeMethods2.MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
            return installState == NativeMethods2.InstallState.Local ? path.ToString() : null;
        }

        private static string? ReadShortcut(string filePath, bool includeArguments)
        {
            Type? t = Type.GetTypeFromCLSID(ShellClassId);
            dynamic shell = Activator.CreateInstance(t!)!; // 登録済みの COM クラスは必ず生成できる
            string? result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = includeArguments ? shortcut.TargetPath + " " + shortcut.Arguments : shortcut.TargetPath;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // 有効なショートカット（.lnk）でないファイルでは COMException が出る
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }
    }
}
