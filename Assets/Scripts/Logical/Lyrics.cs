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
        //ÿ����ʱ���ļ�ʱ��
        private readonly Stopwatch _stopwatch;
        //��ͣ�ڼ���Ҫ��ȥ�ļ�ʱ����ֵ
        private readonly Stopwatch _minusStopwatch;
        /// <summary>
        /// �����������ʱ��������������
        /// </summary>
        internal readonly TaskQueue<int> taskQueue;
        /// <summary>
        /// ����Ҫ������ʽ���ʱ��ʹ�õ��������
        /// </summary>
        internal readonly TaskQueue<float> interruptTaskQueue;
        //��ǰ�����ĸ����ɫ
        private readonly Color _defaultPlayingColor;
        //��������ĸ����ɫ
        private readonly Color _defaultNotPlayingColor;
        //��������ɫ��ǰ�涨���������������ĸ����ɫ������ʱ�����õı�����ɫ
        private readonly Color _sparePlayingColor;
        //��������ɫ��ǰ�涨��Ĳ�������ĸ����ɫ������ʱ�����õı�����ɫ
        private readonly Color _spareNotPlayingColor;

        //�Ը���idΪkey,LyricΪvalue���ֵ�
        private readonly Dictionary<int, Lyric> _lyrics;

        //���ڸ���չʾ���Ǿ��ʣ��ڵ�����ʽ���ʱ�ж��Ƿ���Ҫȡ��ԭ����չʾ
        private Line _highLightLyric;

        //���ڲ��ŵĸ����������id
        private int _rollingLyricsId;

        //δ�ӷ������ı���ԭ���߶�
        private float _lyricHeight;
        //���뷭������ı��ĸ߶�
        private float _translationLyricHeight;
        //���뷭�����о�
        private float _translationLyricLineSpacing;
        //���뷭������ı��ĸ߶Ⱥ�δ�ӷ������ı��ĸ߶ȳ���
        private const float TranslationHeightMultiplier = 1.34f;
        //���뷭����о����
        private const float TranslationLineSpacingMultiplier = 1.3f;
        //��ɫ��С�ڴ���ʱ�ж�Ϊ��ɫ����
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

            //TODO:1.����ڳ����п���Զ����С�2.���ı���ʶ�����ʾ
            #region ���ı���ʴ���

            //������]���ʾû��ʱ���ᣬ�Ǵ��ı����
            if (!lyric.Contains(']'))
            {
                Line line = _factory.Create(0f, lyric, notPlayingColor, content);
                line._timeInterval = 0f;
                line.SetSiblingIndex(0);
                list.Add(line);
                //��ΪpreferredHeight���޸�Text���ı��󷵻ص�ֵ��׼ȷ������ֻ�ܽ���textGenerator����߶�
                float height = _textGenerator.GetPreferredHeight(line._text.text,
                    line._text.GetGenerationSettings(controller.scrollRect.viewport.sizeDelta));
                line._text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                //�ӿ��е�Ŀ���ǿ�����û�й�����Ч��
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

            #region �������Լ����UI��С

            List<(float time, string lyric)> translationList = new List<(float time, string lyric)>();
            ParseTranslation(translationList, translationLyric);
            translationList.Sort();

            //������뷭���ĸ��ui�ĸ߶Ⱥ�text�����lineSpacing��С
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
        /// ����ʽ���Ϊ����
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
                        //��ʱû�����������͵ĸ�ʽ�������Դ˴�ֻ���Խ���Ϊ��������ٷ���try��
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
        /// ��������Ϊ�������
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
        /// ������ʣ�ÿ�ι����������λ�ú���ɫ
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="highLight"></param>
        /// <param name="lastLine"></param>
        /// <param name="targetLine"></param>
        /// <param name="controller"></param>
        private void Scroll(Color normal, Color highLight, Line lastLine, Line targetLine, UI.Lyric.Controller controller)
        {
            //�����ʾ���ʱContent��yλ��
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
        /// ��ʼ�ʱ��
        /// </summary>
        /// <param name="maxTime"></param>
        /// <param name="id"></param>
        /// <param name="lastLine"></param>
        /// <param name="highLight"></param>
        /// <param name="normal"></param>
        /// <param name="token">�����ж�����</param>
        /// <returns>�����Ƿ��ж�ִ�У�</returns>
        private async Task<bool> CountDownTimerAsync(float maxTime, int id, Line lastLine, Color highLight, Color normal, CancellationToken token)
        {
            _stopwatch.Reset();
            _minusStopwatch.Reset();
            _stopwatch.Start();
            //���㿪ʼ�������ͣ����Ҫ��ȥ��ͣ�ڼ��ʱ��
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

            //��ͣ��ɾ����ǰ��Ŀ��Ӧ���ٹ������
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
        /// ����չʾ��ʣ���;������ת������ʽڵ㣬��һ�β��ŵ��ô˷���
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

            //���ø�ʿ�ʼλ��
            ContentPosY = -controller.scrollViewTransform.rect.height / 2.00f;
#if UNITY_DEBUG
            Debug.Log("Display");
#endif
            controller.contentTransform.anchoredPosition = Vector2.up * ContentPosY + Vector2.right * controller.contentTransform.anchoredPosition.x;

            //����ʱ���ɫ��Ϊר����ɫ
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
        /// �ɴ�������ȿ�ʼչʾ��ʣ���;������ת���ڵ�����������ʱʹ�ô˷������ж�<see cref="DisplayAsync"/>����
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

            //���ø�ʿ�ʼλ��
            ContentPosY = -controller.scrollViewTransform.rect.height / 2.00f;
            //���㵱ǰ��ʲ���λ��
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
        /// ���¸�ʣ����ظ���������дΪ����
        /// </summary>
        /// <param name="maxTime"></param>
        /// <param name="songId"></param>
        /// <param name="highLight"></param>
        /// <param name="token"></param>
        /// <param name="lastLine"></param>
        /// <param name="targetLine"></param>
        /// <param name="controller"></param>
        /// <param name="normal"></param>
        /// <returns>�����Ƿ��ж�ִ�У�</returns>
        private async Task<bool> IsInterruptWhenScrollAsync(float maxTime, int songId, Color normal, Color highLight, CancellationToken token,
            Line lastLine, Line targetLine, UI.Lyric.Controller controller)
        {
            if (await CountDownTimerAsync(maxTime, songId, lastLine, highLight, normal, token))
                return true;
            Scroll(normal, highLight, lastLine, targetLine, controller);
            return false;
        }

        /// <summary>
        /// �������߼����ݼ�ui���ݣ������ʵ������������
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

            #region ����������

            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            LyricRequest lyricRequest = new LyricRequest(id, cookie.value);
            string resultJson = await _network.PostAsync(Network.LyricUrl, lyricRequest, true);
            LyricResult lyricResult = JsonMapper.ToObject<LyricResult>(resultJson);

            #endregion

            #region �����ʱ�����ɫ�������ɫ����ʸ�����ɫ������������

            Color backgroundColor = SampleAlbumColor(album, controller.originalBackgroundColor.a);
            float deltaNotPlaying = ColorDistanceInRedMean(backgroundColor, _defaultNotPlayingColor);
            Color notPlaying = deltaNotPlaying > ColorDistanceLimit ? _defaultNotPlayingColor : _spareNotPlayingColor;
            List<Line> lyric = PrepareData(lyricResult.lrc.lyric, lyricResult.tlyric?.lyric, notPlaying, lyricContent, controller);
            float deltaPlaying = ColorDistanceInRedMean(backgroundColor, _defaultPlayingColor);
            Color playing = deltaPlaying > ColorDistanceLimit ? _defaultPlayingColor : _sparePlayingColor;

            #endregion

#if UNITY_DEBUG
            Debug.Log($"��ʱ�����ɫΪ{(Color32)backgroundColor}");
            Debug.Log($"ƽʱ��ɫ��{deltaNotPlaying}");
            Debug.Log($"����ʱɫ��{deltaPlaying}");
#endif

            //������֤����ֹ�����������ظ����
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