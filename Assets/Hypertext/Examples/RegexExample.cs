using UnityEngine;

public class RegexExample : MonoBehaviour
{
    [SerializeField]
    RegexHypertext _text;

    const string _urlPattern = "http(s)?://([\\w-]+\\.)+[\\w-]+(/[\\w- ./?%&=]*)?";
    const string _hashtagPattern = "[#＃][Ａ-Ｚａ-ｚA-Za-z一-鿆0-9０-９ぁ-ヶｦ-ﾟー]+";

    void Start()
    {
        _text.SetClickableByRegex(_urlPattern, Color.cyan, url => Debug.Log(url));
        _text.SetClickableByRegex(_hashtagPattern, Color.green, hashtag => Debug.Log(hashtag));
    }
}
