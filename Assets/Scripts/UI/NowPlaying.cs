using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace InnerMediaPlayer.UI
{
    internal class NowPlaying : UIViewerBase
    {
        private Lyric _lyric;
        private Logical.PlayingList _playingList;
        private PlayList _playList;

        private Image _album;
        private Text _songName;
        private Text _artist;
        private Button _openPlayList;
        private Button _playButton;
        private Button _pauseButton;
        private Button _previousSongButton;
        private Button _nextSongButton;

        [Inject]
        private void Initialized(Logical.PlayingList playingList)
        {
            _playingList = playingList;
        }

        private void Start()
        {
            _lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            _playList = uiManager.FindUIViewer<PlayList>("PlayList_P", "Canvas", "CanvasRoot");

            _album = FindGameObjectInList("Album", null).GetComponent<Image>();
            _songName = FindGameObjectInList("Song", "Album").GetComponent<Text>();
            _artist = FindGameObjectInList("Artist", "Album").GetComponent<Text>();
            _openPlayList = FindGameObjectInList("PlayList", "Icon").GetComponent<Button>();
            _playButton = FindGameObjectInList("Play", "Play").GetComponent<Button>();
            _pauseButton = FindGameObjectInList("Pause", "Play").GetComponent<Button>();
            _previousSongButton = FindGameObjectInList("Last", "Icon").GetComponent<Button>();
            _nextSongButton = FindGameObjectInList("Next", "Icon").GetComponent<Button>();

            _album.GetComponent<Button>().onClick.AddListener(LyricControl);
            _openPlayList.onClick.AddListener(PlayListControl);
            _playButton.onClick.AddListener(PlayOrPause);
            _pauseButton.onClick.AddListener(PlayOrPause);
            _nextSongButton.onClick.AddListener(Next);
            _previousSongButton.onClick.AddListener(Previous);
        }

        private void OnDestroy()
        {
            _album.GetComponent<Button>().onClick.RemoveAllListeners();
            _openPlayList.onClick.RemoveAllListeners();
            _playButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.RemoveAllListeners();
            _nextSongButton.onClick.RemoveAllListeners();
            _previousSongButton.onClick.RemoveAllListeners();
        }

        private void Next() => _playingList.Next();

        private void Previous() => _playingList.Previous();

        private void PlayOrPause()
        {
            bool? isPause = _playingList.PlayOrPause();
            if (isPause == null)
                return;
            _pauseButton.gameObject.SetActive(!isPause.Value);
            _playButton.gameObject.SetActive(isPause.Value);
        }

        private void PlayListControl()
        {
            _playList.gameObject.SetActive(true);
        }

        private void LyricControl()
        {
            _lyric.gameObject.SetActive(!_lyric.gameObject.activeSelf);
        }

        internal Task<AudioClip> GetAudioClip(int id) => _playingList.GetAudioClip(id);

        internal Task IterationList(int disposedSongId,bool stopByForce,CancellationToken token)
        {
            return _playingList.IterationList(UpdateUI, _lyric.DisplayLyric, _lyric.Dispose, _lyric.Disable,disposedSongId, stopByForce, token);
        }

        internal int ForceAdd(int id, string songName, string artist, AudioClip audioClip, Sprite album) =>
            _playingList.ForceAdd(id, songName, artist, audioClip, album, _playList.ScrollRect.content, _lyric.Dispose);


        internal void AddToList(int id, string songName, string artist, AudioClip audioClip, Sprite album) =>
            _playingList.AddToList(id, songName, artist, audioClip, album, _playList.ScrollRect.content, _lyric.Dispose);

        private void UpdateUI(Logical.PlayingList.Song song)
        {
            if (song == null)
            {
                _album.sprite = null;
                _songName.text = null;
                _artist.text = null;
                _playButton.gameObject.SetActive(true);
                _pauseButton.gameObject.SetActive(false);
                _lyric.SetDefaultColor();
                return;
            }

            _album.sprite = song._album;
            _songName.text = song._songName;
            _artist.text = song._artist;
            _pauseButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(false);
        }
    }
}
