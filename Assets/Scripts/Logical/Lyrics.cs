using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Models.Lyric;
using InnerMediaPlayer.UI;
using LitJson;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Debug = UnityEngine.Debug;
using Network = InnerMediaPlayer.Tools.Network;

namespace InnerMediaPlayer.Logical
{
    internal class Lyrics
    {
        private readonly PlayingList _playingList;
        private readonly Line.Factory _factory;
        private readonly Network _network;
        private readonly Cookies _cookies;
        private readonly TextGenerator _textGenerator;
        //����ʱ����ɫ
        private readonly Color _playingColor = new Color32(235, 235, 235, 255);
        //���ڲ���ʱ����ɫ
        private readonly Color _notPlayingColor = Color.black;

        private readonly Dictionary<int, (List<Line> lines, Color backgroundColor)> _lyrics;

        private float _contentPosY;
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

        internal Lyrics(PlayingList playingList,Line.Factory factory, Network network, Cookies cookies, TextGenerator textGenerator)
        {
            _playingList = playingList;
            _factory = factory;
            _network = network;
            _cookies = cookies;
            _textGenerator = textGenerator;
            _lyrics = new Dictionary<int, (List<Line> lines, Color backgroundColor)>(20);
        }

        private List<Line> PrepareData(string lyric, string translationLyric, Transform content, Lyric.Controller controller)
        {
            List<Line> list = new List<Line>();

            #region ���ı���ʴ���

            //������]���ʾû��ʱ���ᣬ�Ǵ��ı����
            if (!lyric.Contains(']'))
            {
                Line line = _factory.Create(0f, lyric, content);
                line._timeInterval = 0f;
                line.SetSiblingIndex(0);
                list.Add(line);
                //��ΪpreferredHeight���޸�Text���ı��󷵻ص�ֵ��׼ȷ������ֻ�ܽ���textGenerator����߶�
                float height = _textGenerator.GetPreferredHeight(line._text.text,
                    line._text.GetGenerationSettings(controller.scrollRect.viewport.sizeDelta));
                line._text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                //�ӿ��е�Ŀ���ǿ�����û�й�����Ч��
                Line emptyLine = _factory.Create(0.1f, string.Empty, content);
                emptyLine._timeInterval = 0.1f;
                emptyLine.SetSiblingIndex(1);
                list.Add(emptyLine);

                return list;
            }

            #endregion

            ParseLyric(list, lyric, content);
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

        private void ParseLyric(ICollection<Line> list, string input, Transform content)
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

                    Line line = _factory.Create(currentTime, currentLyric, content);
                    list.Add(line);

                }
            }

        }

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

        private void ScrollingLyric(Line lastLine, Line currentLine, Lyric.Controller controller)
        {
            //�����ʾ���ʱContent��yλ��
            _contentPosY += currentLine._height + controller.VerticalSpacing;
            if (controller._needScroll)
            {
                controller.contentTransform.anchoredPosition = Vector2.up * _contentPosY + Vector2.right * controller.contentTransform.anchoredPosition.x;
            }

            if (lastLine == null)
            {
                currentLine._text.color = _playingColor;
                return;
            }

            lastLine._text.color = _notPlayingColor;
            currentLine._text.color = _playingColor;
        }

        internal async Task DisplayLyricAsync(int id,Lyric.Controller controller,CancellationToken token)
        {
            List<Line> lyric = _lyrics[id].lines;
            foreach (Line line in lyric)
            {
                line._text.gameObject.SetActive(true);
            }

            //���ø�ʿ�ʼλ��
            _contentPosY = -controller.scrollViewTransform.rect.height / 2.00f;
            controller.contentTransform.anchoredPosition = Vector2.up * _contentPosY + Vector2.right * controller.contentTransform.anchoredPosition.x;

            //����ʱ���ɫ��Ϊר����ɫ
            controller.image.color = _lyrics[id].backgroundColor;

            Line lastLine = null;
            for (int i = 0; i < lyric.Count; i++)
            {
                if (i != default)
                    lastLine = lyric[i - 1];
                Line currentLine = lyric[i];
                //ÿ����ʱ���ļ�ʱ��
                Stopwatch stopwatch = Stopwatch.StartNew();
                //���㿪ʼ�������ͣ����Ҫ��ȥ��ͣ�ڼ��ʱ��
                double minusSeconds = default;
                while (stopwatch.Elapsed.TotalSeconds - minusSeconds < currentLine._timeInterval)
                {
                    await Task.Yield();
                    if (token.IsCancellationRequested)
                    {
                        if (lastLine != null)
                            lastLine._text.color = lastLine._originalFontColor;
                        return;
                    }

                    Stopwatch minusStopwatch = null;
                    if (_playingList.Pause)
                        minusStopwatch = Stopwatch.StartNew();
                    while (_playingList.Pause)
                    {
                        await Task.Yield();
                    }

                    if (minusStopwatch == null) 
                        continue;
                    minusSeconds += minusStopwatch.Elapsed.TotalSeconds;
                    minusStopwatch.Stop();
                }

                stopwatch.Stop();
                //��ͣ��ɾ����ǰ��Ŀ��Ӧ���ٹ������
                if (!_lyrics.ContainsKey(id) || !Application.isPlaying)
                    return;
#if UNITY_EDITOR
                Debug.Log((stopwatch.Elapsed.TotalSeconds - minusSeconds, currentLine._timeInterval));
#endif
                ScrollingLyric(lastLine, currentLine, controller);
            }
        }

        internal async Task InstantiateLyricAsync(int id,Transform lyricContent,Lyric.Controller controller, Texture2D album)
        {
            if(_lyrics.ContainsKey(id))
                return;
            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            LyricRequest lyricRequest = new LyricRequest(id, cookie.value);
            string resultJson = await _network.PostAsync(Network.LyricUrl, lyricRequest, true);
            LyricResult lyricResult = JsonMapper.ToObject<LyricResult>(resultJson);
            List<Line> lyric = PrepareData(lyricResult.lrc.lyric, lyricResult.tlyric?.lyric, lyricContent,controller);
            //������֤����ֹ�����������ظ����
            if (_lyrics.ContainsKey(id))
                return;
            Color backgroundColor = SampleAlbumColor(album, controller.originalBackgroundColor.a);
            _lyrics.Add(id, (lyric, backgroundColor));
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

        internal class Line:IPoolable<float,string,Transform,IMemoryPool>,IDisposable,IComparable<Line>
        {
            internal float _time;
            internal float _timeInterval;
            internal float _height;
            internal Text _text;
            internal Color _originalFontColor;

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
                _text.color = _originalFontColor;
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
                _text.color = _originalFontColor;
                _pool = null;
            }

            public void OnSpawned(float time, string lyric, Transform transform, IMemoryPool pool)
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

            internal class Factory : PlaceholderFactory<float,string,Transform,Line>
            {
                private readonly DiContainer _container;

                public Factory(DiContainer container)
                {
                    _container = container;
                }

                public override Line Create(float time, string lyric, Transform content)
                {
                    Line line = base.Create(time, lyric, content);
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
                    line._originalFontColor = line._text.color;
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
    }
}