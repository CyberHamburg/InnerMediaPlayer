using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Models.Lyric;
using InnerMediaPlayer.Tools;
using LitJson;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Debug = UnityEngine.Debug;
using Network = InnerMediaPlayer.Tools.Network;

#pragma warning disable IDE0041

namespace InnerMediaPlayer.Logical
{
    internal class Lyrics
    {
        private readonly PlayingList _playingList;
        private readonly Line.Factory _factory;
        private readonly Network _network;
        private readonly Cookies _cookies;
        private readonly TextGenerator _textGenerator;
        //每句歌词时长的计时器
        private readonly Stopwatch _stopwatch;
        //暂停期间需要减去的计时器数值
        private readonly Stopwatch _minusStopwatch;
        /// <summary>
        /// 歌词正常播放时所加入的任务队列
        /// </summary>
        internal readonly TaskQueue<int> taskQueue;
        /// <summary>
        /// 当需要调整歌词进度时所使用的任务队列
        /// </summary>
        internal readonly TaskQueue<float> interruptTaskQueue;
        //当前高亮的歌词颜色
        private readonly Color _defaultPlayingColor;
        //不在演奏的歌词颜色
        private readonly Color _defaultNotPlayingColor;
        //当背景颜色与前面定义的正在演奏高亮的歌词颜色相类似时所采用的备用颜色
        private readonly Color _sparePlayingColor;
        //当背景颜色与前面定义的不在演奏的歌词颜色相类似时所采用的备用颜色
        private readonly Color _spareNotPlayingColor;

        //以歌曲id为key,Lyric为value的字典
        private readonly Dictionary<int, Lyric> _lyrics;

        //正在高亮展示的那句歌词，在调整歌词进度时判断是否需要取消原高亮展示
        private Line _highLightLyric;

        //正在播放的歌词所属歌曲id
        private int _rollingLyricsId;

        //未加翻译歌词文本的原本高度
        private float _lyricHeight;
        //加入翻译后歌词文本的高度
        private float _translationLyricHeight;
        //加入翻译后的行距
        private float _translationLyricLineSpacing;
        //加入翻译后歌词文本的高度和未加翻译歌词文本的高度乘数
        private const float TranslationHeightMultiplier = 1.34f;
        //加入翻译后行距乘数
        private const float TranslationLineSpacingMultiplier = 1.3f;
        //当色差小于此数时判断为颜色类似
        private const float ColorDistanceLimit = 110f;

        internal float ContentPosY { get; private set; }

        internal Lyrics(PlayingList playingList,Line.Factory factory, Network network, Cookies cookies,
            TextGenerator textGenerator, TaskQueue<int, CancellationToken> taskQueue,
            TaskQueue<float> interruptTaskQueue)
        {
            _playingList = playingList;
            _factory = factory;
            _network = network;
            _cookies = cookies;
            _textGenerator = textGenerator;
            _defaultPlayingColor = new Color32(235, 235, 235, 255);
            _defaultNotPlayingColor = Color.black;
            _sparePlayingColor = new Color32(125, 125, 125, 255);
            _spareNotPlayingColor = new Color32(125, 125, 125, 255);
            _lyrics = new Dictionary<int, Lyric>(20);
            _stopwatch = new Stopwatch();
            _minusStopwatch = new Stopwatch();
            this.taskQueue = taskQueue;
            this.interruptTaskQueue = interruptTaskQueue;
        }

