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
        /// 以"_P"结尾标志为Panel，不加入list
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
        /// 在此Panel下,给已有UI添加组件
        /// </summary>
        /// <param name="gameObjectName">要加组件的物体名字</param>
        /// <param name="rootGameObjectName">随便一个UI父物体的名字，没有可为空或者null</param>
        internal void AddComponent<T>(string gameObjectName, string rootGameObjectName) where T : Component
        {
            GameObject gameObj = FindGameObjectInList(gameObjectName, rootGameObjectName);
            gameObj.AddComponent<T>();
        }

        /// <summary>
        /// 在此Panel下新建UI物体并附加UI组件
        /// </summary>
        /// <typeparam name="T">UI组件</typeparam>
        /// <param name="newGameObjectName">新创建物体的名字，如果在Panel下不可重名</param>
        /// <param name="parentGameObject">指定父物体</param>
        internal GameObject AddComponentToNewObj<T>(string newGameObjectName, GameObject parentGameObject) where T : Component
        {
            Type[] types = { typeof(T) };
            var newGameObject = new GameObject(newGameObjectName, types);
            newGameObject.transform.SetParent(parentGameObject != null ? parentGameObject.transform : gameObject.transform);
            UIComponent.AddToDictionary(new UIComponent(newGameObject.transform),uiComponents);
            return newGameObject;
        }

        /// <summary>
        /// 在此Panel下新建UI物体并附加UI组件
        /// </summary>
        /// <typeparam name="T">UI组件</typeparam>
        /// <typeparam name="T1">UI组件</typeparam>
        /// <param name="newGameObjectName">新创建物体的名字，如果在Panel下不可重名</param>
        /// <param name="parentGameObject">指定父物体</param>
        internal GameObject AddComponentToNewObj<T, T1>(string newGameObjectName, GameObject parentGameObject) where T : Component
        {
            Type[] types = { typeof(T), typeof(T1) };
            GameObject newGameObject = new GameObject(newGameObjectName, types);
            newGameObject.transform.SetParent(parentGameObject != null ? parentGameObject.transform : gameObject.transform);
            UIComponent.AddToDictionary(new UIComponent(newGameObject.transform), uiComponents);
            return newGameObject;
        }

        /// <summary>
        /// 在此Panel下新建UI物体并附加UI组件
        /// </summary>
        /// <typeparam name="T">UI组件</typeparam>
        /// <typeparam name="T1">UI组件</typeparam>
        /// <typeparam name="T2">UI组件</typeparam>
        /// <param name="newGameObjectName">新创建物体的名字，如果在Panel下不可重名</param>
        /// <param name="parentGameObject">指定父物体</param>
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
        /// 在此Panel下移除组件
        ///</summary>
        /// <param name="gameObj">要移除组件对象物体</param>
        internal void RemoveComponent<T>(GameObject gameObj) where T : Component => Destroy(gameObj.GetComponent<T>());

        /// <summary>
        /// 移除此Panel下场景中指定的UI物体
        /// </summary>
        /// <param name="gameObj">UI物体</param>
        internal void RemoveUselessGameObject(GameObject gameObj)
        {
            uiComponents.Remove(gameObj.name);
            Destroy(gameObj);
        }

        /// <summary>
        /// 移除此Panel下场景中指定的UI物体数组
        /// </summary>
        /// <param name="gameObjects">UI物体数组</param>
        internal void RemoveUselessGameObjects(GameObject[] gameObjects)
        {
            foreach (var gameObj in gameObjects)
            {
                uiComponents.Remove(gameObj.name);
                Destroy(gameObj);
            }
        }

        /// <summary>
        /// 添加UI预制体到场景里
        /// </summary>
        /// <param name="uiGameObject">UI物体</param>
        /// <param name="parent">场景中的父对象</param>
        /// <param name="newName">UI物体新名字</param>
        /// <returns>返回加载出的游戏物体</returns>
        internal GameObject LoadUIPrefabToScene(GameObject uiGameObject, Transform parent, string newName)
        {
            var go = Instantiate(uiGameObject, uiGameObject.transform.position, uiGameObject.transform.rotation, parent);
            go.name = newName;
            UIComponent.AddToDictionary(new UIComponent(go.transform), uiComponents);
            //AddToDictionary(new UIComponent(go.transform));
            return go;
        }

        /// <summary>
        /// 查找场景中的UI对象，Panel下不允许同名UI控件
        /// </summary>
        /// <param name="gameObjectName">UI对象名字</param>
        /// <param name="rootGameObjectName">随便一个UI父物体的名字，没有可以为空或者null</param>
        /// <returns></returns>
        internal GameObject FindGameObjectInList(string gameObjectName, string rootGameObjectName)
        {
            bool isRootNameNull = string.IsNullOrEmpty(rootGameObjectName);
            //如果查找值在列表中无相同值则返回查找到的值
            if (uiComponents.TryGetValue(gameObjectName,out UIComponent value))
            {
                if (!value.IsSame||isRootNameNull)
                {
                    return value.UIGameObject;
                }
            }

            //不包含父物体直接在Panel里找，Panel下物体不能重名
            if (isRootNameNull)
                throw new NullReferenceException("未找到" + gameObjectName + "这个UI");
            if (!uiComponents.ContainsKey(rootGameObjectName))
            {
                return transform.Find(gameObjectName).gameObject;
            }
            //包含父物体的从父物体的所有子物体中找
            Transform[] childrenGroup =
                uiComponents[rootGameObjectName].UITransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in childrenGroup.Where(child => child.name == gameObjectName&&child!=childrenGroup[0]))
            {
                return child.gameObject;
            }

            throw new NullReferenceException("未找到" + gameObjectName + "这个UI");
        }

        /// <summary>
        /// 获取指定UI物体的所有包含T组件的T组件集合
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="gameObjectName">UI对象名字</param>
        /// <param name="rootGameObjectName">随便一个UI父物体的名字，没有可以为空或者null</param>
        /// <returns>所有子组件</returns>
        internal T[] FindComponentsInChildren<T>(string gameObjectName, string rootGameObjectName) where T:Component
        {
            return FindGameObjectInList(gameObjectName, rootGameObjectName).GetComponentsInChildren<T>(true);
        }

        /// <summary>
        /// 获取指定UI的所有子物体
        /// </summary>
        /// <param name="gameObjectName">UI对象名字</param>
        /// <param name="rootGameObjectName">随便一个UI父物体的名字，没有可以为空或者null</param>
        /// <returns>所有子物体</returns>
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
        /// 查找场景中的UI对象，Panel下不允许同名UI控件
        /// </summary>
        /// <param name="gameObjectName">UI对象名字数组</param>
        /// <param name="rootGameObjectName">随便一个对应UI父物体的名字，没有可以为空或者null</param>
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
        /// 为不挂载脚本的UI添加接口：如拖动、点击等
        /// </summary>
        /// <param name="go">游戏物体</param>
        /// <param name="triggerType">接口类型</param>
        /// <param name="action">回调委托</param>
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
