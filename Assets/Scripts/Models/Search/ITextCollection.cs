using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.Models.Search
{
    internal interface ITextCollection
    {
        public Text NameOne {  get; set; }
        public Text NameTwo { get; set; }
        public RectTransform TextMask { get; set; }
        public float OriginalSizeXOne { get; set; }
        public float OriginalSizeXTwo { get; set; }
    }
}