        private List<Line> PrepareData(string lyric, string translationLyric, Color notPlayingColor, Transform content, UI.Lyric.Controller controller)
        {
            List<Line> list = new List<Line>();

            //TODO:1.歌词在超出行宽后自动换行。2.纯文本歌词多行显示
            #region 纯文本歌词处理

            //不包含]则表示没有时间轴，是纯文本歌词
            if (!lyric.Contains(']'))
            {
                Line line = _factory.Create(0f, lyric, notPlayingColor, content);
                line._timeInterval = 0f;
                line.SetSiblingIndex(0);
                list.Add(line);
                //因为preferredHeight在修改Text的文本后返回的值不准确，所以只能借助textGenerator计算高度
                float height = _textGenerator.GetPreferredHeight(line._text.text,
                    line._text.GetGenerationSettings(controller.scrollRect.viewport.sizeDelta));
                line._text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                //加空行的目的是看起来没有滚动的效果
                Line emptyLine = _factory.Create(0.1f, string.Empty, notPlayingColor, content);
                emptyLine._timeInterval = 0.1f;
                emptyLine.SetSiblingIndex(1);
                list.Add(emptyLine);

                return list;
            }

            #endregion

            ParseLyric(list, lyric, notPlayingColor, content);
            list.Sort();

            for (int i = 0; i < list.Count; i++)
            {
                list[i].SetSiblingIndex(i);
                if (i == 0)
                    list[i]._timeInterval = list[i]._time;
                else
                    list[i]._timeInterval = list[i]._time - list[i - 1]._time;
            }

            if (string.IsNullOrEmpty(translationLyric))
                return list;

            #region 处理翻译以及歌词UI大小

            List<(float time, string lyric)> translationList = new List<(float time, string lyric)>();
            ParseTranslation(translationList, translationLyric);
            translationList.Sort();

            //计算加入翻译后的歌词ui的高度和text组件的lineSpacing大小
            if (_lyricHeight == 0f || _translationLyricHeight == 0f && list.Count > 0)
            {
                _lyricHeight = list[0]._text.rectTransform.sizeDelta.y;
                _translationLyricHeight = _lyricHeight * TranslationHeightMultiplier + controller.VerticalSpacing;
                _translationLyricLineSpacing = list[0]._text.lineSpacing * TranslationLineSpacingMultiplier;
            }

            int index = 0;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Line line in list)
            {
                if (index >= translationList.Count ||
                    !Mathf.Approximately(line._time, translationList[index].time)) continue;
                stringBuilder.AppendLine(line._text.text);
                stringBuilder.Append(translationList[index++].lyric);
                line._text.text = stringBuilder.ToString();
                RectTransform rectTransform = line._text.rectTransform;
                rectTransform.sizeDelta = rectTransform.sizeDelta.x * Vector2.right +
                                          _translationLyricHeight * Vector2.up;
                line._text.lineSpacing = _translationLyricLineSpacing;
                line._height = _translationLyricHeight;
                stringBuilder.Clear();
            }

            #endregion

            return list;
        }

        /// <summary>
        /// 将歌词解析为数据
        /// </summary>
        /// <param name="list"></param>
        /// <param name="input"></param>
        /// <param name="notPlayingColor"></param>
        /// <param name="content"></param>
        private void ParseLyric(ICollection<Line> list, string input, Color notPlayingColor, Transform content)
        {
            string[] lines = input.Split('\n');
            bool isLastLineEmpty = string.IsNullOrEmpty(lines[lines.Length - 1]);
            int count = isLastLineEmpty ? lines.Length - 1 : lines.Length;
            for (int i = 0; i < count; i++)
            {
                string[] timeLineAndLyric = lines[i].Split(']');
                string currentLyric = timeLineAndLyric[timeLineAndLyric.Length - 1];
                for (int j = 0; j < timeLineAndLyric.Length - 1; j++)
                {
                    float currentTime;
                    try
                    {
                        TimeSpan timeSpan = TimeSpan.ParseExact(timeLineAndLyric[j].Replace("[", string.Empty),
                            @"mm\:ss\.FFF", null);
                        currentTime = (float)timeSpan.TotalSeconds;
                    }
                    catch (OverflowException)
                    {
                        currentTime = float.MaxValue;
                    }
                    catch (FormatException)
                    {
                        //暂时没碰到其他类型的格式错误，所以此处只尝试解析为这个，不再放入try块
                        TimeSpan timeSpan = TimeSpan.ParseExact(timeLineAndLyric[j].Replace("[", string.Empty),
                            @"mm\:ss\.FFF\-\1", null);
                        currentTime = (float)timeSpan.TotalSeconds;
                    }

                    Line line = _factory.Create(currentTime, currentLyric, notPlayingColor, content);
                    list.Add(line);
                }
            }

            Line emptyLine = _factory.Create(float.MaxValue, string.Empty, notPlayingColor, content);
            list.Add(emptyLine);
        }

        /// <summary>
        /// 解析翻译为歌词数据
        /// </summary>
        /// <param name="list"></param>
        /// <param name="input"></param>
        private static void ParseTranslation(ICollection<(float, string)> list, string input)
        {
            string[] lines = input.Split('\n');
            bool isLastLineEmpty = string.IsNullOrEmpty(lines[lines.Length - 1]);
            int count = isLastLineEmpty ? lines.Length - 1 : lines.Length;
            for (int i = 0; i < count; i++)
            {
                string[] timeLineAndLyric = lines[i].Split(']');
                string currentLyric = timeLineAndLyric[timeLineAndLyric.Length - 1];
                for (int j = 0; j < timeLineAndLyric.Length - 1; j++)
                {
                    float currentTime;
                    try
                    {
                        TimeSpan timeSpan = TimeSpan.ParseExact(timeLineAndLyric[j].Replace("[", string.Empty),
                            @"mm\:ss\.FFF", null);
                        currentTime = (float)timeSpan.TotalSeconds;
                    }
                    catch (OverflowException)
                    {
                        currentTime = float.MaxValue;
                    }
                    catch (FormatException)
                    {
                        continue;
                    }

                    list.Add((currentTime, currentLyric));
                }
            }
        }

