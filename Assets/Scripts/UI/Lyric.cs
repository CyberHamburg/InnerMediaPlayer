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
        private Mediator _mediator;
        private CoroutineQueue _coroutineQueue;
        private WaitForEndOfFrame _waitForEndOfFrame;
        private WaitForSeconds _waitForSeconds;

        private float _highLightPositionResetTimer;
        private const float HighLightPositionResetTimer = 2f;

        internal LyricDisplaySignal LyricDisplaySignal { get; set; }
        internal LyricInterruptDisplaySignal LyricInterruptDisplaySignal { get; set; }

        private Mediator Controller
        {
            get
            {
                if (_mediator != null)
                    return _mediator;
                ScrollRect scrollRect = FindGameObjectInList("Scroll View", null).GetComponent<ScrollRect>();
                _mediator = new Mediator(scrollRect, true);
                return _mediator;
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

            Signal.SubscribeId<LyricDisplaySignal>(DisplayLyricWays.Normal, _lyrics.taskQueue.Binder);
            Signal.SubscribeId<LyricInterruptDisplaySignal>(DisplayLyricWays.Interrupted, _lyrics.interruptTaskQueue.Binder);
            _waitForEndOfFrame = new WaitForEndOfFrame();
            _waitForSeconds = new WaitForSeconds(HighLightPositionResetTimer);
        }

        private void Start()
        {
            AddEventTriggerInterface(Controller.scrollRect.gameObject, EventTriggerType.BeginDrag, BeginDrag);
            AddEventTriggerInterface(Controller.scrollRect.gameObject, EventTriggerType.EndDrag, EndDrag);
        }

        private void OnDestroy()
        {
            Signal.UnsubscribeId<LyricDisplaySignal>(DisplayLyricWays.Normal, _lyrics.taskQueue.Binder);
            Signal.UnsubscribeId<LyricInterruptDisplaySignal>(DisplayLyricWays.Interrupted, _lyrics.interruptTaskQueue.Binder);
        }

        private void BeginDrag(BaseEventData eventData)
        {
            _mediator._needScrollAutomatically = false;
            _mediator.scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        private async void EndDrag(BaseEventData eventData)
        {
            _mediator.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            if (_mediator._needHighLightPositionAutoReset)
                _coroutineQueue.Run(HighLightPositionReset);
            await _waitForSeconds;
            _mediator._needScrollAutomatically = true;
        }

        private IEnumerator HighLightPositionReset(CancellationToken token)
        {
            _highLightPositionResetTimer = default;
            while (_highLightPositionResetTimer < HighLightPositionResetTimer)
            {
                while (!_mediator._needScrollAutomatically)
                {
                    yield return null;
                }
                _highLightPositionResetTimer += Time.deltaTime;
                yield return null;
                if(token.IsCancellationRequested)
                    yield break;
            }

            _mediator.contentTransform.anchoredPosition =
                new Vector2(_mediator.contentTransform.anchoredPosition.x, _lyrics.ContentPosY);
        }

        internal async void SwitchControl()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            await _waitForEndOfFrame;
            _lyrics.ResetContentPosY(_mediator);
        }

        /// <summary>
        /// 从开始时完整展示歌词
        /// </summary>
        /// <param name="id">歌曲id</param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task DisplayLyric(int id, CancellationToken token) => _lyrics.DisplayAsync(id, Controller, token);

        /// <summary>
        /// 在特定时间点开始展示歌词
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task DisplayByInterruptAsync(CancellationToken token) => _lyrics.DisplayByInterruptAsync(Controller, token);

        /// <summary>
        /// 停止正常展示歌词任务队列的运行
        /// </summary>
        internal void StopNormalDisplayTask() => _lyrics.taskQueue.Stop();

        internal void StopDisplayByInterruptTask() => _lyrics.interruptTaskQueue.Stop();

        internal Task InstantiateLyricAsync(int id, Texture2D album) => _lyrics.InstantiateLyricAsync(id, Controller, album);

        internal void SetDefaultColor() => Controller.image.color = Controller.originalBackgroundColor;

        internal void Dispose(int id) => _lyrics.Dispose(id);

        internal void Disable(int id) => _lyrics.SetActive(id, false);

        internal class Mediator
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
            /// <summary>
            /// 需要歌词自动复位到正在播放的位置吗？
            /// </summary>
            internal bool _needHighLightPositionAutoReset;

            /// <summary>
            /// 歌词所在的panel原本是关闭的吗
            /// </summary>
            private bool _originalActiveSelf;
            /// <summary>
            /// 歌词背景原本的不透明度
            /// </summary>
            private float _originalAlpha;
            
            internal float VerticalSpacing => verticalLayoutGroup.spacing;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="scrollRect"></param>
            /// <param name="needScrollAutomatically"></param>
            internal Mediator(ScrollRect scrollRect, bool needScrollAutomatically)
            {
                this.scrollRect = scrollRect;
                _needScrollAutomatically = needScrollAutomatically;
                image = scrollRect.GetComponent<Image>();
                originalBackgroundColor = image.color;
                verticalLayoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                contentTransform = scrollRect.content;
                scrollViewTransform = (RectTransform)scrollRect.transform;
                _needHighLightPositionAutoReset = true;
            }
        }
    }
}