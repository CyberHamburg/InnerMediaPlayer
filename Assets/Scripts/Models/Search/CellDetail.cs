using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    /// <summary>
    /// �����������б���������е���UIԪ��
    /// </summary>
    internal class CellDetail : ITextCollection
    {
        internal GameObject _root;
        internal Image _image;
        internal Button _click;

        public Text NameOne { get; set; }
        public Text NameTwo { get; set; }
        public RectTransform TextMask { get; set; }
        public float OriginalSizeXOne {  get; set; }
        public float OriginalSizeXTwo { get; set; }
    }
}
