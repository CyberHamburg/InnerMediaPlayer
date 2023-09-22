using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Management;
using InnerMediaPlayer.Management.UI;
using InnerMediaPlayer.Models;
using InnerMediaPlayer.Tools;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;
using Network = InnerMediaPlayer.Tools.Network;
using Object = UnityEngine.Object;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace InnerMediaPlayer.Logical
{
    internal class PlayingList:IInitializable,IDisposable
    {
        private readonly AudioSource _audioSource;
        private readonly Network _network;
        private readonly Song.Factory _songFactory;
        private readonly UIElement.Factory _uiFactory;
        private readonly TaskQueue<int,CancellationToken> _taskQueue;
        private readonly UIManager _uiManager;
        private readonly PrefabManager _prefabManager;

        //��קʱ��ʱ����
        private UIElement _virtualQueueItem;
        /// <summary>
        /// ��ͬ�ֱ����£�move��ק�����ui���ĵľ���
        /// </summary>
        private Vector3 _distance;
        private Canvas _canvas;

        /// <summary>
        /// Ĭ��Ϊtrue����Ϊ��һ������Ϊ��һ��
        /// </summary>
        private bool _isClickedNextSong;

        /// <summary>
        /// trueΪ���¼�����������ui˳��Ĵ���falseΪ�߳̾���
        /// </summary>
        private bool _handledByEvent;

        internal bool Pause { get; private set; }

        private LinkedList<Song> PlayList { get; }
        private LinkedList<UIElement> UIList { get; }

        internal PlayingList(AudioSource audioSource, Network network, Song.Factory songFactory,
            UIElement.Factory uiFactory, TaskQueue<int, CancellationToken> taskQueue, UIManager uiManager,
            PrefabManager prefabManager)
        {
            PlayList = new LinkedList<Song>();
            UIList = new LinkedList<UIElement>();
            _audioSource = audioSource;
            _network = network;
            _songFactory = songFactory;
            _uiFactory = uiFactory;
            _taskQueue = taskQueue;
            _uiManager = uiManager;
            _prefabManager = prefabManager;
            _isClickedNextSong = true;
        }

        public void Initialize()
        {
            Transform viewport = _uiManager.FindUIViewer<UI.PlayList>("PlayList_P", "Canvas", "CanvasRoot").ScrollRect.viewport;
            GameObject go = Object.Instantiate(_prefabManager["PlayQueueItem"], viewport);
            go.SetActive(false);
            _virtualQueueItem = new UIElement((RectTransform)go.transform);
            _canvas = _uiManager.FindCanvas(typeof(UI.PlayList), "Canvas", "CanvasRoot").GetComponent<Canvas>();
        }

        /// <summary>
        /// ��ӵ�������˳�򲥷�
        /// </summary>
        /// <param name="id"></param>
        /// <param name="songName"></param>
        /// <param name="artist"></param>
        /// <param name="audioClip"></param>
        /// <param name="album"></param>
        /// <param name="uiContent"></param>
        /// <param name="disposeLyric"></param>
        internal void AddToList(int id, string songName, string artist, AudioClip audioClip, Sprite album, RectTransform uiContent, Action<int> disposeLyric)
        {
            Song song = _songFactory.Create(id, songName, artist, audioClip, album);
            if (!PlayList.Contains(song))
            {
                PlayList.AddLast(song);
                UIElement ui = _uiFactory.Create(id, songName, artist, album, uiContent);
                ui._delete.onClick.AddListener(Delete);
                void Delete()
                {
                    _handledByEvent = true;
                    UIList.Remove(ui);
                    ui.Dispose();
                    if (song == PlayList.First.Value)
                        _audioSource.Stop();
                    PlayList.Remove(song);
                    song.Dispose();
                    disposeLyric(id);
                }

                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.Drag, Drag);
                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.BeginDrag, BeginDrag);
                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.EndDrag, EndDrag);

                void Drag(BaseEventData eventData)
                {
                    if (!(eventData is PointerEventData pointer))
                        return;
                    Vector3 position = RectTransformUtility.PixelAdjustPoint(pointer.position, ui._element, _canvas);
                    position = uiContent.InverseTransformPoint(position + _distance);
                    _virtualQueueItem._element.localPosition = position;
					_virtualQueueItem._element.anchoredPosition += uiContent.anchoredPosition;
                }

                void BeginDrag(BaseEventData eventData)
                {
                    _virtualQueueItem.Reassign(songName, artist, album);
                    _virtualQueueItem._element.gameObject.SetActive(true);
                    _distance = ui._element.position - ui._move.position;
                }

                void EndDrag(BaseEventData eventData)
                {
                    _virtualQueueItem._element.gameObject.SetActive(false);
                    UIElement uiElement = null;
                    float distance = float.MaxValue;
                    //��ȡ��Ŀ�������Ԫ��
                    foreach (UIElement element in UIList)
                    {
                        float result = _virtualQueueItem._element.transform.position.y - element._element.transform.position.y;
                        if (!(Mathf.Abs(result) < Mathf.Abs(distance)))
                            continue;
                        uiElement = element;
                        distance = result;
                    }

                    //����뱾��������򷵻�
                    if (ui.Equals(uiElement))
                        return;
                    //�����Ԫ���������еĽڵ�
                    LinkedListNode<UIElement> elementNode = UIList.Find(uiElement);
                    int targetIndex = FindIndex(UIList, uiElement);
                    int currentIndex = FindIndex(UIList, ui);

                    int FindIndex<T>(LinkedList<T> list, T value)
                    {
                        if (value == null)
                            return default;
                        int index = 0;
                        LinkedListNode<T> node = list.First;
                        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                        while (node != null && !comparer.Equals(node.Value, value))
                        {
                            index++;
                            node = node.Next;
                        }

                        return index;
                    }
                    
                    LinkedListNode<Song> songNode = PlayList.Find(_songFactory.Create(uiElement._id, songName, artist, audioClip, album));

                    if (currentIndex < targetIndex && distance > 0)
                        targetIndex--;
                    if (currentIndex > targetIndex && distance < 0)
                        targetIndex++;

                    if (distance > 0)
                    {
                        //�����ק���ǵ�һ��Ԫ��
                        if (PlayList.Find(song) == PlayList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeBefore(PlayList.First, songNode, PlayList);
                            MoveNodeBefore(UIList.First, elementNode, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        //����Ԫ����ק���˵�һλ
                        if (elementNode == UIList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeBefore(PlayList.Find(song), PlayList.First, PlayList);
                            MoveNodeBefore(UIList.Find(ui), UIList.First, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        UIList.Remove(ui);
                        PlayList.Remove(song);
                        UIList.AddBefore(elementNode, ui);
                        PlayList.AddBefore(songNode, song);
                    }
                    else
                    {
                        //�����ק���ǵ�һ��Ԫ��
                        if (PlayList.Find(song) == PlayList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeAfter(PlayList.First, songNode, PlayList);
                            MoveNodeAfter(UIList.First, elementNode, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        UIList.Remove(ui);
                        PlayList.Remove(song);
                        UIList.AddAfter(elementNode, ui);
                        PlayList.AddAfter(songNode, song);
                    }

                    ui._element.SetSiblingIndex(targetIndex);

                    void MoveNodeBefore<T>(LinkedListNode<T> node, LinkedListNode<T> other, LinkedList<T> list)
                    {
                        list.Remove(node);
                        list.AddBefore(other, node);
                    }

                    void MoveNodeAfter<T>(LinkedListNode<T> node, LinkedListNode<T> other, LinkedList<T> list)
                    {
                        list.Remove(node);
                        list.AddAfter(other, node);
                    }
                }

                ui._element.SetAsLastSibling();
                UIList.AddLast(ui);
            }
            else
                Debug.Log($"������:{songName}�Ѿ�����ӹ�");
        }

        /// <summary>
        /// ��Ӻ�ֹͣ���ŵ�ǰ��Ŀ
        /// </summary>
        /// <param name="id"></param>
        /// <param name="songName"></param>
        /// <param name="artist"></param>
        /// <param name="audioClip"></param>
        /// <param name="album"></param>
        /// <param name="uiContent"></param>
        /// <param name="disposeLyric"></param>
        internal int ForceAdd(int id, string songName, string artist, AudioClip audioClip, Sprite album,RectTransform uiContent,Action<int> disposeLyric)
        {
            Song song = _songFactory.Create(id, songName, artist, audioClip, album);
            //ȡ�����Ƴ���ǰ���ŵ���Ŀ
            int disposedId = default;
            if (PlayList.Count>0)
            {
                LinkedListNode<Song> currentPlaying = PlayList.First;
                disposedId = currentPlaying.Value._id;
                PlayList.RemoveFirst();
            }
            //����б�����ӹ��������ŵ���λ
            if (PlayList.Contains(song))
                PlayList.Remove(song);
            PlayList.AddFirst(song);
            //ͬ�������б�ui
            UIElement ui = UIList.FirstOrDefault(element => element._id == id);
            if (ui == null)
            {
                ui = _uiFactory.Create(id, songName, artist, album, uiContent);
                ui._delete.onClick.AddListener(Delete);

                void Delete()
                {
                    _handledByEvent = true;
                    UIList.Remove(ui);
                    ui.Dispose();
                    if (song == PlayList.First.Value)
                        _audioSource.Stop();
                    PlayList.Remove(song);
                    song.Dispose();
                    disposeLyric(id);
                }

                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.Drag, Drag);
                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.BeginDrag, BeginDrag);
                UIViewerBase.AddEventTriggerInterface(ui._eventTrigger, EventTriggerType.EndDrag, EndDrag);

                void Drag(BaseEventData eventData)
                {
                    if (!(eventData is PointerEventData pointer))
                        return;
                    Vector3 position = RectTransformUtility.PixelAdjustPoint(pointer.position, ui._element, _canvas);
                    position = uiContent.InverseTransformPoint(position + _distance);
                    _virtualQueueItem._element.localPosition = position;
                    _virtualQueueItem._element.anchoredPosition += uiContent.anchoredPosition;
                }

                void BeginDrag(BaseEventData eventData)
                {
                    _virtualQueueItem.Reassign(songName, artist, album);
                    _virtualQueueItem._element.gameObject.SetActive(true);
                    _distance = ui._element.position - ui._move.position;
                }

                void EndDrag(BaseEventData eventData)
                {
                    _virtualQueueItem._element.gameObject.SetActive(false);
                    UIElement uiElement = null;
                    float distance = float.MaxValue;
                    //��ȡ��Ŀ�������Ԫ��
                    foreach (UIElement element in UIList)
                    {
                        float result = _virtualQueueItem._element.transform.position.y - element._element.transform.position.y;
                        if (!(Mathf.Abs(result) < Mathf.Abs(distance)))
                            continue;
                        uiElement = element;
                        distance = result;
                    }

                    //����뱾��������򷵻�
                    if (ui.Equals(uiElement))
                        return;
                    //�����Ԫ���������еĽڵ�
                    LinkedListNode<UIElement> elementNode = UIList.Find(uiElement);
                    int targetIndex = FindIndex(UIList, uiElement);
                    int currentIndex = FindIndex(UIList, ui);

                    int FindIndex<T>(LinkedList<T> list, T value)
                    {
                        if (value == null)
                            return default;
                        int index = 0;
                        LinkedListNode<T> node = list.First;
                        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                        while (node != null && !comparer.Equals(node.Value, value))
                        {
                            index++;
                            node = node.Next;
                        }

                        return index;
                    }

                    LinkedListNode<Song> songNode = PlayList.Find(_songFactory.Create(uiElement._id, songName, artist, audioClip, album));

                    if (currentIndex < targetIndex && distance > 0)
                        targetIndex--;
                    if (currentIndex > targetIndex && distance < 0)
                        targetIndex++;

                    if (distance > 0)
                    {
                        //�����ק���ǵ�һ��Ԫ��
                        if (PlayList.Find(song) == PlayList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeBefore(PlayList.First, songNode, PlayList);
                            MoveNodeBefore(UIList.First, elementNode, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        //����Ԫ����ק���˵�һλ
                        if (elementNode == UIList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeBefore(PlayList.Find(song), PlayList.First, PlayList);
                            MoveNodeBefore(UIList.Find(ui), UIList.First, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        UIList.Remove(ui);
                        PlayList.Remove(song);
                        UIList.AddBefore(elementNode, ui);
                        PlayList.AddBefore(songNode, song);
                    }
                    else
                    {
                        //�����ק���ǵ�һ��Ԫ��
                        if (PlayList.Find(song) == PlayList.First)
                        {
                            _handledByEvent = true;
                            MoveNodeAfter(PlayList.First, songNode, PlayList);
                            MoveNodeAfter(UIList.First, elementNode, UIList);
                            Pause = false;
                            _audioSource.Stop();
                            ui._element.SetSiblingIndex(targetIndex);
                            return;
                        }

                        UIList.Remove(ui);
                        PlayList.Remove(song);
                        UIList.AddAfter(elementNode, ui);
                        PlayList.AddAfter(songNode, song);
                    }

                    ui._element.SetSiblingIndex(targetIndex);

                    void MoveNodeBefore<T>(LinkedListNode<T> node, LinkedListNode<T> other, LinkedList<T> list)
                    {
                        list.Remove(node);
                        list.AddBefore(other, node);
                    }

                    void MoveNodeAfter<T>(LinkedListNode<T> node, LinkedListNode<T> other, LinkedList<T> list)
                    {
                        list.Remove(node);
                        list.AddAfter(other, node);
                    }
                }
            }
            else
                UIList.Remove(ui);

            UIList.AddFirst(ui);
            ui._element.SetAsFirstSibling();

            if (disposedId != id && disposedId != default)
            {
                UIElement disposedElement = UIList.FirstOrDefault(element => element._id == disposedId);
                UIList.Remove(disposedElement);
                disposedElement.Dispose();
            }

            if (_audioSource.isPlaying)
                _audioSource.Stop();
            return disposedId;
        }

        /// <returns>
        /// <para>Ŀǰ������ͣ�л��ǲ����У�</para>
        /// <para>�����null�򲥷��б�Ԫ��Ϊ��</para>
        /// </returns>
        internal bool? PlayOrPause()
        {
            if (PlayList.Count == 0)
                return null;
            if (_audioSource.isPlaying)
            {
                _audioSource.Pause();
                Pause = true;
            }
            else
            {
                _audioSource.UnPause();
                Pause = false;
            }
            return Pause;
        }

        /// <summary>
        /// ��һ��
        /// </summary>
        internal void Next()
        {
            if (PlayList.Count < 2)
                return;
            _audioSource.Stop();
            _isClickedNextSong = true;
        }

        /// <summary>
        /// ��һ��
        /// </summary>
        internal void Previous()
        {
            if (PlayList.Count < 2)
                return;
            _audioSource.Stop();
            _isClickedNextSong = false;
        }

        /// <summary>
        /// ������ǰ�����б��¼�������������±�����
        /// </summary>
        /// <param name="updateUI">�������·���������ui����</param>
        /// <param name="updateLyric">���¸�ʵķ���</param>
        /// <param name="disposeLyric">����ʷ����ڴ�صķ���</param>
        /// <param name="disableLyric">��ʱ���ظ�ʵķ���</param>
        /// <param name="disposedSongId">Ҫ���ٵĸ���id</param>
        /// <param name="stopByForce">�Ƿ�ǿ��ֹͣ����</param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal async Task IterationList(Action<Song> updateUI,Func<int,CancellationToken,Task> updateLyric,Action<int> disposeLyric,Action<int> disableLyric,int disposedSongId, bool stopByForce, CancellationToken token)
        {
            #region Log

            foreach (Song song in PlayList)
            {
                Debug.Log(song._songName);
            }

            #endregion

            LinkedListNode<Song> currentPlaying = PlayList.First;
            //�ж��Ƿ���Ҫ�Ƴ����
            if (disposedSongId != default && disposedSongId != currentPlaying.Value._id)
                disposeLyric(disposedSongId);

            while (PlayList.Count > 0)
            {
                switch (stopByForce)
                {
                    case true:
                        Play();
                        break;
                    case false when !_audioSource.isPlaying:
                        Play();
                        break;
                }


                void Play()
                {
                    _audioSource.clip = currentPlaying.Value._audioClip;
                    updateUI(currentPlaying.Value);
                    _taskQueue.AddTask(currentPlaying.Value._id, updateLyric);
                    _audioSource.Play();
                }

                //���Ż���ͣʱ�ó���ǰ�߳�
                while (_audioSource.isPlaying || Pause && PlayList.Count > 0)
                {
                    await Task.Yield();
                    if (token.IsCancellationRequested || !Application.isPlaying)
                        return;
                }

                //������һ�׸��򱻸����жϺ����ص�ǰ��ʣ�Ϊ��һ�׸�׼��
                disableLyric(currentPlaying.Value._id);
                //�������жϺ�����pause״̬
                Pause = false;
                if(!_handledByEvent)
                    //�жϲ���ǰһ�׸軹����һ�׸�
                    switch (_isClickedNextSong)
                    {
                        case true:
                            MoveNodeToLast(PlayList.First, PlayList);
                            UIList.First?.Value._element.SetAsLastSibling();
                            MoveNodeToLast(UIList.First, UIList);
                            break;
                        case false:
                            MoveNodeToFirst(PlayList.Last, PlayList);
                            UIList.Last?.Value._element.SetAsFirstSibling();
                            MoveNodeToFirst(UIList.Last, UIList);
                            break;
                    }

                void MoveNodeToLast<T>(LinkedListNode<T> node, LinkedList<T> list)
                {
                    if (node == null) 
                        return;
                    list.RemoveFirst();
                    list.AddLast(node);
                }

                void MoveNodeToFirst<T>(LinkedListNode<T> node, LinkedList<T> list)
                {
                    if (node == null)
                        return;
                    list.RemoveLast();
                    list.AddFirst(node);
                }

                _handledByEvent = false;
                _isClickedNextSong = true;
                currentPlaying = PlayList.First;
                updateUI(currentPlaying?.Value);
            }
        }

        internal async Task<AudioClip> GetAudioClip(int id)
        {
            //�ɸ�����ȡ���������飬�������ŵ�url
            string json = await _network.Get(Network.SongUrl, false, "id", id.ToString(), "ids", $"[{id}]", "br",
                "999000");
            Debug.Log(json);
            SongResult songResult = JsonMapper.ToObject<SongResult>(json);
            DataItem item = songResult.data[0];
            AudioClip clip = await _network.GetAudioClip(item.url, item.md5, AudioType.MPEG);
            return clip;
        }

        public void Dispose()
        {
            LinkedListNode<UIElement> first = UIList.First;
            while (UIList.Count > 0 && first.Value._element != null)
            {
                first.Value.Dispose();
                UIList.RemoveFirst();
                first = UIList.First;
            }
        }

        internal class Song : IPoolable<int, string, string, AudioClip, Sprite, IMemoryPool>, IDisposable,
            IEquatable<Song>
        {
            internal int _id;
            internal string _songName;
            internal string _artist;
            internal AudioClip _audioClip;
            internal Sprite _album;
            private IMemoryPool _memoryPool;

            public bool Equals(Song other)
            {
                return other != null && _id.Equals(other._id);
            }

            public void OnDespawned()
            {
                _id = default;
                _songName = string.Empty;
                _artist = string.Empty;
                _audioClip = null;
                _album = null;
                _memoryPool = null;
            }

            public void OnSpawned(int id, string songName, string artist, AudioClip audioClip, Sprite album,
                IMemoryPool memoryPool)
            {
                _id = id;
                _songName = songName;
                _artist = artist;
                _audioClip = audioClip;
                _album = album;
                _memoryPool = memoryPool;
            }

            public void Dispose()
            {
                _memoryPool.Despawn(this);
            }

            internal class Factory : PlaceholderFactory<int, string, string, AudioClip, Sprite, Song>
            {

            }
        }

        internal class UIElement:IPoolable<int, string, string, Sprite, Transform, IMemoryPool>,IDisposable, IEquatable<UIElement>
        {
            internal RectTransform _element;
            internal EventTrigger _eventTrigger;
            internal RectTransform _move;
            internal Button _delete;
            internal int _id;

            private IMemoryPool _memoryPool;
            private Text _songName;
            private Text _artist;
            private Image _album;

            public UIElement()
            {

            }

            internal UIElement(RectTransform element)
            {
                _element = element;
                _songName= element.Find("Play/Song").GetComponent<Text>();
                _artist = element.Find("Play/Artist").GetComponent<Text>();
                _album = element.Find("Album").GetComponent<Image>();
                _move = (RectTransform)element.Find("Right/DragToMove");
                _delete = element.Find("Right/Delete").GetComponent<Button>();

                const byte alpha = 225;
                const int index = 3;
                SetColorValue(_songName, index, alpha);
                SetColorValue(_artist, index, alpha);
                SetColorValue(_album, index, alpha);
                SetColorValue(_move.GetComponent<Image>(), index, alpha);
                SetColorValue(_delete.GetComponent<Image>(), index, alpha);
            }

            private static void SetColorValue<T>(T t, int index, byte value) where T : Graphic
            {
                Color32 color = t.color;
                color[index] = value;
                t.color = color;
            }

            internal void Reassign(string songName, string artist, Sprite album)
            {
                _songName.text = songName;
                _artist.text = artist;
                _album.sprite = album;
            }

            public void OnDespawned()
            {
                _element.gameObject.SetActive(false);
                _element.SetAsLastSibling();
                _delete.onClick.RemoveAllListeners();
                if (_delete.TryGetComponent(out EventTrigger eventTrigger))
                {
                    foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                    {
                        entry.callback.RemoveAllListeners();
                    }
                }
                _id = default;
                _songName.text = null;
                _artist.text = null;
                _album.sprite = null;
                _memoryPool = null;
            }

            public void OnSpawned(int id, string songName, string artist, Sprite album, Transform content, IMemoryPool memoryPool)
            {
                if (_element != null && _songName != null && _artist != null && _album != null)
                {
                    _element.gameObject.SetActive(true);
                    _songName.text = songName;
                    _artist.text = artist;
                    _album.sprite = album;
                }

                _id = id;
                _memoryPool = memoryPool;
            }

            public void Dispose()
            {
                _memoryPool.Despawn(this);
            }

            public bool Equals(UIElement other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return _id == other._id;
            }

            internal class Factory : PlaceholderFactory<int, string, string, Sprite, Transform, UIElement>
            {
                private readonly DiContainer _container;

                public Factory(DiContainer container)
                {
                    _container = container;
                }

                public override UIElement Create(int id,string songName, string artist, Sprite album, Transform content)
                {
                    UIElement ui = base.Create(id, songName, artist, album, content);
                    if (ui._songName == null)
                    {
                        GameObject item = _container.InstantiatePrefabResource("PlayQueueItem", content);
                        ui._element = (RectTransform)item.transform;
                        ui._songName = item.transform.Find("Play/Song").GetComponent<Text>();
                        ui._artist = item.transform.Find("Play/Artist").GetComponent<Text>();
                        ui._album = item.transform.Find("Album").GetComponent<Image>();
                        ui._move = (RectTransform)item.transform.Find("Right/DragToMove");
                        ui._delete = item.transform.Find("Right/Delete").GetComponent<Button>();
                        ui._eventTrigger = ui._move.gameObject.AddComponent<EventTrigger>();
                    }

                    ui._songName.text = songName;
                    ui._artist.text = artist;
                    ui._album.sprite = album;
                    ui._id = id;
                    _container.Inject(ui);
                    return ui;
                }

                public override void Validate()
                {
                    _container.InstantiatePrefabResource("PlayQueueItem");
                    _container.Instantiate<UIElement>();
                }
            }
        }
    }
}
