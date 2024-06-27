using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 歌曲UI元素的初始设置
    /// </summary>
    internal class SongItemConfig
    {
        internal float _songNameOriginalSizeX;
        internal float _artistOriginalSizeX;

        internal Color _songNameOriginalColor;
        internal Color _artistOriginalColor;

        internal string _requestKeywords;

        internal List<SongDetail> _songItems;
    }
}
