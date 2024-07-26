using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 储存搜索结果列表界面中的所有单个UI元素
    /// </summary>
    internal class SongDetail : ITextCollection
    {
        internal GameObject _root;
        internal Button _play;
        internal Button _addList;
        internal Image _album;

        public Text NameOne { get; set; }
        public Text NameTwo { get; set; }
        public RectTransform TextMask { get; set; }
        public float OriginalSizeXOne { get; set; }
        public float OriginalSizeXTwo { get; set; }
    }
}
