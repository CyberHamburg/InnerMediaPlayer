using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 存放搜索结果列表界面中所有单个UI元素
    /// </summary>
    internal class CellDetail
    {
        internal GameObject _root;
        internal Image _image;
        internal Text _text;
        internal RectTransform _textMask;
        internal Button _click;
    }
}
