using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using InnerMediaPlayer.Management;
using InnerMediaPlayer.Models.Search;
using InnerMediaPlayer.Tools;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;
using Debug = UnityEngine.Debug;
using Network = InnerMediaPlayer.Tools.Network;

#pragma warning disable IDE0051

namespace InnerMediaPlayer.UI
{
    internal class Search : UIViewerBase
    {
        private bool _isSearching;
        private Network _network;
        private Crypto _crypto;
        private Cookies _cookies;
        private PrefabManager _prefabManager;
        //������
        private InputField _searchContainer;
        //�����������scrollRect���
        private ScrollRect _resultContainer;
        //�������������
        private Transform _container;
        private GameObject _nullSongResult;
        private Image _tipBackground;
        private Text _tipText;
        private Graphic[] _graphics;
        private SearchRequestData _requestJsonData;
        private Lyric _lyric;
        private NowPlaying _nowPlaying;
        private PlayList _playList;
        private SongDetail[] _songItems;
        private TaskQueue<int,bool> _iterateSongListTaskQueue;
        private TaskQueue _searchTaskQueue;
        private TaskQueue<float, float> _tipTaskQueue;
        //ƴ�Ӹ����ַ�����Ҫ
        private StringBuilder _expansion;
        private List<int> _loadingSongsId;
        private float _currentPageDistance;
        private int _searchedSongsCount;

        private Color _originalSongNameColor;
        private Color _originalArtistColor;

        private const float TurnThePageDistance = 350f;
        //��ʾ������Ƚ�������Ϊ����ֵ
        private const float LimitedTipWidth = 400f;
        private const string AddSuccessful = "��ӳɹ�";
        private const string Searching = "����������";
        private const string AddRepeatedly = "�Ѿ���ӹ����׸�";
        private const string PageBeginningAlready = "�Ѿ�����ҳ��";
        private const string PageEndAlready = "�Ѿ���ĩҳ��";

        [Inject]
        private void Initialized(Network network, PrefabManager prefabManager, Crypto crypto, Cookies cookies,
            TaskQueue<int, bool> playingList, TaskQueue searchTaskQueue, TaskQueue<float, float> tipsTaskQueue)
        {
            _network = network;
            _prefabManager = prefabManager;
            _crypto = crypto;
            _cookies = cookies;
            _iterateSongListTaskQueue = playingList;
            _searchTaskQueue = searchTaskQueue;
            _tipTaskQueue = tipsTaskQueue;
        }

        private void Awake()
        {
            _expansion = new StringBuilder(35);
            _loadingSongsId = new List<int>(10);
        }

        private async void Start()
        {
            _lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            _nowPlaying = uiManager.FindUIViewer<NowPlaying>("NowPlaying_P", "Canvas", "CanvasRoot");
            _playList= uiManager.FindUIViewer<PlayList>("PlayList_P", "Canvas", "CanvasRoot");

            _searchContainer = FindGameObjectInList("InputField", null).GetComponent<InputField>();
            _resultContainer = FindGameObjectInList("Display", null).GetComponent<ScrollRect>();
            _container = FindGameObjectInList("Content", "Display").transform;
            _nullSongResult = FindGameObjectInList("NullSongResult", "Content");
            _tipBackground = FindGameObjectInList("Tip", null).GetComponent<Image>();
            _tipText = _tipBackground.GetComponentInChildren<Text>(true);
            _graphics = new Graphic[] { _tipText, _tipBackground };

            _searchContainer.onEndEdit.AddListener(SearchAndDisplay);

            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.EndDrag, JudgeIfTurnThePage);
            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.Drag, CalculateDragDistance);
            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            _requestJsonData = new SearchRequestData(cookie.value);
            _songItems = new SongDetail[int.Parse(_requestJsonData.limit)];
            for (int i = 0; i < _songItems.Length; i++)
            {
                SongDetail item = new SongDetail
                {
                    _root = Instantiate(_prefabManager["SongItem"], _container)
                };
                item._play = item._root.transform.Find("Play").GetComponent<Button>();
                item._addList = item._root.transform.Find("Add").GetComponent<Button>();
                item._album = item._root.transform.Find("Album").GetComponent<Image>();
                Transform text = item._root.transform.Find("Text");
                item._songName = text.Find("Song").GetComponent<Text>();
                item._artist = text.Find("Artist").GetComponent<Text>();
                _songItems[i] = item;
            }

