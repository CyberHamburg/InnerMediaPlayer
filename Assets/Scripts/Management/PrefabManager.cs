using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Management
{
    public class PrefabManager:IDisposable
    {
        private readonly List<GameObject> _gameObjects;

        public PrefabManager()
        {
            _gameObjects = new List<GameObject>();
        }

        public GameObject this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                    return null;
                GameObject gameObject = _gameObjects.Find(go=>go.name==name);
                if (gameObject != null)
                    return gameObject;
                gameObject = Resources.Load<GameObject>(name);
                _gameObjects.Add(gameObject);
                return gameObject;
            }
        }

        public async void Dispose()
        {
            _gameObjects.Clear();
            await Resources.UnloadUnusedAssets();
        }
    }
}
