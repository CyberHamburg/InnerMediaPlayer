using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
        //������
        private InputField _searchContainer;
        //�����������scrollRect���
        private ScrollRect _resultContainer;
        private GameObject _nullResult;
        private Image _tipBackground;
        private Text _tipText;
        //�����ʾ������ֺͱ���������
        private Graphic[] _graphics;
        private SearchRequestData _requestJsonData;
        private Lyric _lyric;
        private NowPlaying _nowPlaying;
        private PlayList _playList;
        private TaskQueue _searchTaskQueue;
        private TaskQueue<float, float> _tipTaskQueue;
        //ƴ�Ӹ����ַ�����Ҫ
        private StringBuilder _expansion;
        //ƴ�Ӹ�������ӳɹ���ʧ�ܵ���ʾ��
        private StringBuilder _addSongTip;
        //ƴ�Ӹ���������ظ�����ʾ��
        private StringBuilder _addRepeatedly;
        //����ͬʱ���صĸ���id
        private List<int> _loadingSongsId;
        private RectTransform[] _containerTransformArray;
        private RectTransform _canvasRectTransform;
        private CanvasScaler _canvasScaler;
        private Rect _canvasRect;
        private Dropdown _searchTypeDropDown;
        private Dictionary<RectTransform, IEnumerable<ITextCollection>> _coroutineCollection;

        [Header("Display Item Configs")]
        [SerializeField] private SongItemConfig _songItemConfig;
        [SerializeField] private UIItemConfig _artistItemConfig;
        [SerializeField] private UIItemConfig _albumItemConfig;
        private HtmlDocument _htmlDocument;
        private float _currentPageDistance;
        private int _searchedResultCounter;
        private bool _isAwakeInvoked;
        //����ʹ�õ���������
        private SearchType _searchType;
        //�ĸ����������а����յ��������
        private WhereNullResult _whereNullResult;

        [Header("Tip Configs")]
        [SerializeField] [Range(0f, 1f)] private float tipWidthMultiplier;
        [SerializeField] [Range(0f, 5f)] private float tipDisplayNum;
        [SerializeField] [Range(0f, 5f)] private float tipFadeOutNum;

        [Header("Text Roller Config")]
        [SerializeField] private float stayTimer;
        [SerializeField] private float rollSpeed;

        private const float TurnThePageDistance = 350f;
        private const string Adding = "���ڼ�����";
        private const string AddSuccessfully = "��ӳɹ�";
        private const string AddFailure = "���ʧ��,ʧ��ԭ��Ϊ";
        private const string Searching = "����������";
        private const string AddRepeatedly = "�Ѿ���ӹ�";
        private const string PageBeginningAlready = "�Ѿ�����ҳ��";
        private const string PageEndAlready = "�Ѿ���ĩҳ��";
        private const string NullResult = "�˽�����û���������";
        private const string SearchingNoInterrupt = "��������֮�����л�";
        private const string NotProvideTurnPage = "��չʾȫ�������������ǰҳ���ݲ�֧�ַ�ҳ";

        //��ʾ������Ƚ�������Ϊ����ֵ
        private float LimitedTipWidth
        {
            get
            {
                if (_canvasRectTransform == null)
                    throw new NullReferenceException($"{nameof(_canvasRectTransform)}�����÷����ڱ���ֵ֮ǰ");
                return _canvasRectTransform.sizeDelta.x * tipWidthMultiplier;
            }
        }

        private float RollSpeed
        {
            get
            {
                return Screen.height / _canvasScaler.referenceResolution.y * rollSpeed;
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
            _htmlDocument = new HtmlDocument();
            _coroutineCollection = new Dictionary<RectTransform, IEnumerable<ITextCollection>>(6);
            _isAwakeInvoked = true;
        }

        private async void Start()
        {
            _canvasRectTransform = (RectTransform)uiManager.FindCanvas(GetType(), "Canvas", "CanvasRoot").transform;
            _canvasRect = _canvasRectTransform.rect;
            _canvasScaler = _canvasRectTransform.GetComponent<CanvasScaler>();

            _lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            _nowPlaying = uiManager.FindUIViewer<NowPlaying>("NowPlaying_P", "Canvas", "CanvasRoot");
            _playList= uiManager.FindUIViewer<PlayList>("PlayList_P", "Canvas", "CanvasRoot");

            _searchTypeDropDown = FindGameObjectInList("Type", "Search").GetComponent<Dropdown>();
            _searchContainer = FindGameObjectInList("InputField", "Search").GetComponent<InputField>();
            _resultContainer = FindGameObjectInList("Display", null).GetComponent<ScrollRect>();
            _songItemConfig._songResultContainer = FindGameObjectInList("SongContent", "Display").GetComponent<RectTransform>();
            _artistItemConfig._resultContainer = FindGameObjectInList("ArtistContent", "Display").GetComponent<RectTransform>();
            _artistItemConfig._songContainer = FindGameObjectInList("ArtistSongContent", "Display").GetComponent<RectTransform>();
            _albumItemConfig._resultContainer = FindGameObjectInList("AlbumContent", "Display").GetComponent<RectTransform>();
            _albumItemConfig._songContainer = FindGameObjectInList("AlbumSongContent", "Display").GetComponent<RectTransform>();
            _containerTransformArray = new RectTransform[] {_songItemConfig._songResultContainer, _artistItemConfig._resultContainer,
                _artistItemConfig._songContainer, _albumItemConfig._resultContainer, _albumItemConfig._songContainer};
            _nullResult = FindGameObjectInList("NullResult", null);
            _artistItemConfig._returnLastPanel = FindGameObjectInList("ReturnLastPanel", "ArtistSongContent").GetComponent<Button>();
            _albumItemConfig._returnLastPanel = FindGameObjectInList("ReturnLastPanel", "AlbumSongContent").GetComponent<Button>();
            _tipBackground = FindGameObjectInList("Tip", null).GetComponent<Image>();
            _tipText = _tipBackground.GetComponentInChildren<Text>(true);
            _graphics = new Graphic[] { _tipText, _tipBackground };

            _searchContainer.onEndEdit.AddListener(SearchAndDisplay);
            _searchTypeDropDown.onValueChanged.AddListener(ChooseSearchType);

            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.EndDrag, JudgeIfTurnThePage);
            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.Drag, CalculateDragDistance);
            Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
            _requestJsonData = new SearchRequestData(cookie.value, _songItemConfig._displayNumPerPage);
            int limitNumEveryPage = int.Parse(_requestJsonData.limit);
            _songItemConfig._songItems = new List<SongDetail>(limitNumEveryPage);
            ExpandSongUINum(limitNumEveryPage, _songItemConfig._songItems, _songItemConfig._songResultContainer);
            InitializeItemConfig(limitNumEveryPage, "DisplayCellItem", _albumItemConfig);
            InitializeItemConfig(limitNumEveryPage, "DisplayCellItem", _artistItemConfig);

            _songItemConfig._songNameOriginalColor = _songItemConfig._songItems[0].NameOne.color;
            _songItemConfig._artistOriginalColor = _songItemConfig._songItems[0].NameTwo.color;
            _songItemConfig._songNameOriginalSizeX = _songItemConfig._songItems[0].NameOne.rectTransform.rect.width;
            _songItemConfig._artistOriginalSizeX = _songItemConfig._songItems[0].NameTwo.rectTransform.rect.width;
            _artistItemConfig._textOriginalSizeX = _artistItemConfig._items[0].NameOne.rectTransform.rect.width;
            _albumItemConfig._textOriginalSizeX = _albumItemConfig._items[0].NameOne.rectTransform.rect.width;
        }

        private void OnDestroy()
        {
            _searchContainer.onEndEdit.RemoveAllListeners();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_isAwakeInvoked)
                return;
            if (_canvasRect == _canvasRectTransform.rect)
                return;
            _canvasRect = _canvasRectTransform.rect;
            if (Mathf.Abs(_canvasRect.height - _canvasScaler.referenceResolution.y) > 0.1f)
                return;
            foreach (RectTransform rectTransform in _containerTransformArray)
            {
                if (rectTransform.gameObject.activeInHierarchy)
                    UpdateCoroutineCollectionValue(rectTransform);
            }

            foreach (KeyValuePair<RectTransform, IEnumerable<ITextCollection>> keyValuePair in _coroutineCollection)
            {
                foreach (ITextCollection item in keyValuePair.Value)
                {
                    //����ԭ���
                    StartNewCoroutineAndStopAllOlds(item.OriginalSizeXOne, item.NameOne, item.TextMask);
                    StartNewCoroutineAndStopAllOlds(item.OriginalSizeXTwo, item.NameTwo, item.TextMask);
                }
            }

            void StartNewCoroutineAndStopAllOlds(float originalSizeX, Text text, RectTransform textMask)
            {
                if (text == null || !text.gameObject.activeInHierarchy)
                    return;
                text.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, originalSizeX);
                text.StopAllCoroutines();
                text.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, text, textMask));
            }
        }

        private void InitializeItemConfig(int limitNumEveryPage, string prefabName, UIItemConfig itemConfig)
        {
            itemConfig._items = new CellDetail[limitNumEveryPage];
            itemConfig._songsItems = new List<SongDetail>(limitNumEveryPage);
            ExpandSongUINum(limitNumEveryPage, itemConfig._songsItems, itemConfig._songContainer);
            for (int i = 0; i < limitNumEveryPage; i++)
            {
                CellDetail cellDetail = new CellDetail()
                {
                    _root = Instantiate(_prefabManager[prefabName], itemConfig._resultContainer)
                };
                cellDetail._image = cellDetail._root.transform.Find("Image").GetComponent<Image>();
                cellDetail.TextMask = cellDetail._root.transform.Find("TextDisplayArea").GetComponent<RectTransform>();
                cellDetail._click = cellDetail._root.transform.Find("Detail").GetComponent<Button>();
                cellDetail.NameOne = cellDetail.TextMask.Find("Text").GetComponent<Text>();
                cellDetail.OriginalSizeXOne = cellDetail.NameOne.rectTransform.rect.width;
                itemConfig._items[i] = cellDetail;
            }
        }

        private void ChooseSearchType(int index)
        {
            if (_isSearching)
            {
                _searchTypeDropDown.value = (int)_searchType;
                SetPreferredSize(SearchingNoInterrupt);
                _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                return;
            }

            _artistItemConfig._songContainer.gameObject.SetActive(false);
            _albumItemConfig._songContainer.gameObject.SetActive(false);

            switch (index)
            {
                case 0:
                    _requestJsonData.type = "1";
                    _searchType = SearchType.Song;
                    _songItemConfig._songResultContainer.gameObject.SetActive(true);
                    _artistItemConfig._resultContainer.gameObject.SetActive(false);
                    _albumItemConfig._resultContainer.gameObject.SetActive(false);
                    _resultContainer.content = _songItemConfig._songResultContainer;
                    _requestJsonData.limit = _songItemConfig._displayNumPerPage.ToString();
                    for (int i = 0; i < _songItemConfig._enabledSongsCount; i++)
                    {
                        Text songName = _songItemConfig._songItems[i].NameOne;
                        Text artist = _songItemConfig._songItems[i].NameTwo;
                        RectTransform textMask = _songItemConfig._songItems[i].TextMask;
                        songName.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, songName, textMask));
                        artist.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, artist, textMask));
                    }

                    if (SetActive(WhereNullResult.Song, true)) break;
                    if (SetActive(WhereNullResult.Artist, false)) break;
                    if (SetActive(WhereNullResult.Album, false)) break;
                    break;
                case 1:
                    SetSearchType("100", SearchType.Artist, _artistItemConfig, _albumItemConfig);
                    if (SetActive(WhereNullResult.Artist, true)) break;
                    if (SetActive(WhereNullResult.Song, false)) break;
                    if (SetActive(WhereNullResult.Album, false)) break;
                    break;
                case 2:
                    SetSearchType("10", SearchType.Album, _albumItemConfig, _artistItemConfig);
                    if (SetActive(WhereNullResult.Album, true)) break;
                    if (SetActive(WhereNullResult.Song, false)) break;
                    if (SetActive(WhereNullResult.Artist, false)) break;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            void SetSearchType(string type, SearchType searchType, UIItemConfig enable, UIItemConfig disable)
            {
                _requestJsonData.type = type;
                _searchType = searchType;
                _songItemConfig._songResultContainer.gameObject.SetActive(false);
                disable._resultContainer.gameObject.SetActive(false);
                enable._resultContainer.gameObject.SetActive(true);
                _resultContainer.content = enable._resultContainer;
                _requestJsonData.limit = enable._displayNumPerPage.ToString();
                AutoSingleTextLineRoller(enable);
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

        private void AutoSingleTextLineRoller(UIItemConfig config)
        {
            for (int i = 0; i < config._enabledItemsCount; i++)
            {
                Text text = config._items[i].NameOne;
                RectTransform textMask = config._items[i].TextMask;
                text.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, text, textMask));
            }
        }

        private void UpdateCoroutineCollectionValue(RectTransform rectTransform)
        {
            IEnumerable<ITextCollection> collections;
            if (ReferenceEquals(rectTransform, _songItemConfig._songResultContainer))
                collections = _songItemConfig._songItems.Cast<ITextCollection>();
            else if (ReferenceEquals(rectTransform, _artistItemConfig._resultContainer))
                collections = _artistItemConfig._items.Cast<ITextCollection>();
            else if (ReferenceEquals(rectTransform, _artistItemConfig._songContainer))
                collections = _artistItemConfig._songsItems.Cast<ITextCollection>();
            else if (ReferenceEquals(rectTransform, _albumItemConfig._resultContainer))
                collections = _albumItemConfig._items.Cast<ITextCollection>();
            else
                collections = _albumItemConfig._songsItems.Cast<ITextCollection>();

            if (_coroutineCollection.ContainsKey(rectTransform))
                _coroutineCollection[rectTransform] = collections;
            else
                _coroutineCollection.Add(rectTransform, collections);
        }

        private IEnumerator HorizontalTextRoller(float stayTimer, float rollSpeed, Text text, RectTransform textMask)
        {
            yield return null;
            if (text.preferredWidth < text.rectTransform.rect.width)
                yield break;
            float textPreferredWidth = text.preferredWidth;
            text.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0f, textPreferredWidth);
            float endPositionX = text.rectTransform.position.x;
            text.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, textPreferredWidth);
            float beginningPositionX = text.rectTransform.position.x;
            Vector2 beginningPosition = (Vector2)text.rectTransform.position;
            Vector2 endingPosition = new Vector2(endPositionX, text.rectTransform.position.y);
            Vector2 normalizedVelocity = (endingPosition - beginningPosition).normalized;

            while (true)
            {
                yield return new WaitForSeconds(stayTimer);
                while (text.rectTransform.position.x - endingPosition.x > 0.05f)
                {
                    text.rectTransform.Translate(normalizedVelocity * rollSpeed);
                    yield return new WaitForSeconds(Time.deltaTime);
                }
                yield return new WaitForSeconds(stayTimer);
                text.rectTransform.position = new Vector2(beginningPositionX, text.rectTransform.position.y);
            }
        }

        //���������������
        private async Task FadeOut(float displayTimer, float fadeOutTimeInterval, Tools.CancellationTokenSource token, IProgress<TaskStatus> progress)
        {
            progress.Report(TaskStatus.Running);
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
                {
                    progress.Report(TaskStatus.Canceled);
                    return;
                }
            }

            if (_isSearching)
                SetPreferredSize(Searching);

            while (_isSearching)
            {
                await Task.Yield();
                if (token.IsCancellationRequested)
                {
                    progress.Report(TaskStatus.Canceled);
                    return;
                }
            }

            float fadeOutTimer = fadeOutTimeInterval;
            while (fadeOutTimer > 0f)
            {
                await new WaitForSeconds(Time.fixedDeltaTime);
                fadeOutTimer -= Time.fixedDeltaTime;
                if (token.IsCancellationRequested)
                {
                    progress.Report(TaskStatus.Canceled);
                    return;
                }
                foreach (Graphic graphic in _graphics)
                {
                    Color target = graphic.color;
                    target.a = 0f;
                    graphic.color = Color.Lerp(target, graphic.color, Mathf.Clamp01(fadeOutTimer / fadeOutTimeInterval));
                }
            }

            _tipBackground.gameObject.SetActive(false);
            progress.Report(TaskStatus.RanToCompletion);
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
            //������������
            if (_resultContainer.verticalNormalizedPosition >= 1f)
            {
                _currentPageDistance += pointerEventData.delta.y;
            }
            //������������
            else if (_resultContainer.verticalNormalizedPosition <= 0f)
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

                if (_artistItemConfig._songContainer.gameObject.activeInHierarchy || _albumItemConfig._songContainer.gameObject.activeInHierarchy)
                {
                    SetPreferredSize(NotProvideTurnPage);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    return;
                }

                //����Ϊ��һҳ
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
            //��һҳ
            else if (_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);

                if (_artistItemConfig._songContainer.gameObject.activeInHierarchy || _albumItemConfig._songContainer.gameObject.activeInHierarchy)
                {
                    SetPreferredSize(NotProvideTurnPage);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    return;
                }

                //����Ϊ���һҳ
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
        /// ������������༭ʱ����������Ҫ�����ݲ�����
        /// </summary>
        /// <param name="str">�����༭ʱ���ַ���</param>
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
                    if (str == _albumItemConfig._requestKeywords)
                        return;
                    _albumItemConfig._requestKeywords = str;
                    break;
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

        private async Task SearchAsync(CancellationTokenSource token, IProgress<TaskStatus> progress)
        {
            progress.Report(TaskStatus.Running);
            SearchedResult result;
            try
            {
                result = await GetSearchedResult();
            }
            catch (Exception e)
            {
                _isSearching = false;
                SetPreferredSize(e.Message);
                _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                progress.Report(TaskStatus.Faulted);
                return;
            }
            
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
                    EscapeWhiteSpace(result.result.songs);
                    _searchedResultCounter = result.result.songCount;
                    result.result.songs = PlaylistUtility.SortByRelationship(result.result.songs, _requestJsonData.s);
                    List<ISongBindable> relationshipSortables = result.result.songs.Cast<ISongBindable>().ToList();
                    _songItemConfig._enabledSongsCount = relationshipSortables.Count;
                    await BindSongData(relationshipSortables, _songItemConfig._songItems, _songItemConfig._songResultContainer, token, progress);
                    progress.Report(TaskStatus.Running);
                    break;
                case SearchType.Artist:
                    //�����������һЩ���δʻ���û����������Ļ�����ʾΪ�ղ����ؽ��
                    if (result.result?.artists == null)
                    {
                        ResetCellItem(WhereNullResult.Artist, _artistItemConfig);
                        _whereNullResult |= WhereNullResult.Artist;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        return;
                    }

                    ResetCellItem(WhereNullResult.Artist, _artistItemConfig);
                    EscapeWhiteSpace(result.result.artists);
                    _searchedResultCounter = result.result.artistCount;
                    result.result.artists = PlaylistUtility.SortByRelationship(result.result.artists, _requestJsonData.s);
                    _artistItemConfig._enabledItemsCount = result.result.artists.Count;
                    await BindCellData(false, Network.ArtistUrl, Network.ArtistXPath, result.result.artists, _artistItemConfig, token, progress);
                    progress.Report(TaskStatus.Running);
                    break;
                case SearchType.Album:
                    if (result.result?.albums == null)
                    {
                        ResetCellItem(WhereNullResult.Album, _albumItemConfig);
                        _whereNullResult |= WhereNullResult.Album;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        return;
                    }

                    ResetCellItem(WhereNullResult.Album, _albumItemConfig);
                    EscapeWhiteSpace(result.result.albums);
                    _searchedResultCounter = result.result.albumCount;
                    result.result.albums = PlaylistUtility.SortByRelationship(result.result.albums, _requestJsonData.s);
                    _albumItemConfig._enabledItemsCount = result.result.albums.Count;
                    await BindCellData(true, Network.AlbumUrl, Network.AlbumXPath, result.result.albums, _albumItemConfig, token, progress);
                    progress.Report(TaskStatus.Running);
                    break;
                default:
                    break;
            }
            
            progress.Report(TaskStatus.RanToCompletion);

            void EscapeWhiteSpace<T>(List<T> list) where T : IRelationshipSortable
            {
                foreach (T item in list)
                {
                    if (item.name[0] != ' ')
                        continue;
                    item.name = item.name.Substring(1);
                }
            }
        }

        /// <summary>
        /// �������ݶ�ui���и�ֵ���󶨰�������
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="uis"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task BindSongData(IList<ISongBindable> songs, IList<SongDetail> uis, RectTransform containerTransform, CancellationTokenSource token, IProgress<TaskStatus> progress)
        {
            progress.Report(TaskStatus.Running);
            //���������Ľ��ʵ�ж����ݺ�ui�İ�
            for (int i = 0; i < songs.Count; i++)
            {
                ISongBindable song = songs[i];
                GameObject go = uis[i]._root;
                Text songName = uis[i].NameOne;
                Text artist = uis[i].NameTwo;
                Button play = uis[i]._play;
                Button addList = uis[i]._addList;
                RectTransform textMask = uis[i].TextMask;

                Image album = uis[i]._album;
                try
                {
                    album.sprite = await _network.GetPictureAsync(song.al.picUrl);
                }
                catch (HttpRequestException e)
                {
                    SetPreferredSize(e.Message);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    album.sprite = null;
                }
                catch (MissingReferenceException)
                {
                    return;
                }

                //���������������������ȡ��ԭ��������
                if (token.IsCancellationRequested)
                {
                    progress.Report(TaskStatus.Canceled);
                    return;
                }

                #region ��UI��ֵ����������

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
                SongResult songResult = null;
                try
                {
                    songResult = await _network.GetSongResultDetailAsync(song.id);
                    reason = reason != 0 ? reason : songResult.data[0].CanPlay();
                }
                catch (HttpRequestException e)
                {
                    reason = CannotListenReason.NetworkError;
                    SetPreferredSize(e.Message);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                }
                catch (MissingReferenceException)
                {
                    return;
                }
                
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
                            Debug.Log($"IdΪ{song.id},����Ϊ{song.name}�ĸ�����ǿ�Ʋ���");
                        }
#endif

                        #endregion

                        //��Ӷ����ɹ���������ʾ��
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name).Append(Adding);
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(1f, 1.3f, FadeOut);

                        //��Ƶ��ӳɹ���ʾ��
                        bool isSucceed = await Play(song.id, song.name, artist.text, song.al.picUrl, album.sprite, songResult);
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
                            Debug.Log($"IdΪ{song.id},����Ϊ{song.name}�ĸ����Ѿ�����ӹ�");
