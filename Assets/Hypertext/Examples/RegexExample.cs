using UnityEngine;

namespace Hypertext
{
    public class RegexExample : MonoBehaviour
    {
        [SerializeField]
        RegexHypertext text;

        const string RegexURL = "http(s)?://([\\w-]+\\.)+[\\w-]+(/[\\w- ./?%&=]*)?";
        const string RegexHashtag = "[#＃][Ａ-Ｚａ-ｚA-Za-z一-鿆0-9０-９ぁ-ヶｦ-ﾟー]+";

        void Start()
        {
            text.OnClick(RegexURL, Color.cyan, url => Debug.Log(url));
            text.OnClick(RegexHashtag, Color.green, hashtag => Debug.Log(hashtag));
        }
    }
}
