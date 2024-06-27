using System;
using System.Collections;
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
using HtmlAgilityPack;
using Debug = UnityEngine.Debug;
using Network = InnerMediaPlayer.Tools.Network;
using System.Linq;

#pragma warning disable IDE0051

namespace InnerMediaPlayer.UI
{
    internal enum SearchType
    {
        Song,
        Artist,
        Album
    }

    [Flags]
    internal enum WhereNullResult
    {
        None = 0,
        Song = 1 << 0,
        Artist = 1 << 1,
        Album = 1 << 2,
        All = ~0
    }

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
        private RectTransform _songResultContainer;
        private RectTransform _artistResultContainer;
        private RectTransform _artistSongContainer;
        private GameObject _nullResult;
        private Button _returnLastPanel;
        private Image _tipBackground;
        private Text _tipText;
        //存放提示语的文字和背景的数组
        private Graphic[] _graphics;
        private SearchRequestData _requestJsonData;
        private Lyric _lyric;
        private NowPlaying _nowPlaying;
        private PlayList _playList;
        private TaskQueue _searchTaskQueue;
        private TaskQueue<float, float> _tipTaskQueue;
        //拼接歌手字符串需要
        private StringBuilder _expansion;
        //拼接歌名与添加成功、失败的提示语
        private StringBuilder _addSongTip;
        //拼接歌名与添加重复的提示语
        private StringBuilder _addRepeatedly;
        //正在同时加载的歌曲id
        private List<int> _loadingSongsId;
        private RectTransform _canvasRectTransform;
        private Dropdown _searchTypeDropDown;
        private SongItemConfig _songItemConfig;
        private ArtistItemConfig _artistItemConfig;
        private HtmlDocument _htmlDocument;
        private float _currentPageDistance;
        private int _searchedResultCounter;
        //正在使用的搜索类型
        private SearchType _searchType;
        //哪个搜索界面中包含空的搜索结果
        private WhereNullResult _whereNullResult;
        private int _enabledSongsCount;
        private int _enabledArtistsCount;

        [Header("Tip Configs")]
        [SerializeField] [Range(0f, 1f)] private float tipWidthMultiplier;
        [SerializeField] [Range(0f, 5f)] private float tipDisplayNum;
        [SerializeField] [Range(0f, 5f)] private float tipFadeOutNum;

        [Header("Text Roller Config")]
        [SerializeField] private float stayTimer;
        [SerializeField] private float rollSpeed;