            _originalArtistColor = _songItems[0]._artist.color;
            _originalSongNameColor = _songItems[0]._songName.color;
        }

        private void OnDestroy()
        {
            _searchContainer.onEndEdit.RemoveAllListeners();
        }

        private async Task FadeOut(float displayTimer, float fadeOutTimeInterval, CancellationToken token)
        {
            _tipBackground.gameObject.SetActive(true);
            foreach (Graphic graphic in _graphics)
            {
                Color color = graphic.color;
                color.a = 1f;
                graphic.color = color;
            }

            while (displayTimer > 0f)
            {
                await new WaitForSeconds(Time.fixedDeltaTime);
                displayTimer -= Time.fixedDeltaTime;
                if (token.IsCancellationRequested)
                    return;
            }

            while (_isSearching)
            {
                await Task.Yield();
                if (token.IsCancellationRequested)
                    return;
            }

            float fadeOutTimer = fadeOutTimeInterval;
            while (fadeOutTimer > 0f)
            {
                await new WaitForSeconds(Time.fixedDeltaTime);
                fadeOutTimer -= Time.fixedDeltaTime;
                if (token.IsCancellationRequested)
                    return;
                foreach (Graphic graphic in _graphics)
                {
                    Color target = graphic.color;
                    target.a = 0f;
                    graphic.color = Color.Lerp(target, graphic.color, Mathf.Clamp01(fadeOutTimer / fadeOutTimeInterval));
                }
            }

            _tipBackground.gameObject.SetActive(false);
        }

        /// <summary>
        /// �Զ�����Tip�����뱳�����С
        /// </summary>
        /// <param name="message"></param>
        /// <param name="maxWidth"></param>
        private void SetPreferredSize(string message, float maxWidth)
        {
            _tipText.text = message;
            Vector2 imageSize = _tipBackground.rectTransform.rect.size;
            Vector2 textSize = _tipText.rectTransform.rect.size;
            float heightGap = Mathf.Abs(imageSize.y - textSize.y);
            float widthGap = Mathf.Abs(imageSize.x - textSize.x);
            Vector2 originalSize = _tipText.rectTransform.sizeDelta;
            if (_tipText.preferredWidth < maxWidth)
            {
                originalSize.x = _tipText.preferredWidth;
            }
            else
            {
                originalSize.x = maxWidth;
                originalSize.y = _tipText.preferredHeight;
            }
            
            _tipText.rectTransform.sizeDelta = originalSize;
            originalSize.x += widthGap;
            originalSize.y += heightGap;
            _tipBackground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
            _tipBackground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
        }

        private void CalculateDragDistance(BaseEventData eventData)
        {
            if (_isSearching || !(eventData is PointerEventData pointerEventData))
                return;
            //������������
            if (_resultContainer.verticalNormalizedPosition >= 1)
            {
                _currentPageDistance += pointerEventData.delta.y;
            }
            //������������
            else if (_resultContainer.verticalNormalizedPosition <= 0)
            {
                _currentPageDistance += pointerEventData.delta.y;
            }
        }

        /// <summary>
        /// �Զ���ҳ��ʵ��
        /// </summary>
        /// <param name="eventData"></param>
        private void JudgeIfTurnThePage(BaseEventData eventData)
        {
            //����deltaΪ���������ø�ֵ�жϷ���һҳ
            if (-_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                int page = offset / limit;
                //����Ϊ��һҳ
                if (page <= 0)
                {
                    SetPreferredSize(PageBeginningAlready, LimitedTipWidth);
                    _tipTaskQueue.AddTask(0.3f, 2f, FadeOut);
                    return;
                }
                _requestJsonData.offset = (--page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                _searchTaskQueue.AddTask(SearchSongAsync);
            }
            //��һҳ
            else if (_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                //����Ϊ���һҳ
                if (offset + limit >= _searchedSongsCount)
                {
                    SetPreferredSize(PageEndAlready, LimitedTipWidth);
                    _tipTaskQueue.AddTask(0.3f, 2f, FadeOut);
                    return;
                }
                int page = offset / limit;
                _requestJsonData.offset = (++page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                _searchTaskQueue.AddTask(SearchSongAsync);
            }
        }

        /// <summary>
        /// ������������༭ʱ����������Ҫ�����ݲ�����
        /// </summary>
        /// <param name="str">�����༭ʱ���ַ���</param>
        private void SearchAndDisplay(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;
            if (_requestJsonData.s.Equals(str))
                return;
            _requestJsonData.s = str;
            _requestJsonData.offset = 0.ToString();
            
            string unencryptedString = JsonMapper.ToJson(_requestJsonData);
            string unescapedString = System.Text.RegularExpressions.Regex.Unescape(unencryptedString);
            string encrypt = _crypto.Encrypt(unescapedString);
            _network.UpdateFormData(Network.Params, encrypt);
            _searchTaskQueue.AddTask(SearchSongAsync);
        }

        private async Task SearchSongAsync(CancellationToken token)
        {
            _isSearching = true;
            SetPreferredSize(Searching, LimitedTipWidth);
            _tipTaskQueue.AddTask(0f, 2f, FadeOut);
            string json = await _network.PostAsync(Network.SearchUrl, true);
            SearchedResult result = JsonMapper.ToObject<SearchedResult>(json);
#if UNITY_DEBUG
            Debug.Log(json);
#endif

            //��������������SongItem״̬
            ResetSongItem();

            if (result.code != 200)
            {
                throw new HttpRequestException($"���ص�״̬��{result.code}����ȷ,�����������");
            }
            //�����������һЩ���δʻ���û����������Ļ�����ʾΪ�ղ����ؽ��
            if (result.result?.songs == null)
            {
                _nullSongResult.transform.SetAsFirstSibling();
                _nullSongResult.SetActive(true);
                return;
            }

            //���������ĸ�����������
            _searchedSongsCount = result.result.songCount;

            //���������������ض�����
            SortByRelationship(result, _requestJsonData.s);

            //���������Ľ��ʵ�ж����ݺ�ui�İ�
            for (int i = 0; i < result.result.songs.Count; i++)
            {
                SongsItem song = result.result.songs[i];
                GameObject go = _songItems[i]._root;
                Text songName = _songItems[i]._songName;
                Text artist = _songItems[i]._artist;
                Button play = _songItems[i]._play;
                Button addList = _songItems[i]._addList;
                #region ���ظ������沢��ΪͼƬ��ʾ

                Texture2D texture = await _network.GetTextureAsync(song.al.picUrl, "param", "200y200");
                Image album = _songItems[i]._album;
                if (texture != null)
                {
                    Rect rect = new Rect(0, 0, texture.width, texture.height);
                    album.sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f, 100);
                }

                #endregion

                //���������������������ȡ��ԭ��������
                if (token.IsCancellationRequested)
                    return;

                bool canPlay = song.privilege.freeTrialPrivilege.cannotListenReason == null &&
                               song.privilege.freeTrialPrivilege.resConsumable == false;
                #region ��UI��ֵ����������

                songName.text = song.name;
                artist.text = string.Empty;
                _expansion.Clear();
                foreach (ArItem arItem in song.ar)
                {
                    _expansion.Append(arItem.name);
                    _expansion.Append(',');
                }

                _expansion.Remove(_expansion.Length - 1, 1);
                artist.text = _expansion.ToString();

                #endregion

                if (canPlay)
                {
                    play.onClick.AddListener(() =>
                    {
#if UNITY_DEBUG
                        #region Log
                        bool isAdded = _loadingSongsId.Contains(song.id) || _playList.Contains(song.id);
                        if (isAdded)
                        {
                            Debug.Log($"IdΪ{song.id},����Ϊ{song.name}�ĸ�����ǿ�Ʋ���");
                        }
                        #endregion
#endif
                        Play(song.id, song.name, artist.text, album.sprite);
                        SetPreferredSize(AddSuccessful, LimitedTipWidth);
                        _tipTaskQueue.AddTask(0.3f, 2f, FadeOut);
                    });
                    addList.onClick.AddListener(() =>
                    {
                        bool isAdded = _loadingSongsId.Contains(song.id) || _playList.Contains(song.id);
                        if (isAdded)
                        {
                            #region Log

#if UNITY_DEBUG
                            Debug.Log($"IdΪ{song.id},����Ϊ{song.name}�ĸ����Ѿ�����ӹ�");
#endif

                            #endregion

                            SetPreferredSize(AddRepeatedly, LimitedTipWidth);
                            _tipTaskQueue.AddTask(0.3f, 2f, FadeOut);
                            return;
                        }
                        AddToList(song.id, song.name, artist.text, album.sprite);
                        SetPreferredSize(AddSuccessful, LimitedTipWidth);
                        _tipTaskQueue.AddTask(0.3f, 2f, FadeOut);
                    });
                }
                else
                {
                    songName.color = Color.gray;
                    artist.color = Color.gray;
                    addList.gameObject.SetActive(false);
                }
                go.SetActive(true);
            }
            _isSearching = false;
        }

        /// <summary>
        /// �������һ���ֻ�������ȫƥ��򲿷�ƥ��������չʾ����ȫƥ������ȶ����
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestString"></param>
        private void SortByRelationship(SearchedResult result,string requestString)
        {
            List<SongsItem> songs = result.result.songs;
            string lower = requestString.ToLower();

            //�������ȼ�����
            (int, int)[] indexArray = new (int, int)[songs.Count];
            for (int i = 0; i < songs.Count; i++)
            {
                //��ȫƥ�����ȼ���ߣ��򲻿����������
                if (lower.Equals(songs[i].name.ToLower()))
                {
                    indexArray[i] = (1, i);
                    continue;
                }
                //���������ȼ���Щ
                if (songs[i].name.ToLower().Contains(lower))
                    indexArray[i] = (3, i);

                List<ArItem> arItems = songs[i].ar;
                //��������ƥ���
                foreach (ArItem arItem in arItems)
                {
                    if (lower.Equals(arItem.name.ToLower()))
                    {
                        indexArray[i] = (2, i);
                        break;
                    }
                    if (arItem.name.ToLower().Contains(lower) && indexArray[i].Item1 == 0)
                        indexArray[i] = (4, i);
                }

                if (indexArray[i].Item1 == 0)
                    indexArray[i].Item2 = i;
            }

            int lastIndex = songs.Count - 1;
            int startIndex = 0;
            using IEnumerator<(int, int)> iEnumerator = indexArray.OrderBy(item => item.Item1).GetEnumerator();
            List<SongsItem> songItems = new List<SongsItem>(songs);
            //�������ȶ�����
            while (iEnumerator.MoveNext())
            {
                var (priority, index) = iEnumerator.Current;
                if (priority == 0)
                {
                    songItems[lastIndex] = songs[index];
                    lastIndex--;
                }
                else
                {
                    songItems[startIndex] = songs[index];
                    startIndex++;
                }
            }

            result.result.songs = songItems;
        }

        private async void Play(int id,string songName,string artist,Sprite album)
        {
            _loadingSongsId.Add(id);
            AudioClip clip = await _playList.GetAudioClipAsync(id);
            await _lyric.InstantiateLyric(id, album.texture);
            int disposedSongId = _playList.ForceAdd(id, songName, artist, clip, album, _playList.ScrollRect.content, _lyric.Dispose);
            _iterateSongListTaskQueue.AddTask(disposedSongId, true, IterationListAsync);
            _loadingSongsId.Remove(id);
        }

        private async void AddToList(int id, string songName, string artist, Sprite album)
        {
            _loadingSongsId.Add(id);
            AudioClip clip = await _playList.GetAudioClipAsync(id);
            await _lyric.InstantiateLyric(id, album.texture);
            _playList.AddToList(id, songName, artist, clip, album, _playList.ScrollRect.content, _lyric.Dispose);
            _iterateSongListTaskQueue.AddTask(default, false, IterationListAsync);
            _loadingSongsId.Remove(id);
        }

        private Task IterationListAsync(int disposedSongId, bool stopByForce, CancellationToken token)
            => _playList.IterationListAsync(_nowPlaying.UpdateUI, _lyric.Dispose, _lyric.Disable, disposedSongId,
                stopByForce, _lyric.LyricDisplaySignal, token);

        private void ResetSongItem()
        {
            _nullSongResult.SetActive(false);
            foreach (SongDetail songDetail in _songItems)
            {
                songDetail._root.SetActive(false);
                songDetail._play.onClick.RemoveAllListeners();
                songDetail._addList.gameObject.SetActive(true);
                songDetail._addList.onClick.RemoveAllListeners();
                songDetail._songName.color = _originalSongNameColor;
                songDetail._artist.color = _originalArtistColor;
                songDetail._album.sprite = null;
            }
        }
    }
}