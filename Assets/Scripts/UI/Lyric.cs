using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace InnerMediaPlayer.UI
{
    internal class Lyric : UIViewerBase
    {
        private Lyrics _lyrics;
        private Transform _lyricContent;
        private Controller _controller;

        private Controller Control
        {
            get
            {
                if (_controller != null)
                    return _controller;
                ScrollRect scrollRect = FindGameObjectInList("Scroll View", null).GetComponent<ScrollRect>();
                _controller = new Controller(scrollRect, true);
                return _controller;
            }
        }

        [Inject]
        private void Initialize(Lyrics lyrics)
        {
            _lyrics = lyrics;
        }

        private void Start()
        {
            AddEventTriggerInterface(Control.scrollRect.gameObject, EventTriggerType.BeginDrag, BeginDrag);
            AddEventTriggerInterface(Control.scrollRect.gameObject, EventTriggerType.EndDrag, EndDrag);
        }

        private void BeginDrag(BaseEventData eventData)
        {
            _controller._needScroll = false;
            _controller.scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        private void EndDrag(BaseEventData eventData)
        {
            _controller._needScroll = true;
            _controller.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        }

        internal Task DisplayLyric(int id,CancellationToken token)
        {
            return _lyrics.DisplayLyricAsync(id, Control, token);
        }

        internal Task InstantiateLyric(int id, Texture2D album)
        {
            if (_lyricContent == null)
                _lyricContent = FindGameObjectInList("Content", "Viewport").transform;
            return _lyrics.InstantiateLyricAsync(id, _lyricContent, Control, album);
        }

        internal void SetDefaultColor()
        {
            Control.image.color = Control.originalBackgroundColor;
        }

        internal void Dispose(int id) => _lyrics.Dispose(id);

        internal void Disable(int id) => _lyrics.Disable(id);

        internal class Controller
        {
            internal readonly ScrollRect scrollRect;
            internal readonly RectTransform scrollViewTransform;
            internal readonly RectTransform contentTransform;
            internal readonly Image image;
            internal readonly VerticalLayoutGroup verticalLayoutGroup;
            internal readonly Color originalBackgroundColor;
            internal bool _needScroll;

            internal float VerticalSpacing => verticalLayoutGroup.spacing;

            public Controller(ScrollRect scrollRect, bool needScroll)
            {
                this.scrollRect = scrollRect;
                _needScroll = needScroll;
                image = scrollRect.GetComponent<Image>();
                originalBackgroundColor = image.color;
                verticalLayoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                contentTransform = scrollRect.content;
                scrollViewTransform = (RectTransform)scrollRect.transform;
            }
        }
    }
}