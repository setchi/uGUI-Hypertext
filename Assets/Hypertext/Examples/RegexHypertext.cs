using System;
using System.Collections.Generic;
using System.Linq;
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
            var lineCount = text.Count(x => x == '\n') + 1;
            bool hasFolded = lineCount != cachedTextGenerator.lineCount;

            foreach (var entry in entries)
            {
                foreach (Match match in Regex.Matches(text, entry.RegexPattern))
                {
#if UNITY_2019_1_OR_NEWER
                    if (hasFolded)
                    {
                        OnClick(match.Index, match.Value.Length, entry.Color, entry.Callback);
                    }
                    else
                    {
                        // 折り返していない場合のみ「空白」と「改行」が描画されないため、調整する
                        var head = text.Substring(0, match.Index);
                        var count = head.Count(x => x == ' ') + head.Count(x => x == '\n');
                        OnClick(match.Index - count, match.Value.Length, entry.Color, entry.Callback);
                    }
#else
                    OnClick(match.Index, match.Value.Length, entry.Color, entry.Callback);
#endif
                }
            }
        }
    }
}
