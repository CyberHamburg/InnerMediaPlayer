using UnityEngine;
using Zenject;

namespace LitJson.Extension
{
    public class JsonRegister : IInitializable
    {
        public void Initialize()
        {
#if !UNITY_EDITOR
            JsonExtension.Registered();
#elif UNITY_DEBUG
            Debug.Log("ÔÚ±à¼­Æ÷×¢²á");
#endif
        }
    }
}