        /// <summary>
        /// 滚动歌词，每次滚动后计算歌词位置和颜色
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="highLight"></param>
        /// <param name="lastLine"></param>
        /// <param name="targetLine"></param>
        /// <param name="controller"></param>
        private void Scroll(Color normal, Color highLight, Line lastLine, Line targetLine, UI.Lyric.Controller controller)
        {
            //计算歌词居中时Content的y位置
            ContentPosY += targetLine._height + controller.VerticalSpacing;
            if (controller._needScrollAutomatically)
            {
                controller.contentTransform.anchoredPosition = Vector2.up * ContentPosY + Vector2.right * controller.contentTransform.anchoredPosition.x;
            }

            if (lastLine == null)
            {
                targetLine._text.color = highLight;
                return;
            }

            lastLine._text.color = normal;
            targetLine._text.color = highLight;
        }

        /// <summary>
        /// 歌词计时器
        /// </summary>
        /// <param name="maxTime"></param>
        /// <param name="id"></param>
        /// <param name="lastLine"></param>
        /// <param name="highLight"></param>
        /// <param name="normal"></param>
        /// <param name="token">任务中断令牌</param>
        /// <returns>任务是否被中断执行？</returns>
        private async Task<bool> CountDownTimerAsync(float maxTime, int id, Line lastLine, Color highLight, Color normal, CancellationToken token)
        {
            _stopwatch.Reset();
            _minusStopwatch.Reset();
            _stopwatch.Start();
            //计算开始后如果暂停则需要减去暂停期间的时间
            double minusSeconds = default;
            while (_stopwatch.Elapsed.TotalSeconds - minusSeconds < maxTime)
            {
                await Task.Yield();
                if (token.IsCancellationRequested)
                {
                    if (lastLine != null)
                        lastLine._text.color = normal;
                    if (_highLightLyric == null) 
                        return true;
                    _highLightLyric._text.color = highLight;
                    _highLightLyric = null;
                    return true;
                }

                if (_playingList.Pause)
                    _minusStopwatch.Start();
                while (_playingList.Pause)
                {
                    await Task.Yield();
                }

                if (!_minusStopwatch.IsRunning)
                    continue;
                minusSeconds += _minusStopwatch.Elapsed.TotalSeconds;
                _minusStopwatch.Reset();
            }

            //暂停且删除当前曲目后，应不再滚动歌词
            if (!_lyrics.ContainsKey(id) || !Application.isPlaying)
            {
                _stopwatch.Reset();
                return true;
            }
#if UNITY_EDITOR && UNITY_DEBUG
            Debug.Log((_stopwatch.Elapsed.TotalSeconds - minusSeconds, maxTime));
#endif
            _stopwatch.Reset();
            return false;
        }

        /// <summary>
        /// 完整展示歌词，中途不能跳转其他歌词节点，第一次播放调用此方法
        /// </summary>
        /// <param name="id"></param>
        /// <param name="controller"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task DisplayAsync(int id, UI.Lyric.Controller controller, CancellationToken token)
        {
            _rollingLyricsId = id;
            Lyric lyric = _lyrics[id];
            List<Line> lines = lyric.lines;
            foreach (Line line in lines)
            {
                line._text.gameObject.SetActive(true);
            }

            //设置歌词开始位置
            ContentPosY = -controller.scrollViewTransform.rect.height / 2.00f;
#if UNITY_DEBUG
            Debug.Log("Display");
#endif
            controller.contentTransform.anchoredPosition = Vector2.up * ContentPosY + Vector2.right * controller.contentTransform.anchoredPosition.x;

            //将歌词背景色调为专辑主色
            controller.image.color = lyric.albumBackground;

            Line lastLine = null;
            for (int i = 0; i < lines.Count; i++)
            {
                if (i != default)
                    lastLine = lines[i - 1];
                Line currentLine = lines[i];
                if(await IsInterruptWhenScrollAsync(currentLine._timeInterval, id, lyric.normal, lyric.highLight, token, lastLine, currentLine, controller))
                    return;
            }
        }

