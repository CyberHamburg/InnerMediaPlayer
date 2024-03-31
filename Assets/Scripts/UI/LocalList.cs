using System;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using UnityEngine;
using UnityEngine.UI;

namespace InnerMediaPlayer.UI
{
    internal class LocalList : UIViewerBase
    {
        private PlaylistUtility _playlistUtility;
        private Button _confirm;
        private Button _cancel;
        private Button _exit;
        private Text _tipText;
        private RectTransform _tipBackground;
        private GameObject _background;

        private const float TipWidthMultiplier = 0.8f;

        internal const string FileDontExistTip = "本地没有读取到上次的列表文件";
        internal const string DownloadSongError = "下载歌曲时出错";

        private void Start()
        {
            _confirm = FindGameObjectInList("Confirm", "ConfirmAnchor").GetComponent<Button>();
            _cancel = FindGameObjectInList("Cancel", "CancelAnchor").GetComponent<Button>();
            _tipText = FindGameObjectInList("Tip", "TipBackground").GetComponent<Text>();
            _tipBackground = FindGameObjectInList("TipBackground", null).GetComponent<RectTransform>();
            _exit = FindGameObjectInList("Exit", "TipBackground").GetComponent<Button>();
            _background = FindGameObjectInList("Background", null);

            Lyric lyric = uiManager.FindUIViewer<Lyric>("Lyric_P", "Canvas", "CanvasRoot");
            NowPlaying nowPlaying = uiManager.FindUIViewer<NowPlaying>("NowPlaying_P", "Canvas", "CanvasRoot");
            PlayList playList = uiManager.FindUIViewer<PlayList>("PlayList_P", "Canvas", "CanvasRoot");

            _confirm.onClick.AddListener(Confirm);
            _cancel.onClick.AddListener(SetActiveFalse);

            async void Confirm()
            {
                _background.SetActive(false);
                _tipBackground.gameObject.SetActive(true);
                bool successful = await _playlistUtility.LoadAsync(_tipText, lyric, playList, nowPlaying, SetPreferredSize);
                if (successful)
                {
                    SetActiveFalse();
                    return;
                }
                _exit.gameObject.SetActive(true);
                _exit.onClick.AddListener(SetActiveFalse);
            }

            void SetActiveFalse()
            {
                gameObject.SetActive(false);
            }
        }

        private void SetPreferredSize(string message)
        {
            RectTransform rectTransform = (RectTransform)uiManager.FindCanvas(GetType(), "Canvas", "CanvasRoot").transform;
            _tipText.text = message;
            SetPreferredSize(rectTransform.sizeDelta.x * TipWidthMultiplier, _tipText, _tipBackground);
        }

        [Zenject.Inject]
        private void Initialized(PlaylistUtility playlistUtility)
        {
            _playlistUtility = playlistUtility;
        }

        private async void OnDestroy()
        {
            _cancel.onClick.RemoveAllListeners();
            _exit.onClick.RemoveAllListeners();
            _confirm.onClick.RemoveAllListeners();
            await _playlistUtility.Save();
        }
    }
}
