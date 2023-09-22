using System;
using System.Collections.Generic;
using System.Linq;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Models.UI;
using UnityEngine;
using Zenject;

namespace InnerMediaPlayer.Management.UI
{
    /// <summary>
    /// <para>������һViewer֮ǰִ�л��</para>
    /// </summary>
    public sealed class UIManager:IInitializable,IDisposable
    {
        internal Dictionary<Canvas,UIComponent<Canvas>> Canvases { get; private set; }
        internal Dictionary<GameObject, UIViewerBase> UIViewers { get; private set; }

        private readonly PrefabManager _prefabManager;
        private readonly DiContainer _container;

        public delegate void AddPanelDelegate(GameObject panelGameObject);

        public UIManager(DiContainer container,PrefabManager prefabManager)
        {
            _container = container;
            _prefabManager = prefabManager;
        }

        public void Initialize()
        {
            InitializeUIViewer();
            InitializeCanvas();
        }

        public void Dispose()
        {
            Canvases?.Clear();
            UIViewers?.Clear();
        }

        private void InitializeUIViewer()
        {
            UIViewers = new Dictionary<GameObject, UIViewerBase>();
            GameObject[] canvas = GameObject.FindGameObjectsWithTag("CanvasRoot");
            List<UIViewerBase> uiViewers = new List<UIViewerBase>();
            foreach (GameObject o in canvas)
            {
                UIViewerBase[] viewers = o.GetComponentsInChildren<UIViewerBase>(true);
                uiViewers.AddRange(viewers);
            }
                
            foreach (UIViewerBase viewer in uiViewers)
            {
                if (viewer == null) continue;
                viewer.Initialization();
                UIViewers.Add(viewer.gameObject, viewer);
            }
        }

        /// <summary>
        /// <para>��ʼ��canvas�ֵ�</para>
        /// </summary>
        private void InitializeCanvas()
        {
            Canvases = new Dictionary<Canvas, UIComponent<Canvas>>();
            foreach (var uiViewer in UIViewers)
            {
                Canvas additiveCanvas = uiViewer.Key.GetComponentInParent<Canvas>(true);
                if (Canvases.ContainsKey(additiveCanvas)) continue;
                UIComponent<Canvas> uiComponent = new UIComponent<Canvas>(additiveCanvas.transform, additiveCanvas);
                UIComponent<Canvas>.AddToDictionary(uiComponent, Canvases,additiveCanvas);
            }
        }

        /// <summary>
        /// ����Panel��û���ҵ���NullReferenceException�쳣
        /// </summary>
        /// <typeparam name="T">UIViewerBase���ͣ�ʵΪPanel���Ϲ��ؽű�����</typeparam>
        /// <param name="viewerName">Panel����</param>
        /// <param name="canvasName">UIViewer������Canvas����</param>
        /// <param name="canvasTag">��Canvas��tag������</param>
        /// <returns></returns>
        internal T FindUIViewer<T>(string viewerName, string canvasName, string canvasTag)
            where T : UIViewerBase
        {
            IEnumerable<KeyValuePair<GameObject,UIViewerBase>> viewerList = UIViewers.Where(viewer =>
                viewer.Key.name == viewerName && viewer.Key.GetComponentInParent<Canvas>().name == canvasName);

            foreach (KeyValuePair<GameObject,UIViewerBase> tempViewer in viewerList.Where(tempViewer =>
                tempViewer.Value.GetType() == typeof(T) && tempViewer.Key.GetComponentInParent<Canvas>().CompareTag(canvasTag)))
            {
                return tempViewer.Value as T;
            }

            throw new NullReferenceException("û���ҵ�" + viewerName + "���Panel");
        }

        /// <summary>
        /// �ӳ�������canvas���Ҳ�������
        /// </summary>
        /// <param name="panelScriptType">canvas��panel�Ϲ���UIViewer����</param>
        /// <param name="canvasName">canvas����</param>
        /// <param name="canvasTag">canvas�ı�ǩ</param>
        /// <returns></returns>
        internal GameObject FindCanvas(Type panelScriptType,string canvasName,string canvasTag)
        {
            foreach (var canvas in Canvases)
            {
                Component[] components = canvas.Key.GetComponentsInChildren(panelScriptType, true);
                if (!canvas.Key.name.Equals(canvasName) || !canvas.Key.CompareTag(canvasTag)) 
                    continue;
                if (components.Any(type => type.GetType()==panelScriptType))
                {
                    return canvas.Key.gameObject;
                }
            }

            throw new NullReferenceException("û���ҵ�" + canvasName);
        }

        internal GameObject AddPanel<T>(GameObject panelPrefab, Canvas canvas,string newName,
            AddPanelDelegate addPanel)
            where T : UIViewerBase
        {
            GameObject go = _container.InstantiatePrefab(panelPrefab, canvas.transform);
            go.GetComponent<RectTransform>().anchoredPosition3D=Vector3.zero;
            go.name = newName;
            addPanel += (prefab) => { UIViewers.Add(go,go.GetComponent<T>()); };
            addPanel(panelPrefab);
            return go;
        }
    }
}
