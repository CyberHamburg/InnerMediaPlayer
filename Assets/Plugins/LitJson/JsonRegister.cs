using UnityEngine;

namespace LitJson.Extension
{
    public class JsonRegister : MonoBehaviour
    {
        private void Start()
        {
#if !UNITY_EDITOR
            JsonExtension.Registered();
#else
            Debug.Log("ÔÚ±à¼­Æ÷×¢²á");
#endif
        }
    }
}
