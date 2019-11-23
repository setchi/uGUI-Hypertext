/*
 * uGUI-Hypertext (https://github.com/setchi/uGUI-Hypertext)
 * Copyright (c) 2019 setchi
 * Licensed under MIT (https://github.com/setchi/uGUI-Hypertext/blob/master/LICENSE)
 */

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
        /// 正規表現にマッチした部分文字列にクリックイベントリスナを登録します
        /// </summary>
        /// <param name="regexPattern">正規表現</param>
        /// <param name="onClick">クリック時のコールバック</param>
        public void OnClick(string regexPattern, Action<string> onClick)
        {
            OnClick(regexPattern, color, onClick);
        }

        /// <summary>
        /// 正規表現にマッチした部分文字列に色とクリックイベントリスナを登録します
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
        /// <see cref="HypertextBase.OnClick"/> を使ってクリックイベントリスナを登録してください
        /// </summary>
        protected override void AddListeners()
        {
            foreach (var entry in entries)
            {
                foreach (Match match in Regex.Matches(text, entry.RegexPattern))
                {
                    OnClick(match.Index, match.Value.Length, entry.Color, entry.Callback);
                }
            }
        }
    }
}
