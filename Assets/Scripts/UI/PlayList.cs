using System;
using System.Threading;
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

        internal Task IterationListAsync(Action<PlayingList.Song> updateUI, Lyric lyric, int disposedSongId, bool stopByForce, CancellationToken token) =>
            _playingList.IterationListAsync(updateUI, lyric, disposedSongId, stopByForce, token);

        internal Task<AudioClip> GetAudioClipAsync(int id) => _playingList.GetAudioClipAsync(id);

        internal int ForceAdd(int id, string songName, string artist, AudioClip audioClip, Sprite album,
            RectTransform uiContent, Action<int> disposeLyric) =>
            _playingList.ForceAdd(id, songName, artist, audioClip, album, uiContent, disposeLyric);

        internal void AddToList(int id, string songName, string artist, AudioClip audioClip, Sprite album,
            RectTransform uiContent, Action<int> disposeLyric) =>
            _playingList.AddToList(id, songName, artist, audioClip, album, uiContent, disposeLyric);

        internal bool Contains(int id) => _playingList.Contains(id);
    }
}