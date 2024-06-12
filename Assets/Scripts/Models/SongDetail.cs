using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// 储存搜索结果列表界面中的单个元素
    /// </summary>
    internal class SongDetail
    {
        internal GameObject _root;
        internal Button _play;
        internal Button _addList;
        internal Text _songName;
        internal Text _artist;
        internal Image _album;
    }
}