        private const float TurnThePageDistance = 350f;
        private const string Adding = "正在加载中";
        private const string AddSuccessfully = "添加成功";
        private const string AddFailure = "添加失败,失败原因为";
        private const string Searching = "正在搜索中";
        private const string AddRepeatedly = "已经添加过";
        private const string PageBeginningAlready = "已经是首页了";
        private const string PageEndAlready = "已经是末页了";
        private const string NullResult = "此界面下没有搜索结果";

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
            _songItemConfig = new SongItemConfig();
            _artistItemConfig = new ArtistItemConfig();
            _htmlDocument = new HtmlDocument();
        }

        private async void Start()
        {
            _lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            _nowPlaying = uiManager.FindUIViewer<NowPlaying>("NowPlaying_P", "Canvas", "CanvasRoot");
            _playList= uiManager.FindUIViewer<PlayList>("PlayList_P", "Canvas", "CanvasRoot");

            _searchTypeDropDown = FindGameObjectInList("Type", "Search").GetComponent<Dropdown>();
            _searchContainer = FindGameObjectInList("InputField", "Search").GetComponent<InputField>();
            _resultContainer = FindGameObjectInList("Display", null).GetComponent<ScrollRect>();
            _songResultContainer = FindGameObjectInList("SongContent", "Display").GetComponent<RectTransform>();
            _artistResultContainer = FindGameObjectInList("ArtistContent", "Display").GetComponent<RectTransform>();
            _artistSongContainer = FindGameObjectInList("ArtistSongContent", "Display").GetComponent<RectTransform>();
            _nullResult = FindGameObjectInList("NullResult", null);
            _returnLastPanel = FindGameObjectInList("ReturnLastPanel", "ArtistSongContent").GetComponent<Button>();
            _tipBackground = FindGameObjectInList("Tip", null).GetComponent<Image>();
            _tipText = _tipBackground.GetComponentInChildren<Text>(true);
            _graphics = new Graphic[] { _tipText, _tipBackground };

            _searchContainer.onEndEdit.AddListener(SearchAndDisplay);
            _searchTypeDropDown.onValueChanged.AddListener(ChooseSearchType);

            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.EndDrag, JudgeIfTurnThePage);
            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.Drag, CalculateDragDistance);
            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            _requestJsonData = new SearchRequestData(cookie.value);
            int limitNumEveryPage = int.Parse(_requestJsonData.limit);
            _songItemConfig._songItems = new List<SongDetail>(limitNumEveryPage);
            _artistItemConfig._artistItems = new ArtistDetail[limitNumEveryPage];
            _artistItemConfig._songsItems = new List<SongDetail>(limitNumEveryPage);
            ExpandSongUINum(limitNumEveryPage, _songItemConfig._songItems, _songResultContainer);
            ExpandSongUINum(limitNumEveryPage, _artistItemConfig._songsItems, _artistSongContainer);
            for (int i = 0; i < limitNumEveryPage; i++)
            {
                ArtistDetail artist = new ArtistDetail()
                {
                    _root = Instantiate(_prefabManager["ArtistItem"], _artistResultContainer)
                };
                artist._artist = artist._root.transform.Find("Artist").GetComponent<Image>();
                artist._textMask = artist._root.transform.Find("TextDisplayArea").GetComponent<RectTransform>();
                artist._click = artist._root.transform.Find("Detail").GetComponent<Button>();
                artist._artistText = artist._textMask.Find("Text").GetComponent<Text>();
                _artistItemConfig._artistItems[i] = artist;
            }

            _songItemConfig._songNameOriginalColor = _songItemConfig._songItems[0]._songName.color;
            _songItemConfig._artistOriginalColor = _songItemConfig._songItems[0]._artist.color;
            _songItemConfig._songNameOriginalSizeX = _songItemConfig._songItems[0]._songName.rectTransform.rect.width;
            _songItemConfig._artistOriginalSizeX = _songItemConfig._songItems[0]._artist.rectTransform.rect.width;
        }

        private void OnDestroy()
        {
            _searchContainer.onEndEdit.RemoveAllListeners();
        }

        private void ChooseSearchType(int index)
        {
            if (_isSearching)
            {
                _searchTypeDropDown.value = (int)_searchType;
                return;
            }

            switch (index)
            {
                case 0:
                    _requestJsonData.type = "1";
                    _searchType = SearchType.Song;
                    _songResultContainer.gameObject.SetActive(true);
                    _artistResultContainer.gameObject.SetActive(false);
                    _resultContainer.content = _songResultContainer;
                    for (int i = 0; i < _enabledSongsCount; i++)
                    {
                        Text songName = _songItemConfig._songItems[i]._songName;
                        Text artist = _songItemConfig._songItems[i]._artist;
                        RectTransform textMask = _songItemConfig._songItems[i]._textMask;
                        songName.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, songName, textMask));
                        artist.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, artist, textMask));
                    }

                    if (SetActive(WhereNullResult.Song, true)) break;
                    if (SetActive(WhereNullResult.Artist, false)) break;
                    if (SetActive(WhereNullResult.Album, false)) break;
                    break;
                case 1:
                    _requestJsonData.type = "100";
                    _searchType = SearchType.Artist;
                    _songResultContainer.gameObject.SetActive(false);
                    _artistResultContainer.gameObject.SetActive(true);
                    _resultContainer.content = _artistResultContainer;
                    for (int i = 0; i < _enabledArtistsCount; i++)
                    {
                        Text artistText = _artistItemConfig._artistItems[i]._artistText;
                        RectTransform textMask = _artistItemConfig._artistItems[i]._textMask;
                        artistText.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, artistText, textMask));
                    }

                    if (SetActive(WhereNullResult.Artist, true)) break;
                    if (SetActive(WhereNullResult.Song, false)) break;
                    if (SetActive(WhereNullResult.Album, false)) break;
                    break;
                case 2:
                    _requestJsonData.type= "10";
                    _searchType = SearchType.Album;
                    if (SetActive(WhereNullResult.Album, true)) break;
                    if (SetActive(WhereNullResult.Song, false)) break;
                    if (SetActive(WhereNullResult.Artist, false)) break;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            bool SetActive(WhereNullResult type, bool activeSelf)
            {
                bool isEquals = false;
                if ((_whereNullResult & type) == type)
                {
                    isEquals = true;
                    _nullResult.SetActive(activeSelf);
                }

                return isEquals;
            }
        }

        private IEnumerator HorizontalTextRoller(float stayTimer, float rollSpeed, Text text, RectTransform textMask)
        {
            yield return null;
            if (text.preferredWidth < text.rectTransform.rect.width)
                yield break;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, text.preferredWidth);
            float beginningPositionX = textMask.position.x - textMask.rect.width / 2f;
            float endingPositionX = textMask.position.x + textMask.rect.width / 2f;
            Vector2 beginningPosition = new Vector2(beginningPositionX + text.rectTransform.rect.width / 2f, text.rectTransform.position.y);
            Vector2 endingPosition = new Vector2(endingPositionX - text.rectTransform.rect.width / 2f, text.rectTransform.position.y);
            Vector2 normalizedVelocity = (endingPosition - beginningPosition).normalized;

            text.rectTransform.position = beginningPosition;
            while (true)
            {
                yield return new WaitForSeconds(stayTimer);
                while (text.rectTransform.position.x - endingPosition.x > 0.05f)
                {
                    text.rectTransform.Translate(normalizedVelocity * rollSpeed);
                    yield return new WaitForSeconds(Time.deltaTime);
                }
                yield return new WaitForSeconds(stayTimer);
                text.rectTransform.position = new Vector2(beginningPositionX + text.rectTransform.rect.width / 2f, text.rectTransform.position.y);
            }
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
            if (_searchType != SearchType.Song)
                return;
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
                _searchTaskQueue.AddTask(SearchAsync);
            }
            //下一页
            else if (_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                //限制为最后一页
                if (offset + limit >= _searchedResultCounter)
                {
                    SetPreferredSize(PageEndAlready);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    return;
                }
                int page = offset / limit;
                _requestJsonData.offset = (++page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                _searchTaskQueue.AddTask(SearchAsync);
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
            switch (_searchType)
            {
                case SearchType.Song:
                    if (str == _songItemConfig._requestKeywords)
                        return;
                    _songItemConfig._requestKeywords = str;
                    break;
                case SearchType.Artist:
                    if (str == _artistItemConfig._requestKeywords)
                        return;
                    _artistItemConfig._requestKeywords = str;
                    break;
                case SearchType.Album:
                    return;
                default:
                    break;
            }

            _requestJsonData.s = str;
            _requestJsonData.offset = 0.ToString();
            
            string unencryptedString = JsonMapper.ToJson(_requestJsonData);
            string unescapedString = System.Text.RegularExpressions.Regex.Unescape(unencryptedString);
            string encrypt = _crypto.Encrypt(unescapedString);
            _network.UpdateFormData(Network.Params, encrypt);
            _searchTaskQueue.AddTask(SearchAsync);
        }

        private async Task SearchAsync(CancellationToken token)
        {
            SearchedResult result = await GetSearchedResult();
            switch (_searchType)
            {
                case SearchType.Song:
                    if (result.result?.songs == null)
                    {
                        ResetSongItem(_songItemConfig._songItems, true);
                        _whereNullResult |= WhereNullResult.Song;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        break;
                    }

                    ResetSongItem(_songItemConfig._songItems, true);
                    _searchedResultCounter = result.result.songCount;
                    result.result.songs = PlaylistUtility.SortByRelationship(result.result.songs, _requestJsonData.s);
                    List<ISongBindable> relationshipSortables = result.result.songs.Cast<ISongBindable>().ToList();
                    _enabledSongsCount = relationshipSortables.Count;
                    await BindSongData(relationshipSortables, _songItemConfig._songItems, token);
                    break;
                case SearchType.Artist:
                    //如果搜索到是一些屏蔽词或者没有搜索结果的话，提示为空并返回结果
                    if (result.result?.artists == null)
                    {
                        ResetArtistItem();
                        _whereNullResult |= WhereNullResult.Artist;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        return;
                    }

                    ResetArtistItem();
                    _searchedResultCounter = result.result.artistCount;
                    result.result.artists = PlaylistUtility.SortByRelationship(result.result.artists, _requestJsonData.s);
                    _enabledArtistsCount = result.result.artists.Count;
                    await BindArtistData(result, token);
                    break;
                case SearchType.Album:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 根据数据对ui进行赋值并绑定按键功能
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="uis"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task BindSongData(IList<ISongBindable> songs, IList<SongDetail> uis, CancellationToken token)
        {
            //对搜索到的结果实行对数据和ui的绑定
            for (int i = 0; i < songs.Count; i++)
            {
                ISongBindable song = songs[i];
                GameObject go = uis[i]._root;
                Text songName = uis[i]._songName;
                Text artist = uis[i]._artist;
                Button play = uis[i]._play;
                Button addList = uis[i]._addList;
                RectTransform textMask = uis[i]._textMask;

                Image album = uis[i]._album;
                album.sprite = await _network.GetPictureAsync(song.al.picUrl);

                //如果有新搜索动作产生则取消原搜索动作
                if (token.IsCancellationRequested)
                    return;

                #region 向UI赋值歌名和作家

                songName.text = song.name;
                _expansion.Clear();
                foreach (Artist arItem in song.ar)
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

            for (int i = 0; i < songs.Count; i++)
            {
                Text songName = uis[i]._songName;
                Text artist = uis[i]._artist;
                RectTransform textMask = uis[i]._textMask;
                if (!songName.gameObject.activeSelf || !artist.gameObject.activeSelf)
                    break;
                songName.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, songName, textMask));
                artist.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, artist, textMask));
            }
            _isSearching = false;
        }

        /// <summary>
        /// 根据数据对ui进行赋值并绑定按键功能
        /// </summary>
        /// <param name="result"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task BindArtistData(SearchedResult result, CancellationToken token)
        {
            //对搜索到的结果实行对数据和ui的绑定
            for (int i = 0; i < result.result.artists.Count; i++)
            {
                ArtistItem artists = result.result.artists[i];
                GameObject go = _artistItemConfig._artistItems[i]._root;
                Image artist = _artistItemConfig._artistItems[i]._artist;
                Text artistText = _artistItemConfig._artistItems[i]._artistText;
                RectTransform textMask = _artistItemConfig._artistItems[i]._textMask;
                Button openDetailPage = _artistItemConfig._artistItems[i]._click;

                Image album = _artistItemConfig._artistItems[i]._artist;
                album.sprite = await _network.GetPictureAsync(artists.picUrl);
                artistText.text = artists.name;

                //如果有新搜索动作产生则取消原搜索动作
                if (token.IsCancellationRequested)
                    return;
                openDetailPage.onClick.AddListener(OpenPage);

                //TODO:打开艺人旗下所有歌曲
                async void OpenPage()
                {
                    string htmlPage = await _network.GetAsync(Network.ArtistUrl, false, "id", artists.id.ToString());
                    //从静态html中分离所需要的数据
                    _htmlDocument.LoadHtml(htmlPage);
                    HtmlNode node = _htmlDocument.DocumentNode.SelectSingleNode("//body/div/div/div/div/div/div/div/div/textarea");
                    //为空则没有元素
                    if (node == null)
                    {
                        _returnLastPanel.onClick.RemoveAllListeners();
                        SetPreferredSize(NullResult);
                        _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                        _isSearching = false;
                        return;
                    }
                    string standardJson = Convert2StandardJson(node.InnerText);
                    Models.Search.FullName.SearchedResult searchedResult = JsonMapper.ToObject<Models.Search.FullName.SearchedResult>(standardJson);
                    //打开歌曲界面
                    _returnLastPanel.onClick.RemoveAllListeners();
                    _artistSongContainer.gameObject.SetActive(true);
                    _artistResultContainer.gameObject.SetActive(false);
                    _resultContainer.content = _artistSongContainer;
                    _returnLastPanel.gameObject.SetActive(true);
                    _returnLastPanel.onClick.AddListener(() =>
                    {
                        _artistSongContainer.gameObject.SetActive(false);
                        _artistResultContainer.gameObject.SetActive(true);
                        _resultContainer.content = _artistResultContainer;
                        _returnLastPanel.gameObject.SetActive(false);
                    });

                    ResetSongItem(_artistItemConfig._songsItems, false);
                    if (searchedResult.results == null || searchedResult.results.Count == 0)
                    {
                        _whereNullResult |= WhereNullResult.Song;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        return;
                    }

                    _searchedResultCounter = searchedResult.results.Count;
                    searchedResult.results = PlaylistUtility.SortByRelationship(searchedResult.results, _requestJsonData.s);
                    ExpandSongUINum(searchedResult.results.Count, _artistItemConfig._songsItems, _artistSongContainer);
                    List<ISongBindable> relationshipSortables = searchedResult.results.Cast<ISongBindable>().ToList();
                    await BindSongData(relationshipSortables, _artistItemConfig._songsItems, token);
                }

                string Convert2StandardJson(string json)
                {
                    string propertyName = typeof(Models.Search.FullName.SearchedResult).GetProperties()[0].Name;
                    return $"{{\"{propertyName}\":{json}}}";
                }

                go.SetActive(true);
            }

            for (int i = 0; i < result.result.artists.Count; i++)
            {
                Text artistText = _artistItemConfig._artistItems[i]._artistText;
                RectTransform textMask = _artistItemConfig._artistItems[i]._textMask;
                if (!artistText.gameObject.activeSelf)
                    break;
                artistText.StartCoroutine(HorizontalTextRoller(stayTimer, rollSpeed, artistText, textMask));
            }
            _isSearching = false;
        }

        private void ExpandSongUINum(int limit, IList<SongDetail> songs, RectTransform parent)
        {
            if (songs.Count >= limit)
                return;
            int count = songs.Count;
            for (int i = count; i < limit; i++)
            {
                SongDetail song = new SongDetail
                {
                    _root = Instantiate(_prefabManager["SongItem"], parent)
                };
                song._play = song._root.transform.Find("Play").GetComponent<Button>();
                song._addList = song._root.transform.Find("Add").GetComponent<Button>();
                song._album = song._root.transform.Find("Album").GetComponent<Image>();
                Transform text = song._root.transform.Find("Text");
                song._textMask = (RectTransform)text;
                song._songName = text.Find("Song").GetComponent<Text>();
                song._artist = text.Find("Artist").GetComponent<Text>();
                songs.Add(song);
            }
        }

        private async Task<SearchedResult> GetSearchedResult()
        {
            _isSearching = true;
            SetPreferredSize(Searching);
            _tipTaskQueue.AddTask(0f, tipFadeOutNum, FadeOut);
            string json = await _network.PostAsync(Network.SearchUrl, true);
            SearchedResult result = JsonMapper.ToObject<SearchedResult>(json);
#if UNITY_DEBUG
            Debug.Log(json);
#endif
            if (result.code != 200)
            {
                _isSearching = false;
                throw new HttpRequestException($"返回的状态码{result.code}不为200,检查网络问题");
            }

            return result;
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

        private void ResetSongItem(IList<SongDetail> songs, bool isSearchDirectly)
        {
            if (isSearchDirectly)
            {
                _nullResult.SetActive(false);
                if ((_whereNullResult & WhereNullResult.Song) == WhereNullResult.Song)
                    _whereNullResult &= ~WhereNullResult.Song;
            }

            foreach (SongDetail songDetail in songs)
            {
                songDetail._root.SetActive(false);
                songDetail._play.onClick.RemoveAllListeners();
                songDetail._addList.gameObject.SetActive(true);
                songDetail._addList.onClick.RemoveAllListeners();
                songDetail._songName.color = _songItemConfig._songNameOriginalColor;
                songDetail._artist.color = _songItemConfig._artistOriginalColor;
                songDetail._songName.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, _songItemConfig._songNameOriginalSizeX);
                songDetail._artist.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, _songItemConfig._artistOriginalSizeX);
            }
        }

        private void ResetArtistItem()
        {
            _nullResult.SetActive(false);
            if ((_whereNullResult & WhereNullResult.Artist) == WhereNullResult.Artist)
                _whereNullResult &= ~WhereNullResult.Artist;
            foreach (ArtistDetail artist in _artistItemConfig._artistItems)
            {
                artist._root.SetActive(false);
                artist._click.onClick.RemoveAllListeners();
            }
        }
    }
}