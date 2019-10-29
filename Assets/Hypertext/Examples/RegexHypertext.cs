using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Hypertext
{
    public class RegexHypertext : HypertextBase
    {
        readonly List<Entry> entries = new List<Entry>();

        struct Entry
        {
            public readonly string RegexPattern;
            public readonly Color Color;
            public readonly Action<string> Callback;

            public Entry(string regexPattern, Color color, Action<string> callback)
            {
                RegexPattern = regexPattern;
                Color = color;
                Callback = callback;
            }
        }

        /// <summary>
        /// 正規表現にマッチした部分文字列にクリックイベントを登録します
        /// </summary>
        /// <param name="regexPattern">正規表現</param>
        /// <param name="onClick">クリック時のコールバック</param>
        public void OnClick(string regexPattern, Action<string> onClick)
        {
            OnClick(regexPattern, color, onClick);
        }

        /// <summary>
        /// 正規表現にマッチした部分文字列に色とクリックイベントを登録します
        /// </summary>
        /// <param name="regexPattern">正規表現</param>
        /// <param name="color">テキストカラー</param>
        /// <param name="onClick">クリック時のコールバック</param>
        public void OnClick(string regexPattern, Color color, Action<string> onClick)
        {
            if (string.IsNullOrEmpty(regexPattern) || onClick == null)
            {
                return;
            }

            entries.Add(new Entry(regexPattern, color, onClick));
        }

        public override void RemoveListeners()
        {
            base.RemoveListeners();
            entries.Clear();
        }

        /// <summary>
        /// イベントリスナを追加します
        /// テキストの変更などでイベントの再登録が必要なときにも呼び出されます
        /// <see cref="HypertextBase.OnClick"/> を使ってクリックイベントを登録してください
        /// </summary>
        protected override void AddListeners()
        {
            foreach (var entry in entries)
            {
#if UNITY_2019_1_OR_NEWER
                var _text = text.Replace(" ", "").Replace("\n", "");
#else
                var _text = text;
#endif
                foreach (Match match in Regex.Matches(_text, entry.RegexPattern))
                {
                    OnClick(match.Index, match.Value.Length, entry.Color, entry.Callback);
                }
            }
        }
    }
}
