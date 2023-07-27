using System;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace LitJson.Extension
{
    public static class JsonExtension
    {
        private static bool _isRegistered;

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Registered()
        {
            if (_isRegistered)
                return;
            _isRegistered = true;

            #region ×¢²áVector

            JsonMapper.RegisterExporter<Vector3>((vector3, writer) =>
            {
                writer.WriteObjectStart();
                writer.WritePropertyName(nameof(vector3.x));
                writer.Write(vector3.x);
                writer.WritePropertyName(nameof(vector3.y));
                writer.Write(vector3.y);
                writer.WritePropertyName(nameof(vector3.z));
                writer.Write(vector3.z);
                writer.WriteObjectEnd();
            });

            JsonMapper.RegisterExporter<Vector2>((vector2, writer) =>
            {
                writer.WriteObjectStart();
                writer.WritePropertyName(nameof(vector2.x));
                writer.Write(vector2.x);
                writer.WritePropertyName(nameof(vector2.y));
                writer.Write(vector2.y);
                writer.WriteObjectEnd();
            });

            JsonMapper.RegisterExporter<Vector4>((vector4, writer) =>
            {
                writer.WriteObjectStart();
                writer.WritePropertyName(nameof(vector4.x));
                writer.Write(vector4.x);
                writer.WritePropertyName(nameof(vector4.y));
                writer.Write(vector4.y);
                writer.WritePropertyName(nameof(vector4.z));
                writer.Write(vector4.z);
                writer.WritePropertyName(nameof(vector4.w));
                writer.Write(vector4.w);
                writer.WriteObjectEnd();
            });

            #endregion

            #region ×¢²áQuaternion

            JsonMapper.RegisterExporter<Quaternion>((quaternion, writer) =>
            {
                writer.WriteObjectStart();
                writer.WritePropertyName(nameof(quaternion.x));
                writer.Write(quaternion.x);
                writer.WritePropertyName(nameof(quaternion.y));
                writer.Write(quaternion.y);
                writer.WritePropertyName(nameof(quaternion.z));
                writer.Write(quaternion.z);
                writer.WritePropertyName(nameof(quaternion.w));
                writer.Write(quaternion.w);
                writer.WriteObjectEnd();
            });

            #endregion

            JsonMapper.RegisterImporter<long,int>(a =>
            {
                return (int)a;
            });

            JsonMapper.RegisterImporter<int,string>(x =>
            {
                return x.ToString();
            });
        }
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class JsonIgnoreAttribute : Attribute
    {

    }

}