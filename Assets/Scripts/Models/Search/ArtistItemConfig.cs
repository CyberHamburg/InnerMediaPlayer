using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// ����UIԪ�صĳ�ʼ����
    /// </summary>
    internal class ArtistItemConfig
    {
        internal Vector3 _artistOriginalPosition;

        internal string _requestKeywords;

        internal ArtistDetail[] _artistItems;
        internal List<SongDetail> _songsItems;
    }
}
