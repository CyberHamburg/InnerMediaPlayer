using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 艺人UI元素的初始设置
    /// </summary>
    internal class ArtistItemConfig
    {
        internal Vector3 _artistOriginalPosition;

        internal string _requestKeywords;

        internal ArtistDetail[] _artistItems;
        internal List<SongDetail> _songsItems;
    }
}