        /// <summary>
        /// 可从任意进度开始展示歌词，中途随意跳转，在调整歌曲进度时使用此方法并中断<see cref="DisplayAsync"/>方法
        /// </summary>
        /// <param name="time"></param>
        /// <param name="controller"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task DisplayByInterruptAsync(float time, UI.Lyric.Controller controller, CancellationToken token)
        {
            Lyric lyric = _lyrics[_rollingLyricsId];
            List<Line> lines = lyric.lines;
#if UNITY_DEBUG
            Debug.Log("DisplayByInterrupt");
#endif
            int targetIndex = default;
            float startTime = default;
            for (int i = 0; i < lines.Count; i++)
            {
                Line line = lines[i];
                if (i != 0)
                    startTime = lines[i - 1]._time;
                if (!(startTime - time + line._timeInterval > 0))
                    continue;
                targetIndex = i;
                break;
            }

            //设置歌词开始位置
            ContentPosY = -controller.scrollViewTransform.rect.height / 2.00f;
            //计算当前歌词播放位置
            for (int i = 0; i < targetIndex; i++)
            {
                ContentPosY += lines[i]._height + controller.VerticalSpacing;
            }

            controller.contentTransform.anchoredPosition = Vector2.up * ContentPosY +
                                                           Vector2.right * controller.contentTransform.anchoredPosition.x;
            Line target = lines[targetIndex];
            _highLightLyric = lines[targetIndex - 1];
            _highLightLyric._text.color = _defaultPlayingColor;
            if (await IsInterruptWhenScrollAsync(target._timeInterval + startTime - time, _rollingLyricsId, lyric.normal, lyric.highLight,
                    token, _highLightLyric, target, controller))
                return;
            Line lastLine = null;
            for (int i = targetIndex + 1; i < lines.Count; i++)
            {
                if (i != default)
                    lastLine = lines[i - 1];
                Line currentLine = lines[i];
                if (await IsInterruptWhenScrollAsync(currentLine._timeInterval, _rollingLyricsId, lyric.normal, lyric.highLight, token,
                        lastLine, currentLine, controller))
                    return;
            }
        }

        /// <summary>
        /// 更新歌词，因被重复调用所以写为方法
        /// </summary>
        /// <param name="maxTime"></param>
        /// <param name="songId"></param>
        /// <param name="highLight"></param>
        /// <param name="token"></param>
        /// <param name="lastLine"></param>
        /// <param name="targetLine"></param>
        /// <param name="controller"></param>
        /// <param name="normal"></param>
        /// <returns>任务是否被中断执行？</returns>
        private async Task<bool> IsInterruptWhenScrollAsync(float maxTime, int songId, Color normal, Color highLight, CancellationToken token,
            Line lastLine, Line targetLine, UI.Lyric.Controller controller)
        {
            if (await CountDownTimerAsync(maxTime, songId, lastLine, highLight, normal, token))
                return true;
            Scroll(normal, highLight, lastLine, targetLine, controller);
            return false;
        }

        /// <summary>
        /// 处理歌词逻辑数据及ui数据，将歌词实例化到场景中
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lyricContent"></param>
        /// <param name="controller"></param>
        /// <param name="album"></param>
        /// <returns></returns>
        internal async Task InstantiateLyricAsync(int id,Transform lyricContent,UI.Lyric.Controller controller, Texture2D album)
        {
            if(_lyrics.ContainsKey(id))
                return;

            #region 请求歌词数据

            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            LyricRequest lyricRequest = new LyricRequest(id, cookie.value);
            string resultJson = await _network.PostAsync(Network.LyricUrl, lyricRequest, true);
            LyricResult lyricResult = JsonMapper.ToObject<LyricResult>(resultJson);

            #endregion

            #region 计算歌词背景颜色、歌词颜色、歌词高亮颜色，处理歌词数据

            Color backgroundColor = SampleAlbumColor(album, controller.originalBackgroundColor.a);
            float deltaNotPlaying = ColorDistanceInRedMean(backgroundColor, _defaultNotPlayingColor);
            Color notPlaying = deltaNotPlaying > ColorDistanceLimit ? _defaultNotPlayingColor : _spareNotPlayingColor;
            List<Line> lyric = PrepareData(lyricResult.lrc.lyric, lyricResult.tlyric?.lyric, notPlaying, lyricContent, controller);
            float deltaPlaying = ColorDistanceInRedMean(backgroundColor, _defaultPlayingColor);
            Color playing = deltaPlaying > ColorDistanceLimit ? _defaultPlayingColor : _sparePlayingColor;

            #endregion

#if UNITY_DEBUG
            Debug.Log($"歌词背景颜色为{(Color32)backgroundColor}");
            Debug.Log($"平时的色差{deltaNotPlaying}");
            Debug.Log($"高亮时色差{deltaPlaying}");
#endif

            //二次验证，防止点击过快造成重复添加
            if (_lyrics.ContainsKey(id))
                return;
            _lyrics.Add(id, new Lyric(lyric, backgroundColor, playing, notPlaying));
        }

