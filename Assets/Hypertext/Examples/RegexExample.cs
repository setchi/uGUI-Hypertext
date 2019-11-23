/*
 * uGUI-Hypertext (https://github.com/setchi/uGUI-Hypertext)
 * Copyright (c) 2019 setchi
 * Licensed under MIT (https://github.com/setchi/uGUI-Hypertext/blob/master/LICENSE)
 */

using UnityEngine;

namespace Hypertext
{
    public class RegexExample : MonoBehaviour
    {
        [SerializeField] RegexHypertext text = default;

        const string RegexUrl = @"https?://(?:[!-~]+\.)+[!-~]+";
        const string RegexHashtag = @"[#＃][Ａ-Ｚａ-ｚA-Za-z一-鿆0-9０-９ぁ-ヶｦ-ﾟー]+";

        void Start()
        {
            text.OnClick(RegexUrl, Color.cyan, url => Debug.Log(url));
            text.OnClick(RegexHashtag, Color.green, hashtag => Debug.Log(hashtag));
        }
    }
}
