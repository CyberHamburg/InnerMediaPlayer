using InnerMediaPlayer.Base;
using UnityEngine.UI;

namespace InnerMediaPlayer.UI
{
    internal class PlayList : UIViewerBase
    {
        private Button _return;
        private ScrollRect _scrollRect;

        internal ScrollRect ScrollRect
        {
            get
            {
                return _scrollRect ??= FindGameObjectInList("List", null).GetComponent<ScrollRect>();
            }
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
    }

}