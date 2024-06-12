using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using InnerMediaPlayer.Management;
using InnerMediaPlayer.Models;
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
        private PlaylistUtility _playlistUtility;
        private Cookies _cookies;
        private PrefabManager _prefabManager;
        //搜索框
        private InputField _searchContainer;
        //放搜索结果的scrollRect组件
        private ScrollRect _resultContainer;
        //搜索结果的容器
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
        private TaskQueue _searchTaskQueue;
        private TaskQueue<float, float> _tipTaskQueue;
        //拼接歌手字符串需要
        private StringBuilder _expansion;
        //拼接歌名与添加成功提示语
        private StringBuilder _addSongTip;
        private StringBuilder _addRepeatedly;
        private List<int> _loadingSongsId;
        private RectTransform _canvasRectTransform;
        private float _currentPageDistance;
        private int _searchedSongsCount;

        [Header("Tip Configs")]
        [SerializeField] [Range(0f, 1f)] private float tipWidthMultiplier;
        [SerializeField] [Range(0f, 5f)] private float tipDisplayNum;
        [SerializeField] [Range(0f, 5f)] private float tipFadeOutNum;

        private Color _originalSongNameColor;
        private Color _originalArtistColor;

        private const float TurnThePageDistance = 350f;
        private const string Adding = "正在加载中";
        private const string AddSuccessfully = "添加成功";
        private const string AddFailure = "添加失败,失败原因为";
        private const string Searching = "正在搜索中";
        private const string AddRepeatedly = "已经添加过";
        private const string PageBeginningAlready = "已经是首页了";
        private const string PageEndAlready = "已经是末页了";

        //提示框最大宽度将被限制为此数值
        private float LimitedTipWidth
        {
            get
            {
                if (_canvasRectTransform == null)
                    _canvasRectTransform = (RectTransform)uiManager.FindCanvas(GetType(), "Canvas", "CanvasRoot").transform;
                return _canvasRectTransform.sizeDelta.x * tipWidthMultiplier;
            }
        }

        [Inject]
        private void Initialized(Network network, PrefabManager prefabManager, Crypto crypto, Cookies cookies, 
            TaskQueue searchTaskQueue, TaskQueue<float, float> tipsTaskQueue, PlaylistUtility playlistUtility)
        {
            _network = network;
            _prefabManager = prefabManager;
            _crypto = crypto;
            _cookies = cookies;
            _searchTaskQueue = searchTaskQueue;
            _tipTaskQueue = tipsTaskQueue;
            _playlistUtility = playlistUtility;
        }

        private void Awake()
        {
            _expansion = new StringBuilder(35);
            _addSongTip = new StringBuilder(100);
            _addRepeatedly = new StringBuilder(130);
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

        //搜索动作特殊计算
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

            if (_isSearching)
                SetPreferredSize(Searching);

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

        private void SetPreferredSize(string message)
        {
            _tipText.text = message;
            SetPreferredSize(LimitedTipWidth, _tipText, _tipBackground.rectTransform);
        }

        private void CalculateDragDistance(BaseEventData eventData)
        {
            if (_isSearching || !(eventData is PointerEventData pointerEventData))
                return;
            //翻到了最上面
            if (_resultContainer.verticalNormalizedPosition >= 1)
            {
                _currentPageDistance += pointerEventData.delta.y;
            }
            //翻到了最下面
            else if (_resultContainer.verticalNormalizedPosition <= 0)
            {
                _currentPageDistance += pointerEventData.delta.y;
            }
        }

        /// <summary>
        /// 自动翻页的实现
        /// </summary>
        /// <param name="eventData"></param>
        private void JudgeIfTurnThePage(BaseEventData eventData)
        {
            //向上delta为负，所以用负值判断翻上一页
            if (-_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                int page = offset / limit;
                //限制为第一页
                if (page <= 0)
                {
                    SetPreferredSize(PageBeginningAlready);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    return;
                }
                _requestJsonData.offset = (--page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                _searchTaskQueue.AddTask(SearchSongAsync);
            }
            //下一页
            else if (_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                //限制为最后一页
                if (offset + limit >= _searchedSongsCount)
                {
                    SetPreferredSize(PageEndAlready);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
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
        /// 当搜索框结束编辑时生成搜索需要的数据并搜索
        /// </summary>
        /// <param name="str">结束编辑时的字符串</param>
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
            SetPreferredSize(Searching);
            _tipTaskQueue.AddTask(0f, tipFadeOutNum, FadeOut);
            string json = await _network.PostAsync(Network.SearchUrl, true);
            SearchedResult result = JsonMapper.ToObject<SearchedResult>(json);
#if UNITY_DEBUG
            Debug.Log(json);
#endif

            //重新搜索后重置SongItem状态
            ResetSongItem();

            if (result.code != 200)
            {
                _isSearching = false;
                throw new HttpRequestException($"返回的状态码{result.code}不正确,检查网络问题");
            }
            //如果搜索到是一些屏蔽词或者没有搜索结果的话，提示为空并返回结果
            if (result.result?.songs == null)
            {
                _nullSongResult.transform.SetAsFirstSibling();
                _nullSongResult.SetActive(true);
                _isSearching = false;
                return;
            }

            //对搜索到的歌曲数量计数
            _searchedSongsCount = result.result.songCount;

            //对搜索结果进行相关度排序
            PlaylistUtility.SortByRelationship(result, _requestJsonData.s);

            //对搜索到的结果实行对数据和ui的绑定
            for (int i = 0; i < result.result.songs.Count; i++)
            {
                SongsItem song = result.result.songs[i];
                GameObject go = _songItems[i]._root;
                Text songName = _songItems[i]._songName;
                Text artist = _songItems[i]._artist;
                Button play = _songItems[i]._play;
                Button addList = _songItems[i]._addList;

                Image album = _songItems[i]._album;
                album.sprite = await _network.GetAlbum(song.al.picUrl);

                //如果有新搜索动作产生则取消原搜索动作
                if (token.IsCancellationRequested)
                    return;

                #region 向UI赋值歌名和作家

                songName.text = song.name;
                _expansion.Clear();
                foreach (ArItem arItem in song.ar)
                {
                    _expansion.Append(arItem.name);
                    _expansion.Append(',');
                }

                _expansion.Remove(_expansion.Length - 1, 1);
                artist.text = _expansion.ToString();

                #endregion

				CannotListenReason reason = song.CanPlay();
                SongResult songDetail = await _network.GetSongResultDetailAsync(song.id);
                reason = reason != 0 ? reason : songDetail.data[0].CanPlay();
                if (reason == CannotListenReason.None)
                {
                    play.onClick.AddListener(PlayLocalMethod);
                    addList.onClick.AddListener(AddLocalMethod);

                    async void PlayLocalMethod()
                    {
                        #region Log

#if UNITY_DEBUG

                        bool isAdded = _loadingSongsId.Contains(song.id) || _playList.Contains(song.id);
                        if (isAdded)
                        {
                            Debug.Log($"Id为{song.id},名称为{song.name}的歌曲被强制播放");
                        }
#endif

                        #endregion

                        //添加动作成功触发的提示语
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name).Append(Adding);
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(1f, 1.3f, FadeOut);

                        //音频添加成功提示语
                        bool isSucceed = await Play(song.id, song.name, artist.text, song.al.picUrl, album.sprite, songDetail);
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name);
                        _addSongTip.Append(isSucceed ? AddSuccessfully : $"{AddFailure}{reason}");
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(1f, 1.3f, FadeOut);

                    }

                    async void AddLocalMethod()
                    {
                        bool isAdded = _loadingSongsId.Contains(song.id) || _playList.Contains(song.id);
                        if (isAdded)
                        {
                            #region Log

#if UNITY_DEBUG
                            Debug.Log($"Id为{song.id},名称为{song.name}的歌曲已经被添加过");
#endif

                            #endregion

                            _addRepeatedly.Clear();
                            _addRepeatedly.Append(AddRepeatedly);
                            _addRepeatedly.Append(song.name);
                            SetPreferredSize(_addRepeatedly.ToString());
                            _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                            return;
                        }

                        //添加动作成功触发的提示语
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name).Append(Adding);
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(1f, 1.3f, FadeOut);

                        //音频添加成功提示语
                        bool isSucceed = await AddToList(song.id, song.name, artist.text, song.al.picUrl, album.sprite, songDetail);
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name);
                        _addSongTip.Append(isSucceed ? AddSuccessfully : $"{AddFailure}{reason}");
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    }
                }
                else
                {
#if UNITY_DEBUG
                    Debug.Log($"不能播放该歌曲的原因为:{reason}");
#endif
                    songName.color = Color.gray;
                    artist.color = Color.gray;
                    addList.gameObject.SetActive(false);
                }
                go.SetActive(true);
            }
            _isSearching = false;
        }

        private async Task<bool> Play(int id,string songName,string artist,string albumUrl, Sprite album, SongResult songResult)
        {
            _loadingSongsId.Add(id);
            bool isSuccess = await _playlistUtility.PlayAsync(id, songName, artist, albumUrl, album, _lyric, _playList, _nowPlaying, songResult);
            _loadingSongsId.Remove(id);
            return isSuccess;
        }

        private async Task<bool> AddToList(int id, string songName, string artist, string albumUrl, Sprite album, SongResult songResult)
        {
            _loadingSongsId.Add(id);
            bool isSuccess = await _playlistUtility.AddAsync(true, id, songName, artist, albumUrl, album, _lyric, _playList, _nowPlaying, songResult);
            _loadingSongsId.Remove(id);
            return isSuccess;
        }

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