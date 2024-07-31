using System;
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

        private bool _needScrollAutomatically;
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
            _needScrollAutomatically = false;
            _mediator._needScrollAutomatically = false;
            _mediator.scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        private void EndDrag(BaseEventData eventData)
        {
            _mediator.scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            if (_mediator._needHighLightPositionAutoReset)
                _coroutineQueue.Run(HighLightPositionReset);
            else
                _mediator._needScrollAutomatically = true;
            _needScrollAutomatically = true;
        }

        private IEnumerator HighLightPositionReset(Tools.CancellationTokenSource token, IProgress<TaskStatus> progress)
        {
            progress.Report(TaskStatus.Running);
            _highLightPositionResetTimer = default;
            while (_highLightPositionResetTimer < HighLightPositionResetTimer)
            {
                while (!_needScrollAutomatically && !_mediator._needScrollAutomatically)
                {
                    yield return null;
                }

                _highLightPositionResetTimer += Time.deltaTime;
                yield return null;
                if(token.IsCancellationRequested)
                {
                    progress.Report(TaskStatus.Canceled);
                    yield break;
                }
            }

            _mediator._needScrollAutomatically = true;
            _mediator.contentTransform.anchoredPosition =
                new Vector2(_mediator.contentTransform.anchoredPosition.x, _lyrics.ContentPosY);
            progress.Report(TaskStatus.RanToCompletion);
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
        internal Task DisplayLyric(int id, Tools.CancellationTokenSource token, IProgress<TaskStatus> progress) => _lyrics.DisplayAsync(id, Controller, token, progress);

        /// <summary>
        /// 在特定时间点开始展示歌词
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal Task DisplayByInterruptAsync(Tools.CancellationTokenSource token, IProgress<TaskStatus> progress) => _lyrics.DisplayByInterruptAsync(Controller, token, progress);

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

            internal float VerticalSpacing => verticalLayoutGroup.spacing;

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