        private static float ColorDistanceInRedMean(Color32 colorA, Color32 colorB)
        {
            int deltaR = colorA.r - colorB.r;
            int deltaG = colorA.g - colorB.g;
            int deltaB = colorA.b - colorB.b;
            float averageR = (colorA.r + colorB.r) / 2.00f;
            float sum = (2f + averageR / 256f) * Mathf.Pow(deltaR, 2f) + 4 * Mathf.Pow(deltaG, 2f) +
                        (2f + (255f - averageR) / 256f) * Mathf.Pow(deltaB, 2f);
            float deltaC = Mathf.Sqrt(sum);
            return deltaC;
        }

        private Color SampleAlbumColor(Texture2D album, float alpha)
        {
            Color[] array = album.GetPixels();
            float r=default;
            float g=default;
            float b=default;
            foreach (Color c in array)
            {
                r += c.r;
                g += c.g;
                b += c.g;
            }

            Color color = new Color(r / array.Length, g / array.Length, b / array.Length, alpha);

            return color;
        }

        internal void Dispose(int id)
        {
            if (!_lyrics.ContainsKey(id))
                return;
            List<Line> lyric = _lyrics[id].lines;
            _lyrics.Remove(id);
            for (int i = 0; i < lyric.Count; i++)
            {
                lyric[i].Dispose();
                lyric[i] = null;
            }

            lyric.Clear();
        }

        internal void Disable(int id)
        {
            if (!_lyrics.ContainsKey(id))
                return;
            List<Line> lyric = _lyrics[id].lines;
            foreach (Line line in lyric)
            {
                line.Disable();
            }
        }

        internal class Line:IPoolable<float,string,Color,Transform,IMemoryPool>,IDisposable,IComparable<Line>
        {
            internal float _time;
            internal float _timeInterval;
            internal float _height;
            internal Text _text;

            private float _originalSizeY;
            private float _originalLineSpacing;
            private IMemoryPool _pool;

            public void Dispose()
            {
                _pool.Despawn(this);
            }

            public void Disable()
            {
                _text.gameObject.SetActive(false);
            }

            public void OnDespawned()
            {
                _time = default;
                _timeInterval = default;
                _height = default;
                _text.gameObject.SetActive(false);
                _text.text = null;
                _text.rectTransform.sizeDelta = Vector2.up * _originalSizeY + Vector2.right * _text.rectTransform.sizeDelta.x;
                _text.lineSpacing = _originalLineSpacing;
                _pool = null;
            }

            public void OnSpawned(float time, string lyric, Color defaultColor, Transform transform, IMemoryPool pool)
            {
                _time = time;
                _height = _originalSizeY;
                if (_text != null)
                {
                    _text.text = lyric;
                }

                _pool = pool;
            }

            internal void SetSiblingIndex(int index) => _text.rectTransform.SetSiblingIndex(index);

            public int CompareTo(Line other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return _time.CompareTo(other._time);
            }

            internal class Factory : PlaceholderFactory<float,string,Color,Transform,Line>
            {
                private readonly DiContainer _container;

                public Factory(DiContainer container)
                {
                    _container = container;
                }

                public override Line Create(float time, string lyric, Color defaultColor, Transform content)
                {
                    Line line = base.Create(time, lyric, defaultColor, content);
                    if (line._text == null)
                    {
                        GameObject go = _container.InstantiatePrefabResource("LyricText", content);
                        line._text = go.GetComponent<Text>();
                    }

                    line._originalSizeY = line._text.rectTransform.sizeDelta.y;
                    line._originalLineSpacing = line._text.lineSpacing;
                    line._height = line._originalSizeY;
                    line._time = time;
                    line._text.text = lyric;
                    line._text.color = defaultColor;
                    _container.Inject(line);
                    return line;
                }

                public override void Validate()
                {
                    _container.InstantiatePrefabResource("LyricText");
                    _container.Instantiate<Line>();
                }
            }
        }

        internal struct Lyric
        {
            internal readonly List<Line> lines;

            internal readonly Color albumBackground;
            internal readonly Color highLight;
            internal readonly Color normal;

            public Lyric(List<Line> lines, Color albumBackground, Color highLight, Color normal)
            {
                this.lines = lines;
                this.albumBackground = albumBackground;
                this.highLight = highLight;
                this.normal = normal;

                if (lines == null || lines.Count == 0) 
                    return;
                if (lines[0]._text.color == normal) 
                    return;
                foreach (Line line in lines)
                {
                    line._text.color = normal;
                }
            }
        }
    }
}