using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    [Serializable]
    internal class UIItemConfig
    {
        internal string _requestKeywords;
        internal float _textOriginalSizeX;
        internal int _enabledItemsCount;
        [SerializeField]
        [Range(0, 50)]
        protected internal int _displayNumPerPage = 15;

        internal CellDetail[] _items;
        internal List<SongDetail> _songsItems;

        internal RectTransform _resultContainer;
        internal RectTransform _songContainer;

        internal Button _returnLastPanel;
    }
}
