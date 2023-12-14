using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using InnerMediaPlayer.Models.Signal;
using InnerMediaPlayer.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

#pragma warning disable IDE0051

namespace InnerMediaPlayer.UI
{
    internal class Lyric : UIViewerBase, IInitializable
    {
        private Lyrics _lyrics;
        private Transform _lyricContent;
        private Controller _controller;
        private CoroutineQueue _coroutineQueue;

        private float _highLightPositionResetTimer;
        private const float HighLightPositionResetTimer = 2f;

        internal LyricDisplaySignal LyricDisplaySignal { get; private set; }
        internal LyricInterruptDisplaySignal LyricInterruptDisplaySignal { get; set; }

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
        private void Initialized(Lyrics lyrics, CoroutineQueue queue)
        {
            _lyrics = lyrics;
            _coroutineQueue = queue;
        }

        public void Initialize()
        {
            LyricDisplaySignal = new LyricDisplaySignal
            {
                Func = DisplayLyric,
                CallBack = StopDisplayByInterruptTask
            };
            LyricInterruptDisplaySignal = new LyricInterruptDisplaySignal
            {
                Func = DisplayByInterruptAsync
            };

            Signal.SubscribeId<LyricDisplaySignal>("Normal", _lyrics.taskQueue.Binder);
            Signal.SubscribeId<LyricInterruptDisplaySignal>("Interruption", _lyrics.interruptTaskQueue.Binder);
        }

        private void Start()
        {
            AddEventTriggerInterface(Control.scrollRect.gameObject, EventTriggerType.BeginDrag, BeginDrag);
            AddEventTriggerInterface(Control.scrollRect.gameObject, EventTriggerType.EndDrag, EndDrag);
        }

        private void OnDestroy()
        {
            Signal.UnsubscribeId<LyricDisplaySignal>("Normal", _lyrics.taskQueue.Binder);
            Signal.UnsubscribeId<LyricInterruptDisplaySignal>("Interruption", _lyrics.interruptTaskQueue.Binder);
        }

        private void BeginDrag(BaseEventData eventData)
        {
            _controller._needScrollAutomatically = false;
            _controller.scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        private void EndDrag(BaseEventData eventData)
        {
            _controller._needScrollAutomatically = true;
            _controller.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _coroutineQueue.Run(HighLightPositionReset);
        }

        private IEnumerator HighLightPositionReset(CancellationToken token)
        {
            _highLightPositionResetTimer = default;
            while (_highLightPositionResetTimer < HighLightPositionResetTimer)
            {
                while (!_controller._needScrollAutomatically)
                {
                    yield return null;
                }
                _highLightPositionResetTimer += Time.deltaTime;
                yield return null;
                if(token.IsCancellationRequested)
                    yield break;
            }

            _controller.contentTransform.anchoredPosition =
                new Vector2(_controller.contentTransform.anchoredPosition.x, _lyrics.ContentPosY);
        }

        /// <summary>
        /// 从开始时完整展示歌词
        /// </summary>
        /// <param name="id">歌曲id</param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task DisplayLyric(int id, CancellationToken token) => _lyrics.DisplayAsync(id, Control, token);

        /// <summary>
        /// 在特定时间点开始展示歌词
        /// </summary>
        /// <param name="time">在此时间点开始展示歌词</param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task DisplayByInterruptAsync(float time, CancellationToken token) => _lyrics.DisplayByInterruptAsync(time, Control, token);

        /// <summary>
        /// 停止正常展示歌词任务队列的运行
        /// </summary>
        internal void StopNormalDisplayTask() => _lyrics.taskQueue.Stop();

        internal void StopDisplayByInterruptTask() => _lyrics.interruptTaskQueue.Stop();

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
            /// <summary>
            /// 歌词Panel下ScrollRect组件
            /// </summary>
            internal readonly ScrollRect scrollRect;
            internal readonly RectTransform scrollViewTransform;
            internal readonly RectTransform contentTransform;
            /// <summary>
            /// 歌词背景图片
            /// </summary>
            internal readonly Image image;
            internal readonly VerticalLayoutGroup verticalLayoutGroup;
            internal readonly Color originalBackgroundColor;
            /// <summary>
            /// 需要脚本控制滚动歌词吗？
            /// </summary>
            internal bool _needScrollAutomatically;

            internal float VerticalSpacing => verticalLayoutGroup.spacing;

            public Controller(ScrollRect scrollRect, bool needScrollAutomatically)
            {
                this.scrollRect = scrollRect;
                _needScrollAutomatically = needScrollAutomatically;
                image = scrollRect.GetComponent<Image>();
                originalBackgroundColor = image.color;
                verticalLayoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                contentTransform = scrollRect.content;
                scrollViewTransform = (RectTransform)scrollRect.transform;
            }
        }
    }
}