using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// ����UIԪ�صĳ�ʼ����
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

        //�������������
        internal RectTransform _songResultContainer;
    }
}
