using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 歌曲UI元素的初始设置
    /// </summary>
    [Serializable]
    internal class SongItemConfig
    {
        internal float _songNameOriginalSizeX;
        internal float _artistOriginalSizeX;

        [SerializeField]
        [Range(0, 100)]
        internal int _displayNumPerPage = 20;

        internal Color _songNameOriginalColor;
        internal Color _artistOriginalColor;

        internal string _requestKeywords;

        internal int _enabledSongsCount;

        internal List<SongDetail> _songItems;

        //搜索结果的容器
        internal RectTransform _songResultContainer;
    }
}
