using System.Collections.Generic;
using System.Net.Http;
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

namespace InnerMediaPlayer.UI
{
    public class Search : UIViewerBase
    {
        private bool _isSearching;
        private Network _network;
        private Crypto _crypto;
        private Cookies _cookies;
        private PrefabManager _prefabManager;
        //搜索框
        private InputField _searchContainer;
        //放搜索结果的scrollRect组件
        private ScrollRect _resultContainer;
        //搜索结果的容器
        private Transform _container;
        private GameObject _nullSongResult;
        private SearchRequestData _requestJsonData;
        private Lyric _lyric;
        private NowPlaying _nowPlaying;
        private SongDetail[] _songItems;
        private TaskQueue<int,bool> _playingList;
        private float _currentPageDistance;
        private int _searchedSongsCount;

        private Color _originalSongNameColor;
        private Color _originalArtistColor;

        private const float TurnThePageDistance = 350f;

        [Inject]
        private void Initialized(Network network,PrefabManager prefabManager,Crypto crypto,Cookies cookies,TaskQueue<int,bool> playingList)
        {
            _network = network;
            _prefabManager = prefabManager;
            _crypto = crypto;
            _cookies = cookies;
            _playingList = playingList;
        }

        private async void Start()
        {
            _lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            _nowPlaying = uiManager.FindUIViewer<NowPlaying>("NowPlaying_P", "Canvas", "CanvasRoot");

            _searchContainer = FindGameObjectInList("InputField", null).GetComponent<InputField>();
            _resultContainer = FindGameObjectInList("Display", null).GetComponent<ScrollRect>();
            _container = FindGameObjectInList("Content", "Display").transform;
            _nullSongResult = FindGameObjectInList("NullSongResult", "Content");

            _searchContainer.onEndEdit.AddListener(SearchAndDisplay);

            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.EndDrag, JudgeIfTurnThePage);
            AddEventTriggerInterface(_resultContainer.gameObject, EventTriggerType.Drag, CalculateDragDistance);
            Cookies.Cookie cookie = await _cookies.GetCsrfToken();
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
        private async void JudgeIfTurnThePage(BaseEventData eventData)
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
                    return;
                _requestJsonData.offset = (--page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                await SearchSong();
            }
            //下一页
            else if (_currentPageDistance > TurnThePageDistance)
            {
                _currentPageDistance = 0;
                int offset = int.Parse(_requestJsonData.offset);
                int limit = int.Parse(_requestJsonData.limit);
                //限制为最后一页
                if (offset + limit >= _searchedSongsCount)
                    return;
                int page = offset / limit;
                _requestJsonData.offset = (++page * limit).ToString();
                string encryptRequestData = _crypto.Encrypt(_requestJsonData);
                _network.UpdateFormData(Network.Params, encryptRequestData);
                await SearchSong();
            }
        }

        /// <summary>
        /// 当搜索框结束编辑时生成搜索需要的数据并搜索
        /// </summary>
        /// <param name="str">结束编辑时的字符串</param>
        private async void SearchAndDisplay(string str)
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
            await SearchSong();
        }

        private async Task SearchSong()
        {
            _isSearching = true;
            string json = await _network.Post(Network.SearchUrl, true);
            Debug.Log(json);
            SearchedResult result = JsonMapper.ToObject<SearchedResult>(json);

            //重新搜索后重置SongItem状态
            ResetSongItem();

            if (result.code != 200)
            {
                throw new HttpRequestException($"返回的状态码{result.code}不正确,检查网络问题");
            }
            //如果搜索到是一些违禁词的话，提示为空并返回结果
            if (result.result == null)
            {
                _nullSongResult.transform.SetAsFirstSibling();
                _nullSongResult.SetActive(true);
                return;
            }
            //搜索结果为空的话就返回空
            if (result.result.songs == null)
                return;
            //对搜索到的歌曲数量计数
            _searchedSongsCount = result.result.songCount;

            //对搜索结果进行相关度排序
            SortByRelationship(result,_requestJsonData.s);

            //对搜索到的结果实行对数据和ui的绑定
            for (int i = 0; i < result.result.songs.Count; i++)
            {
                SongsItem song = result.result.songs[i];
                GameObject go = _songItems[i]._root;
                Text songName = _songItems[i]._songName;
                Text artist = _songItems[i]._artist;
                Button play = _songItems[i]._play;
                Button addList = _songItems[i]._addList;
                #region 下载歌曲封面并作为图片显示

                Texture2D texture = await _network.GetTexture(song.al.picUrl, "param", "200y200");
                Image album = _songItems[i]._album;
                if (texture != null)
                {
                    Rect rect = new Rect(0, 0, texture.width, texture.height);
                    album.sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f, 100);
                }

                #endregion

                bool canPlay = song.privilege.freeTrialPrivilege.cannotListenReason == null &&
                               song.privilege.freeTrialPrivilege.resConsumable == false;
                #region 向UI赋值歌名和作家

                songName.text = song.name;
                artist.text = string.Empty;
                for (int j = 0; j < song.ar.Count; j++)
                {
                    artist.text += song.ar[j].name + ",";
                }
                artist.text = artist.text.Remove(artist.text.Length - 1, 1);

                #endregion

                if (canPlay)
                {
                    play.onClick.AddListener(() => Play(song.id, song.name, artist.text, album.sprite));
                    addList.onClick.AddListener(() => AddToList(song.id, song.name, artist.text, album.sprite));
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
        /// 如果有任一歌手或名字完全匹配或部分匹配则优先展示，完全匹配的优先度最高
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestString"></param>
        private void SortByRelationship(SearchedResult result,string requestString)
        {
            int k = 0;
            List<SongsItem> songs = result.result.songs;
            string lower = requestString.ToLower();
            for (int i = 1; i < songs.Count; i++)
            {
                if (lower.Equals(songs[i].name.ToLower()))
                {
                    (songs[i], songs[k]) = (songs[k], songs[i]);
                    k++;
                    continue;
                }

                List<ArItem> arItems = songs[i].ar;
                foreach (ArItem arItem in arItems)
                {
                    if (!lower.Equals(arItem.name.ToLower()))
                        continue;
                    (songs[i], songs[k]) = (songs[k], songs[i]);
                    k++;
                    break;
                }
            }

            if (k == songs.Count)
                return;

            for (int i = k + 1; i < songs.Count; i++)
            {
                if (songs[i].name.ToLower().Contains(lower))
                {
                    (songs[i], songs[k]) = (songs[k], songs[i]);
                    k++;
                    continue;
                }

                List<ArItem> arItems = songs[i].ar;
                foreach (ArItem arItem in arItems)
                {
                    if (!arItem.name.ToLower().Contains(lower)) 
                        continue;
                    (songs[i], songs[k]) = (songs[k], songs[i]);
                    k++;
                    break;
                }
            }
        }

        private async void Play(int id,string songName,string artist,Sprite album)
        {
            AudioClip clip = await _nowPlaying.GetAudioClip(id);
            await _lyric.InstantiateLyric(id, album.texture);
            int disposedSongId = _nowPlaying.ForceAdd(id, songName, artist, clip, album);
            _playingList.AddTask(disposedSongId, true,_nowPlaying.IterationList);
        }

        private async void AddToList(int id, string songName, string artist, Sprite album)
        {
            AudioClip clip = await _nowPlaying.GetAudioClip(id);
            await _lyric.InstantiateLyric(id, album.texture);
            _nowPlaying.AddToList(id, songName, artist, clip, album);
            _playingList.AddTask(default, false,_nowPlaying.IterationList);
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