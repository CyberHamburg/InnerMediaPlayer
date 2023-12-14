using System;
using System.Collections;
using UnityEngine;

#pragma warning disable CS0168

namespace InnerMediaPlayer.Models.UI
{
    public struct UIComponent<T> where T : Component
    {
        public Transform UITransform;

        public T UIComponentT;
        public string UIName => UITransform.name;
        public GameObject UIGameObject => UITransform.gameObject;
        public bool IsSame { internal set; get; }

        public UIComponent(Transform uiTransform, T tComponent)
        {
            UITransform = uiTransform;
            IsSame = false;
            UIComponentT = tComponent;
        }

        public static void AddToDictionary<T1, T2>(UIComponent<T> uiComponent, T1 collection, T2 key)
            where T1 : IDictionary, ICollection
        {
            try
            {
                collection.Add(key, uiComponent);
            }
            catch (ArgumentException a)
            {
#if UNITY_EDITOR && UNITY_DEBUG
                Debug.Log(a.Message);
#endif
                var tempUIComponent = (UIComponent<T>)collection[key];
                tempUIComponent.IsSame = true;
                collection[key] = tempUIComponent;
            }
        }
    }
    public struct UIComponent
    {
        public Transform UITransform;
        public string UIName => UITransform.name;
        public GameObject UIGameObject => UITransform.gameObject;
        public bool IsSame { internal set; get; }

        public UIComponent(Transform uiTransform)
        {
            UITransform = uiTransform;
            IsSame = false;
        }
        public static void AddToDictionary<T>(UIComponent uiComponent, T collection) where T : IDictionary, ICollection
        {
            try
            {
                collection.Add(uiComponent.UIName, uiComponent);
            }
            catch (ArgumentException e)
            {
#if UNITY_EDITOR && UNITY_DEBUG
                Debug.Log(e.Message);
#endif
                var tempUIComponent = (UIComponent)collection[uiComponent.UIName];
                tempUIComponent.IsSame = true;
                collection[uiComponent.UIName] = tempUIComponent;
            }
        }
    }
}