using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    internal enum DisplayLyricWays
    {
        Normal = 0,
        Interrupted
    }

    internal class Lyrics
    {
        private readonly PlayingList _playingList;
        private readonly Line.Factory _factory;
        private readonly Network _network;
        private readonly Cookies _cookies;
        //每句歌词时长的计时器
        private readonly Stopwatch _stopwatch;
        //暂停期间需要减去的计时器数值
        private readonly Stopwatch _minusStopwatch;
        //以歌曲id为key,Lyric为value的字典
        private readonly Dictionary<int, Lyric> _lyrics;

        /// <summary>
        /// 歌词正常播放时所加入的任务队列
        /// </summary>
        internal readonly TaskQueue<int> taskQueue;
        /// <summary>
        /// 当需要调整歌词进度时所使用的任务队列
        /// </summary>
        internal readonly TaskQueue interruptTaskQueue;

        //当前高亮的歌词颜色
        private readonly Color _defaultPlayingColor;
        //不在演奏的歌词颜色
        private readonly Color _defaultNotPlayingColor;
        //当背景颜色与前面定义的正在演奏高亮的歌词颜色相类似时所采用的备用颜色
        private readonly Color _sparePlayingColor;
        //当背景颜色与前面定义的不在演奏的歌词颜色相类似时所采用的备用颜色
        private readonly Color _spareNotPlayingColor;

        //正在高亮展示的那句歌词，在调整歌词进度时判断是否需要取消原高亮展示
        private Line _highLightLyric;

        private readonly StringBuilder _timeParsePatten;

        private int _rollingLyricsId;
        /// <summary>
        /// 当此值改变时，则表示需要重新计算<see cref="ContentPosY"/>并赋值
        /// </summary>
        private float _contentHeight;

        //加入翻译后行距
        private const float TranslationLineSpacing = 1.3f;
        //当色差小于此数时判断为颜色类似，大约在80-130为合理
        private const float ColorDistanceLimit = 110f;
        //纯文本歌词在歌词展示界面的提示语
        private const string LyricTextOnlyTip = "(这是纯文本歌词，不支持自动滚动)";
        //没有歌词时在歌词展示界面的提示语
        private const string NoLyricTip = "(暂无人上传歌词)";

        /// <summary>
        /// 高亮歌词应在的y轴位置
        /// </summary>
        internal float ContentPosY { get; private set; }

        internal Lyrics(PlayingList playingList,Line.Factory factory, Network network, Cookies cookies,
            TaskQueue<int> taskQueue, TaskQueue interruptTaskQueue)
        {
            _playingList = playingList;
            _factory = factory;
            _network = network;
            _cookies = cookies;
            _defaultPlayingColor = new Color32(235, 235, 235, 255);
            _defaultNotPlayingColor = Color.black;
            _sparePlayingColor = new Color32(125, 125, 125, 255);
            _spareNotPlayingColor = new Color32(125, 125, 125, 255);
            _lyrics = new Dictionary<int, Lyric>(20);
            _stopwatch = new Stopwatch();
            _minusStopwatch = new Stopwatch();
            _timeParsePatten = new StringBuilder(30);
            this.taskQueue = taskQueue;
            this.interruptTaskQueue = interruptTaskQueue;
        }

        private (List<Line> list, bool needHighLightPositionAutoReset) PrepareData(string lyric, string translationLyric, Color notPlayingColor, Transform content)
        {
            List<Line> list = new List<Line>();

            #region 没有歌词

            if (string.IsNullOrEmpty(lyric))
            {
                Line emptyLine = _factory.Create(0f, string.Empty, notPlayingColor, content);
                emptyLine._timeInterval = float.MaxValue;
                list.Add(emptyLine);

                Line tip = _factory.Create(0f, NoLyricTip, notPlayingColor, content);
                list.Add(tip);
                return (list, false);
            }

            #endregion

            #region 纯文本歌词处理

            //不包含]则表示没有时间轴，是纯文本歌词
            if (!lyric.Contains(']'))
            {
                //加空行的目的是看起来没有滚动的效果，先加是因为要使高亮一直停留在第一句
                Line emptyLine = _factory.Create(0f, string.Empty, notPlayingColor, content);
                emptyLine._timeInterval = float.MaxValue;
                list.Add(emptyLine);

                Line tip = _factory.Create(0f, LyricTextOnlyTip, notPlayingColor, content);
                list.Add(tip);

                string[] lyrics = lyric.Split('\n');
                list.AddRange(lyrics.Select(lyc => _factory.Create(0f, lyc, notPlayingColor, content)));

                return (list, false);
            }

            #endregion

            ParseLyric(list, lyric, notPlayingColor, content);
            list.Sort();

            for (int i = 0; i < list.Count; i++)
            {
                if (i == 0)
                    list[i]._timeInterval = list[i]._time;
                else
                    list[i]._timeInterval = list[i]._time - list[i - 1]._time;
            }

            if (string.IsNullOrEmpty(translationLyric))
                return (list, true);

            #region 处理翻译

            List<(float time, string lyric)> translationList = new List<(float time, string lyric)>();
            ParseTranslation(translationList, translationLyric);
            translationList.Sort();

            int index = 0;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Line line in list)
            {
                if (index >= translationList.Count ||
                    !Mathf.Approximately(line._time, translationList[index].time)) continue;
                stringBuilder.AppendLine(line._text.text);
                stringBuilder.Append(translationList[index++].lyric);
                line._text.text = stringBuilder.ToString();
                line._text.lineSpacing = TranslationLineSpacing;
                stringBuilder.Clear();
            }

            #endregion

            return (list, true);
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
                //可能有多个时间轴使用同一句歌词的情况
                string[] timeLineAndLyric = lines[i].Split(']');
                //最后一句总为歌词
                string currentLyric = timeLineAndLyric[timeLineAndLyric.Length - 1];
                for (int j = 0; j < timeLineAndLyric.Length - 1; j++)
                {
                    TimeSpan timeSpan;
                    string timeLine = timeLineAndLyric[j].Replace("[", string.Empty);
                    //匹配第一个符合格式的时间
                    const string matchPatten = @"^(?:\d{1,2}:)?(?:\d{1,2}:)?\d{1,2}(?:\.\d{1,3})?";
                    Match timeMatch = Regex.Match(timeLine, matchPatten, RegexOptions.None);
                    if(!timeMatch.Success)
                        continue;
                    //提取合法时间
                    string matchedTime = timeMatch.Value;
                    const string hh = @"\d{1,2}";
                    short h = 0;
                    short m = 0;
                    short s;
                    short f = 0;
                    string[] res = matchedTime.Split(':', '.');

                    if (Regex.IsMatch(matchedTime, Combine(true, hh, hh, hh)))
                    {
                        h = short.Parse(res[0]);
                        m = short.Parse(res[1]);
                        s = short.Parse(res[2]);
                        f = short.Parse(res[3]);
                    }
                    else if (Regex.IsMatch(matchedTime, Combine(false, hh, hh, hh)))
                    {
                        h = short.Parse(res[0]);
                        m = short.Parse(res[1]);
                        s = short.Parse(res[2]);
                    }
                    else if (Regex.IsMatch(matchedTime, Combine(true, hh, hh)))
                    {
                        m = short.Parse(res[0]);
                        s = short.Parse(res[1]);
                        f = short.Parse(res[2]);
                    }
                    else if (Regex.IsMatch(matchedTime, Combine(false, hh, hh)))
                    {
                        m = short.Parse(res[0]);
                        s = short.Parse(res[1]);
                    }
                    else if (Regex.IsMatch(matchedTime, Combine(true, hh)))
                    {
                        s = short.Parse(res[0]);
                        f = short.Parse(res[1]);
                    }
                    else
                    {
                        s = short.Parse(res[0]);
                    }

                    timeSpan = new TimeSpan(0, h, m, s, f);
                    float currentTime = (float)timeSpan.TotalSeconds;

                    // ReSharper disable once InconsistentNaming
                    string Combine(bool needFFF, params string[] patten)
                    {
                        const string fff = @"\d{1,3}";
                        _timeParsePatten.Clear();
                        for (int k = 0; k < patten.Length - 1; k++)
                        {
                            _timeParsePatten.Append(patten[k]).Append(':');
                        }

                        _timeParsePatten.Append(patten[patten.Length - 1]);
                        if (needFFF)
                            _timeParsePatten.Append("\\.").Append(fff);

                        return _timeParsePatten.ToString();
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
        /// <param name="mediator"></param>
        private void Scroll(Color normal, Color highLight, Line lastLine, Line targetLine, UI.Lyric.Mediator mediator)
        {
            //计算歌词居中时Content的y位置
            ContentPosY += targetLine.rectTransform.rect.height + mediator.VerticalSpacing;
            if (mediator._needScrollAutomatically)
            {
                mediator.contentTransform.anchoredPosition = Vector2.up * ContentPosY + Vector2.right * mediator.contentTransform.anchoredPosition.x;
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
        /// <param name="resetContentPosY"></param>
        /// <param name="token">任务中断令牌</param>
        /// <returns>任务是否被中断执行？</returns>
        private async Task<bool> CountDownTimerAsync(double maxTime, int id, Action resetContentPosY, CancellationToken token)
        {
            _stopwatch.Reset();
            _minusStopwatch.Reset();
            _stopwatch.Start();
            //计算开始后如果暂停则需要减去暂停期间的时间
            double minusSeconds = default;
            while (_stopwatch.Elapsed.TotalSeconds - minusSeconds < maxTime)
            {
                await Task.Yield();
                resetContentPosY();
                if (token.IsCancellationRequested)
                    return true;
                if (_playingList.Pause)
                    _minusStopwatch.Start();
                while (_playingList.Pause)
                {
                    await Task.Yield();
                    if (token.IsCancellationRequested)
                        return true;
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
        /// <param name="mediator"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task DisplayAsync(int id, UI.Lyric.Mediator mediator, CancellationToken token)
        {
            //在换歌后调整前一首的高亮为普通
            if (_rollingLyricsId != 0 && _lyrics.ContainsKey(_rollingLyricsId))
            {
                Lyric lastLyric = _lyrics[_rollingLyricsId];
                if (_highLightLyric != null)
                    _highLightLyric._text.color = lastLyric.normal;
            }

            _rollingLyricsId = id;
            Lyric lyric = _lyrics[id];
            List<Line> lines = lyric.lines;
            int index = 0;
            //调整歌词顺序
            foreach (Line line in lines)
            {
                line._text.gameObject.SetActive(true);
                if (Mathf.Approximately(line._timeInterval, float.MaxValue) &&
                    string.IsNullOrEmpty(line._text.text))
                {
                    line.SetSiblingIndex(lines.Count - 1);
                    continue;
                }
                line.SetSiblingIndex(index);
                index++;
            }

            //设置歌词开始位置
            ContentPosY = -mediator.scrollViewTransform.rect.height / 2.00f;
            mediator.contentTransform.anchoredPosition = Vector2.up * ContentPosY + Vector2.right * mediator.contentTransform.anchoredPosition.x;
            //将歌词背景色调为专辑主色
            mediator.image.color = lyric.albumBackground;
            //滚动歌词
            Line lastLine = null;
            for (int i = 0; i < lines.Count; i++)
            {
                if (i != default)
                    lastLine = lines[i - 1];
                Line currentLine = lines[i];
                if(await IsInterruptWhenScrollAsync(currentLine._timeInterval, id, lyric.normal, lyric.highLight, token, lastLine, currentLine, mediator))
                    return;
            }
        }

        /// <summary>
        /// 可从任意进度开始展示歌词，中途随意跳转，在调整歌曲进度时使用此方法并中断<see cref="DisplayAsync"/>方法
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task DisplayByInterruptAsync(UI.Lyric.Mediator mediator, CancellationToken token)
        {
            Lyric lyric = _lyrics[_rollingLyricsId];
            List<Line> lines = lyric.lines;
            bool needHighLightPositionAutoReset = lyric.needHighLightPositionAutoReset;
            mediator._needHighLightPositionAutoReset = needHighLightPositionAutoReset;
            int targetIndex = default;
            float startTime = default;
            float currentTime = _playingList.CurrentTime;
            //寻找目标歌词索引
            for (int i = 0; i < lines.Count; i++)
            {
                Line line = lines[i];
                if (i != 0)
                    startTime = lines[i - 1]._time;
                if (!(startTime - currentTime + line._timeInterval > 0))
                    continue;
                targetIndex = i;
                break;
            }

            if (needHighLightPositionAutoReset)
            {
                //设置歌词开始位置
                ContentPosY = -mediator.scrollViewTransform.rect.height / 2.00f;
                //计算当前歌词播放位置
                for (int i = 0; i < targetIndex; i++)
                {
                    ContentPosY += lines[i].rectTransform.rect.height + mediator.VerticalSpacing;
                }

                mediator.contentTransform.anchoredPosition = Vector2.up * ContentPosY +
                                                               Vector2.right * mediator.contentTransform.anchoredPosition.x;
            }

            //将跳转进度前高亮歌词取消高亮
            if (_highLightLyric != null)
                _highLightLyric._text.color = lyric.normal;
            //将前句未唱完的歌词高亮并展示指定时间
            Line target = lines[targetIndex];
            if (targetIndex != 0)
            {
                _highLightLyric = lines[targetIndex - 1];
                _highLightLyric._text.color = lyric.highLight;
            }
            if (await IsInterruptWhenScrollAsync(target._timeInterval + startTime - currentTime, _rollingLyricsId, lyric.normal, lyric.highLight,
                    token, _highLightLyric, target, mediator))
                return;
            Line lastLine = null;
            //从目标歌词开始顺序展示
            for (int i = targetIndex + 1; i < lines.Count; i++)
            {
                if (i != default)
                    lastLine = lines[i - 1];
                Line currentLine = lines[i];
                if (await IsInterruptWhenScrollAsync(currentLine._timeInterval, _rollingLyricsId, lyric.normal, lyric.highLight, token,
                        lastLine, currentLine, mediator))
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
        /// <param name="mediator"></param>
        /// <param name="normal"></param>
        /// <returns>任务是否被中断执行？</returns>
        private async Task<bool> IsInterruptWhenScrollAsync(double maxTime, int songId, Color normal, Color highLight, CancellationToken token,
            Line lastLine, Line targetLine, UI.Lyric.Mediator mediator)
        {
            if (lastLine == null)
                _highLightLyric = targetLine;
            _highLightLyric = lastLine;
            if (await CountDownTimerAsync(maxTime, songId, ResetContentPos, token))
                return true;
            Scroll(normal, highLight, lastLine, targetLine, mediator);
            return false;

            void ResetContentPos() => ResetContentPosY(mediator);
        }

        /// <summary>
        /// 处理歌词逻辑数据及ui数据，将歌词实例化到场景中
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mediator"></param>
        /// <param name="album"></param>
        /// <returns></returns>
        internal async Task InstantiateLyricAsync(int id,UI.Lyric.Mediator mediator, Texture2D album)
        {
            if (!_lyrics.ContainsKey(id))
            {
                #region 请求歌词数据

                Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
                LyricRequest lyricRequest = new LyricRequest(id, cookie.value);
                string resultJson = await _network.PostAsync(Network.LyricUrl, lyricRequest, true);
                LyricResult lyricResult = JsonMapper.ToObject<LyricResult>(resultJson);

                #endregion

                #region 计算歌词背景颜色、歌词颜色、歌词高亮颜色，处理歌词数据

                Color backgroundColor = SampleAlbumColor(album, mediator.originalBackgroundColor.a);
                float deltaNotPlaying = ColorDistanceInRedMean(backgroundColor, _defaultNotPlayingColor);
                Color notPlaying = deltaNotPlaying > ColorDistanceLimit ? _defaultNotPlayingColor : _spareNotPlayingColor;
                (List<Line> list, bool needHighLightPositionAutoReset) = PrepareData(lyricResult.lrc.lyric, lyricResult.tlyric?.lyric,
                    notPlaying, mediator.contentTransform);
                float deltaPlaying = ColorDistanceInRedMean(backgroundColor, _defaultPlayingColor);
                Color playing = deltaPlaying > ColorDistanceLimit ? _defaultPlayingColor : _sparePlayingColor;
                //二次验证，防止点击过快造成重复添加
                if (!_lyrics.ContainsKey(id))
                    _lyrics.Add(id, new Lyric(list, backgroundColor, playing, notPlaying, needHighLightPositionAutoReset));

                #endregion

#if UNITY_DEBUG
                Debug.Log($"歌词背景颜色为{(Color32)backgroundColor}");
                Debug.Log($"平时的色差{deltaNotPlaying}");
                Debug.Log($"高亮时色差{deltaPlaying}");
#endif
            }
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

        private static Color SampleAlbumColor(Texture2D album, float alpha)
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

        internal void SetActive(int id, bool value)
        {
            if (id == 0 || !_lyrics.ContainsKey(id))
                return;
            List<Line> lyric = _lyrics[id].lines;
            foreach (Line line in lyric)
            {
                line._text.gameObject.SetActive(value);
            }
        }

        internal void CalculateContentPosY(UI.Lyric.Mediator mediator)
        {
            if (!_lyrics[_rollingLyricsId].needHighLightPositionAutoReset)
                return;
            List<Line> lines = _lyrics[_rollingLyricsId].lines;
            int targetIndex = default;
            float startTime = default;
            float currentTime = _playingList.CurrentTime;
            for (int i = 0; i < lines.Count; i++)
            {
                Line line = lines[i];
                if (i != 0)
                    startTime = lines[i - 1]._time;
                if (!(startTime - currentTime + line._timeInterval > 0))
                    continue;
                targetIndex = i;
                break;
            }

            //设置歌词开始位置
            ContentPosY = -mediator.scrollViewTransform.rect.height / 2.00f;
            //计算当前歌词播放位置
            for (int i = 0; i < targetIndex; i++)
            {
                ContentPosY += lines[i].rectTransform.rect.height + mediator.VerticalSpacing;
            }
        }

        internal void ResetContentPosY(UI.Lyric.Mediator mediator)
        {
            float value = mediator.contentTransform.rect.height;
            if (Mathf.Approximately(_contentHeight, value))
                return;
            _contentHeight = value;
            CalculateContentPosY(mediator);
            mediator.contentTransform.anchoredPosition = Vector2.up * ContentPosY +
                                                         Vector2.right * mediator.contentTransform.anchoredPosition.x;
        }

        internal class Line:IPoolable<float,string,Color,Transform,IMemoryPool>,IDisposable,IComparable<Line>
        {
            internal float _time;
            internal float _timeInterval;
            internal Text _text;
            internal Mask _mask;

            private float _originalLineSpacing;
            private IMemoryPool _pool;

            internal RectTransform rectTransform { get; private set; }

            public void Dispose()
            {
                _pool.Despawn(this);
            }

            void IPoolable<float, string, Color, Transform, IMemoryPool>.OnDespawned()
            {
                _time = default;
                _timeInterval = default;
                _text.gameObject.SetActive(false);
                _text.text = null;
                _text.lineSpacing = _originalLineSpacing;
                _pool = null;
            }

            void IPoolable<float, string, Color, Transform, IMemoryPool>.OnSpawned(float time, string lyric, Color defaultColor, Transform transform, IMemoryPool pool)
            {
                _time = time;
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
                        line._mask = go.GetComponent<Mask>();
                        line.rectTransform = line._text.rectTransform;
                    }

                    line._originalLineSpacing = line._text.lineSpacing;
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

        internal readonly struct Lyric
        {
            internal readonly List<Line> lines;

            internal readonly Color albumBackground;
            internal readonly Color highLight;
            internal readonly Color normal;
            /// <summary>
            /// 需要歌词自动复位到正在播放的位置吗？
            /// </summary>
            internal readonly bool needHighLightPositionAutoReset;

            internal Lyric(List<Line> lines, Color albumBackground, Color highLight, Color normal, bool needHighLightPositionAutoReset)
            {
                this.lines = lines;
                this.albumBackground = albumBackground;
                this.highLight = highLight;
                this.normal = normal;
                this.needHighLightPositionAutoReset = needHighLightPositionAutoReset;
            }
        }
    }
}