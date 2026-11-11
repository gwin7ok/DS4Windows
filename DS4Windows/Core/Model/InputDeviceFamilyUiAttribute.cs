using System;

namespace DS4Windows.Core.Model
{
    /// <summary>
    /// <see cref="InputDeviceFamily"/> の値に付ける、「この機種を使う」の画面の文言のリソースキー（<c>Strings.resx</c>）。
    /// 設定画面のチェックボックスと、はじめの起動画面の説明は、系統の一覧とこの属性から作られる（系統を足すときは、enum の値にこの属性を付ける）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InputDeviceFamilyUiAttribute : Attribute
    {
        public InputDeviceFamilyUiAttribute(string supportLabelKey, string firstLaunchLabelKey, string firstLaunchTextKey, string helpUrl = null)
        {
            SupportLabelKey = supportLabelKey;
            FirstLaunchLabelKey = firstLaunchLabelKey;
            FirstLaunchTextKey = firstLaunchTextKey;
            HelpUrl = helpUrl;
        }

        /// <summary>設定画面のチェックボックスの文言。</summary>
        public string SupportLabelKey { get; }

        /// <summary>はじめの起動画面のチェックボックスの文言。</summary>
        public string FirstLaunchLabelKey { get; }

        /// <summary>はじめの起動画面の説明文。</summary>
        public string FirstLaunchTextKey { get; }

        /// <summary>はじめの起動画面に出す補足のリンク（なければ null）。</summary>
        public string HelpUrl { get; }
    }
}
