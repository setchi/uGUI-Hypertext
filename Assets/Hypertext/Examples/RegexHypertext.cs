using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class RegexHypertext : HypertextBase
{
    readonly Dictionary<string, RegexEntry> _regexEntryTable = new Dictionary<string, RegexEntry>();

    struct RegexEntry
    {
        public string Pattern;
        public Color Color;
        public Action<string> OnClick;

        public RegexEntry(string pattern, Color color, Action<string> onClick)
        {
            Pattern = pattern;
            Color = color;
            OnClick = onClick;
        }
    }

    /// <summary>
    /// 正規表現にマッチした部分文字列にクリックイベントを登録します
    /// </summary>
    /// <param name="regexPattern">正規表現</param>
    /// <param name="onClick">クリック時のコールバック</param>
    public void SetClickableByRegex(string regexPattern, Action<string> onClick)
    {
        SetClickableByRegex(regexPattern, color, onClick);
    }

    /// <summary>
    /// 正規表現にマッチした部分文字列に色とクリックイベントを登録します
    /// </summary>
    /// <param name="regexPattern">正規表現</param>
    /// <param name="color">正規表現でマッチしたテキストの色</param>
    /// <param name="onClick">クリック時のコールバック</param>
    public void SetClickableByRegex(string regexPattern, Color color, Action<string> onClick)
    {
        if (string.IsNullOrEmpty(regexPattern) || onClick == null)
        {
            return;
        }

        _regexEntryTable[regexPattern] = new RegexEntry(regexPattern, color, onClick);
    }

    public override void RemoveClickable()
    {
        base.RemoveClickable();
        _regexEntryTable.Clear();
    }

    /// <summary>
    /// テキストの変更などでクリックする文字位置の再計算が必要なときに呼び出されます
    /// 親の RegisterClickable メソッドを使ってクリック対象文字の情報を登録してください
    /// </summary>
    protected override void RegisterClickable()
    {
        foreach (var regexEntry in _regexEntryTable.Values)
        {
            foreach (Match m in Regex.Matches(text, regexEntry.Pattern))
            {
                RegisterClickable(m.Index, m.Value.Length, regexEntry.Color, regexEntry.OnClick);
            }
        }
    }
}
