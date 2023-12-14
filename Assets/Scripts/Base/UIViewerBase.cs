using System;
using System.Collections.Generic;
using System.Linq;
using InnerMediaPlayer.Management.UI;
using InnerMediaPlayer.Models.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace InnerMediaPlayer.Base
{
    public abstract class UIViewerBase:MonoBehaviour
    {
        /// <summary>
        /// string:UIName UIComponent:UI structure for save something
        /// </summary>
        protected Dictionary<string, UIComponent> uiComponents;

        protected UIManager uiManager;
        protected SignalBus Signal { get; private set; }

        [Inject]
        private void Initialize(SignalBus signal,UIManager uiManager)
        {
            Signal = signal;
            this.uiManager = uiManager;
        }

        /// <summary>
        /// ��"_P"��β��־ΪPanel��������list
        /// </summary>
        public virtual void Initialization()
        {
            uiComponents = new Dictionary<string, UIComponent>();
            IEnumerable<Transform> uiElements = GetComponentsInChildren<Transform>(true);
            UIViewerBase[] sonPanel = GetComponentsInChildren<UIViewerBase>(true);
            if (sonPanel.Length > 1)
            {
                IEnumerable<Transform[]> sonTransforms = sonPanel.Skip(1)
                    .Select(panel => panel.GetComponentsInChildren<Transform>(true));
                uiElements =
                    sonTransforms.Aggregate(uiElements, (current, sonTransform) => current.Except(sonTransform));
            }

            foreach (Transform child in uiElements)
            {
                if (child.name.EndsWith("_P")) continue;
                UIComponent tempComponent = new UIComponent(child);
                UIComponent.AddToDictionary(tempComponent,uiComponents);
            }
        }

        /// <summary>
        /// �ڴ�Panel��,������UI������
        /// </summary>
        /// <param name="gameObjectName">Ҫ���������������</param>
        /// <param name="rootGameObjectName">���һ��UI����������֣�û�п�Ϊ�ջ���null</param>
        internal void AddComponent<T>(string gameObjectName, string rootGameObjectName) where T : Component
        {
            GameObject gameObj = FindGameObjectInList(gameObjectName, rootGameObjectName);
            gameObj.AddComponent<T>();
        }

        /// <summary>
        /// �ڴ�Panel���½�UI���岢����UI���
        /// </summary>
        /// <typeparam name="T">UI���</typeparam>
        /// <param name="newGameObjectName">�´�����������֣������Panel�²�������</param>
        /// <param name="parentGameObject">ָ��������</param>
        internal GameObject AddComponentToNewObj<T>(string newGameObjectName, GameObject parentGameObject) where T : Component
        {
            Type[] types = { typeof(T) };
            var newGameObject = new GameObject(newGameObjectName, types);
            newGameObject.transform.SetParent(parentGameObject != null ? parentGameObject.transform : gameObject.transform);
            UIComponent.AddToDictionary(new UIComponent(newGameObject.transform),uiComponents);
            return newGameObject;
        }

        /// <summary>
        /// �ڴ�Panel���½�UI���岢����UI���
        /// </summary>
        /// <typeparam name="T">UI���</typeparam>
        /// <typeparam name="T1">UI���</typeparam>
        /// <param name="newGameObjectName">�´�����������֣������Panel�²�������</param>
        /// <param name="parentGameObject">ָ��������</param>
        internal GameObject AddComponentToNewObj<T, T1>(string newGameObjectName, GameObject parentGameObject) where T : Component
        {
            Type[] types = { typeof(T), typeof(T1) };
            GameObject newGameObject = new GameObject(newGameObjectName, types);
            newGameObject.transform.SetParent(parentGameObject != null ? parentGameObject.transform : gameObject.transform);
            UIComponent.AddToDictionary(new UIComponent(newGameObject.transform), uiComponents);
            return newGameObject;
        }

        /// <summary>
        /// �ڴ�Panel���½�UI���岢����UI���
        /// </summary>
        /// <typeparam name="T">UI���</typeparam>
        /// <typeparam name="T1">UI���</typeparam>
        /// <typeparam name="T2">UI���</typeparam>
        /// <param name="newGameObjectName">�´�����������֣������Panel�²�������</param>
        /// <param name="parentGameObject">ָ��������</param>
        internal GameObject AddComponentToNewObj<T, T1, T2>(string newGameObjectName, GameObject parentGameObject) where T : Component
        {
            Type[] types = { typeof(T), typeof(T1), typeof(T2) };
            GameObject newGameObject = new GameObject(newGameObjectName, types);
            newGameObject.transform.SetParent(parentGameObject != null ? parentGameObject.transform : gameObject.transform);
            //AddToDictionary(new UIComponent(newGameObject.transform));
            UIComponent.AddToDictionary(new UIComponent(newGameObject.transform), uiComponents);
            return newGameObject;
        }

        /// <summary>
        /// �ڴ�Panel���Ƴ����
        ///</summary>
        /// <param name="gameObj">Ҫ�Ƴ������������</param>
        internal void RemoveComponent<T>(GameObject gameObj) where T : Component => Destroy(gameObj.GetComponent<T>());

        /// <summary>
        /// �Ƴ���Panel�³�����ָ����UI����
        /// </summary>
        /// <param name="gameObj">UI����</param>
        internal void RemoveUselessGameObject(GameObject gameObj)
        {
            uiComponents.Remove(gameObj.name);
            Destroy(gameObj);
        }

        /// <summary>
        /// �Ƴ���Panel�³�����ָ����UI��������
        /// </summary>
        /// <param name="gameObjects">UI��������</param>
        internal void RemoveUselessGameObjects(GameObject[] gameObjects)
        {
            foreach (var gameObj in gameObjects)
            {
                uiComponents.Remove(gameObj.name);
                Destroy(gameObj);
            }
        }

        /// <summary>
        /// ���UIԤ���嵽������
        /// </summary>
        /// <param name="uiGameObject">UI����</param>
        /// <param name="parent">�����еĸ�����</param>
        /// <param name="newName">UI����������</param>
        /// <returns>���ؼ��س�����Ϸ����</returns>
        internal GameObject LoadUIPrefabToScene(GameObject uiGameObject, Transform parent, string newName)
        {
            var go = Instantiate(uiGameObject, uiGameObject.transform.position, uiGameObject.transform.rotation, parent);
            go.name = newName;
            UIComponent.AddToDictionary(new UIComponent(go.transform), uiComponents);
            //AddToDictionary(new UIComponent(go.transform));
            return go;
        }

        /// <summary>
        /// ���ҳ����е�UI����Panel�²�����ͬ��UI�ؼ�
        /// </summary>
        /// <param name="gameObjectName">UI��������</param>
        /// <param name="rootGameObjectName">���һ��UI����������֣�û�п���Ϊ�ջ���null</param>
        /// <returns></returns>
        internal GameObject FindGameObjectInList(string gameObjectName, string rootGameObjectName)
        {
            bool isRootNameNull = string.IsNullOrEmpty(rootGameObjectName);
            //�������ֵ���б�������ֵͬ�򷵻ز��ҵ���ֵ
            if (uiComponents.TryGetValue(gameObjectName,out UIComponent value))
            {
                if (!value.IsSame||isRootNameNull)
                {
                    return value.UIGameObject;
                }
            }

            //������������ֱ����Panel���ң�Panel�����岻������
            if (isRootNameNull)
                throw new NullReferenceException("δ�ҵ�" + gameObjectName + "���UI");
            if (!uiComponents.ContainsKey(rootGameObjectName))
            {
                return transform.Find(gameObjectName).gameObject;
            }
            //����������ĴӸ��������������������
            Transform[] childrenGroup =
                uiComponents[rootGameObjectName].UITransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childrenGroup.Where(child => child.name == gameObjectName&&child!=childrenGroup[0]))
            {
                return child.gameObject;
            }

            throw new NullReferenceException("δ�ҵ�" + gameObjectName + "���UI");
        }

        /// <summary>
        /// ��ȡָ��UI��������а���T�����T�������
        /// </summary>
        /// <typeparam name="T">�������</typeparam>
        /// <param name="gameObjectName">UI��������</param>
        /// <param name="rootGameObjectName">���һ��UI����������֣�û�п���Ϊ�ջ���null</param>
        /// <returns>���������</returns>
        internal T[] FindComponentsInChildren<T>(string gameObjectName, string rootGameObjectName) where T:Component
        {
            return FindGameObjectInList(gameObjectName, rootGameObjectName).GetComponentsInChildren<T>(true);
        }

        /// <summary>
        /// ��ȡָ��UI������������
        /// </summary>
        /// <param name="gameObjectName">UI��������</param>
        /// <param name="rootGameObjectName">���һ��UI����������֣�û�п���Ϊ�ջ���null</param>
        /// <returns>����������</returns>
        internal GameObject[] FindGameObjectsInChildren(string gameObjectName, string rootGameObjectName)
        {
            var transforms= FindGameObjectInList(gameObjectName, rootGameObjectName).GetComponentsInChildren<Transform>(true);
            var gameObjects = new GameObject[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
            {
                gameObjects[i] = transforms[i].gameObject;
            }

            return gameObjects;
        }

        /// <summary>
        /// ���ҳ����е�UI����Panel�²�����ͬ��UI�ؼ�
        /// </summary>
        /// <param name="gameObjectName">UI������������</param>
        /// <param name="rootGameObjectName">���һ����ӦUI����������֣�û�п���Ϊ�ջ���null</param>
        /// <returns></returns>
        internal GameObject[] FindGameObjectsInList(string[] gameObjectName, string[] rootGameObjectName)
        {
            var gameObjects = new GameObject[gameObjectName.Length];
            for (var i = 0; i < gameObjectName.Length; i++)
            {
                if (rootGameObjectName[i]==null)
                {
                    gameObjects[i] = FindGameObjectInList(gameObjectName[i], null);
                }
                else
                {
                    gameObjects[i] = FindGameObjectInList(gameObjectName[i], rootGameObjectName[i]);
                }
            }

            return gameObjects;
        }

        /// <summary>
        /// Ϊ�����ؽű���UI��ӽӿڣ����϶��������
        /// </summary>
        /// <param name="go">��Ϸ����</param>
        /// <param name="triggerType">�ӿ�����</param>
        /// <param name="action">�ص�ί��</param>
        internal static void AddEventTriggerInterface(GameObject go, EventTriggerType triggerType, UnityAction<BaseEventData> action)
        {
            switch (go.TryGetComponent(out EventTrigger trigger))
            {
                case true:
                    break;
                case false:
                    trigger = go.AddComponent<EventTrigger>();
                    break;
            }
            EventTrigger.Entry entry = new EventTrigger.Entry {eventID = triggerType, callback = new EventTrigger.TriggerEvent()};
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        internal static void AddEventTriggerInterface(EventTrigger eventTrigger, EventTriggerType triggerType, UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = triggerType, callback = new EventTrigger.TriggerEvent() };
            entry.callback.AddListener(action);
            eventTrigger.triggers.Add(entry);
        }
        #region UI_Base_Event

        internal void ButtonListener(Button go, UnityAction action)
        {
            go.onClick.AddListener(action);
        }
        internal void DropDownListener(Dropdown go, UnityAction<int> action)
        {
            go.onValueChanged.AddListener(action);
        }
        internal void InputFieldValueListener(InputField go, UnityAction<string> action)
        {
            go.onValueChanged.AddListener(action);
        }
        internal void InputFieldEditListener(InputField go, UnityAction<string> action)
        {
            go.onEndEdit.AddListener(action);
        }
        internal void ScrollViewListener(ScrollRect go, UnityAction<Vector2> action)
        {
            go.onValueChanged.AddListener(action);
        }
        internal void ScrollBarListener(Scrollbar go, UnityAction<float> action)
        {
            go.onValueChanged.AddListener(action);
        }
        internal void SliderListener(Slider go, UnityAction<float> action)
        {
            go.onValueChanged.AddListener(action);
        }
        internal void ToggleListener(Toggle go, UnityAction<bool> action)
        {
            go.onValueChanged.AddListener(action);
        }

        #endregion
    }
}