#endif

                            #endregion

                            _addRepeatedly.Clear();
                            _addRepeatedly.Append(AddRepeatedly);
                            _addRepeatedly.Append(song.name);
                            SetPreferredSize(_addRepeatedly.ToString());
                            _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                            return;
                        }

                        //��Ӷ����ɹ���������ʾ��
                        _addSongTip.Clear();
                        _addSongTip.Append(song.name).Append(Adding);
                        SetPreferredSize(_addSongTip.ToString());
                        _tipTaskQueue.AddTask(1f, 1.3f, FadeOut);

                        //��Ƶ��ӳɹ���ʾ��
                        bool isSucceed = await AddToList(song.id, song.name, artist.text, song.al.picUrl, album.sprite, songResult);
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
                    Debug.Log($"���ܲ��Ÿø�����ԭ��Ϊ:{reason}");
#endif
                    songName.color = Color.gray;
                    artist.color = Color.gray;
                    addList.gameObject.SetActive(false);
                }

                if (go == null)
                    return;
                go.SetActive(true);
            }

            for (int i = 0; i < songs.Count; i++)
            {
                Text songName = uis[i].NameOne;
                Text artist = uis[i].NameTwo;
                RectTransform textMask = uis[i].TextMask;
                if (!songName.gameObject.activeInHierarchy || !artist.gameObject.activeInHierarchy)
                    break;
                songName.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, songName, textMask));
                artist.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, artist, textMask));
            }

            _isSearching = false;
            progress.Report(TaskStatus.RanToCompletion);
        }

        /// <summary>
        /// �������ݶ�ui���и�ֵ���󶨰�������
        /// </summary>
        /// <param name="result"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task BindCellData<T>(bool needConvertString2Int, string requestUrl, string jsonNodePath, List<T> list, UIItemConfig config, CancellationTokenSource token, IProgress<TaskStatus> progress) where T : CellItem
        {
            progress.Report(TaskStatus.Running);
            //���������Ľ��ʵ�ж����ݺ�ui�İ�
            for (int i = 0; i < list.Count; i++)
            {
                CellItem item = list[i];
                GameObject go = config._items[i]._root;
                Image artist = config._items[i]._image;
                Text artistText = config._items[i].NameOne;
                RectTransform textMask = config._items[i].TextMask;
                Button openDetailPage = config._items[i]._click;

                Image album = config._items[i]._image;
                album.sprite = await _network.GetPictureAsync(item.picUrl);
                artistText.text = item.name;

                //���������������������ȡ��ԭ��������
                if (token.IsCancellationRequested)
                {
                    progress.Report(TaskStatus.Canceled);
                    return;
                }
                openDetailPage.onClick.AddListener(OpenPage);

                //TODO:�������������и���
                async void OpenPage()
                {
                    _isSearching = true;
                    SetPreferredSize(Searching);
                    _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                    string htmlPage = await _network.GetAsync(requestUrl, true, "id", item.id.ToString());
                    //�Ӿ�̬html�з�������Ҫ������
                    _htmlDocument.LoadHtml(htmlPage);
                    HtmlNode node = _htmlDocument.DocumentNode.SelectSingleNode(jsonNodePath);
                    //Ϊ����û��Ԫ��
                    if (node == null)
                    {
                        config._returnLastPanel.onClick.RemoveAllListeners();
                        SetPreferredSize(NullResult);
                        _tipTaskQueue.AddTask(tipDisplayNum, tipFadeOutNum, FadeOut);
                        _isSearching = false;
                        return;
                    }
                    string standardJson = Convert2StandardJson(node.InnerText);
                    Models.Search.FullName.SearchedResult searchedResult = JsonMapper.ToObject<Models.Search.FullName.SearchedResult>(standardJson, needConvertString2Int);
                    //�򿪸�������
                    config._returnLastPanel.onClick.RemoveAllListeners();
                    config._songContainer.gameObject.SetActive(true);
                    config._resultContainer.gameObject.SetActive(false);
                    _resultContainer.content = config._songContainer;
                    config._returnLastPanel.gameObject.SetActive(true);
                    config._returnLastPanel.onClick.AddListener(() =>
                    {
                        _isSearching = false;
                        if (_searchTaskQueue.Status == TaskStatus.Running)
                            _searchTaskQueue.Stop();
                        config._songContainer.gameObject.SetActive(false);
                        config._resultContainer.gameObject.SetActive(true);
                        _resultContainer.content = config._resultContainer;
                        config._returnLastPanel.gameObject.SetActive(false);
                        AutoSingleTextLineRoller(config);
                    });

                    ResetSongItem(config._songsItems, false);
                    if (searchedResult.results == null || searchedResult.results.Count == 0)
                    {
                        _whereNullResult |= WhereNullResult.Song;
                        _nullResult.SetActive(true);
                        _isSearching = false;
                        return;
                    }

                    _searchedResultCounter = searchedResult.results.Count;
                    searchedResult.results = PlaylistUtility.SortByRelationship(searchedResult.results, _requestJsonData.s);
                    ExpandSongUINum(searchedResult.results.Count, config._songsItems, config._songContainer);
                    List<ISongBindable> relationshipSortables = searchedResult.results.Cast<ISongBindable>().ToList();
                    await BindSongData(relationshipSortables, config._songsItems, config._songContainer, token, progress);
                }

                string Convert2StandardJson(string json)
                {
                    string propertyName = typeof(Models.Search.FullName.SearchedResult).GetProperties()[0].Name;
                    return $"{{\"{propertyName}\":{json}}}";
                }

                go.SetActive(true);
            }

            for (int i = 0; i < list.Count; i++)
            {
                Text text = config._items[i].NameOne;
                RectTransform textMask = config._items[i].TextMask;
                if (!text.gameObject.activeInHierarchy)
                    break;
                text.StartCoroutine(HorizontalTextRoller(stayTimer, RollSpeed, text, textMask));
            }

            _isSearching = false;
            progress.Report(TaskStatus.RanToCompletion);
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
                song.TextMask = (RectTransform)text;
                song.NameOne = text.Find("Song").GetComponent<Text>();
                song.NameTwo = text.Find("Artist").GetComponent<Text>();
                song.OriginalSizeXOne = song.NameOne.rectTransform.rect.width;
                song.OriginalSizeXTwo = song.NameTwo.rectTransform.rect.width;
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
                throw new HttpRequestException($"���ص�״̬��{result.code}��Ϊ200,�����������");
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
                songDetail.NameOne.color = _songItemConfig._songNameOriginalColor;
                songDetail.NameTwo.color = _songItemConfig._artistOriginalColor;
                songDetail.NameOne.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, _songItemConfig._songNameOriginalSizeX);
                songDetail.NameTwo.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, _songItemConfig._artistOriginalSizeX);
            }
        }

        private void ResetCellItem(WhereNullResult whereNullResult, UIItemConfig itemConfig)
        {
            _nullResult.SetActive(false);
            if ((_whereNullResult & whereNullResult) == whereNullResult)
                _whereNullResult &= ~whereNullResult;
            foreach (CellDetail detail in itemConfig._items)
            {
                detail._root.SetActive(false);
                detail._click.onClick.RemoveAllListeners();
                detail.NameOne.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, itemConfig._textOriginalSizeX);
            }
        }
    }
}