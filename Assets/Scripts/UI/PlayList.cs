using System;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

#pragma warning disable IDE0051

namespace InnerMediaPlayer.UI
{
    internal class PlayList : UIViewerBase
    {
        private Button _return;
        private ScrollRect _scrollRect;
        private PlayingList _playingList;

        internal ScrollRect ScrollRect
        {
            get
            {
                return _scrollRect = _scrollRect != null ? _scrollRect : FindGameObjectInList("List", null).GetComponent<ScrollRect>();
            }
        }

        internal bool Pause => _playingList.Pause;
        internal float CurrentTime => _playingList.CurrentTime;
        internal float TotalTime => _playingList.TotalTime;
        internal float? AlreadyPlayedRate => _playingList.AlreadyPlayedRate;
        [Inject]
        private void Initialized(PlayingList playingList)
        {
            _playingList = playingList;
        }

        private void Start()
        {
            _return = FindGameObjectInList("Return", null).GetComponent<Button>();

            _return.onClick.AddListener(Return);
        }

        private void OnDestroy()
        {
            _return.onClick.RemoveAllListeners();
        }

        private void Return()
        {
            gameObject.SetActive(false);
        }

        internal bool? PlayOrPause() => _playingList.PlayOrPause();

        internal void Next() => _playingList.Next();

        internal void Previous() => _playingList.Previous();

        internal void ProcessAdjustment(float value) => _playingList.ProcessAdjustment(value);

        internal Task IterationListAsync(Action<PlayingList.Song> updateUI, Lyric lyric, long disposedSongId, bool stopByForce, Tools.CancellationTokenSource token, IProgress<TaskStatus> progress) =>
            _playingList.IterationListAsync(updateUI, lyric, disposedSongId, stopByForce, token, progress);

        internal long ForceAdd(long id, string songName, string artist, string albumUrl, AudioClip audioClip, Sprite album,
            RectTransform uiContent, Action<long> disposeLyric) =>
            _playingList.ForceAdd(id, songName, artist, albumUrl, audioClip, album, uiContent, disposeLyric);

        internal void AddToList(long id, string songName, string artist, string albumUrl, AudioClip audioClip, Sprite album,
            RectTransform uiContent, Action<long> disposeLyric) =>
            _playingList.AddToList(id, songName, artist, albumUrl, audioClip, album, uiContent, disposeLyric);

        internal bool Contains(long id) => _playingList.Contains(id);
    }
}