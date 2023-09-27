using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InnerMediaPlayer.Logical;
using UnityEngine;
using UnityEngine.Networking;

namespace InnerMediaPlayer.Tools
{
    internal class Network
    {
        internal const string SearchUrl = "https://music.163.com/weapi/cloudsearch/get/web";
        internal const string SongUrl = "http://music.163.com/api/song/enhance/player/url";
        internal const string LoginUrl = "https://music.163.com/weapi/login/qrcode/unikey";
        internal const string LyricUrl = "https://music.163.com/weapi/song/lyric";
        internal const string QrCodeUrl = "https://music.163.com/weapi/login/qrcode/client/login";
        internal const string QrCodeGenerateUrl = "http://music.163.com/login";
        internal const string LoginRefreshUrl = "https://music.163.com/weapi/login/token/refresh";

        internal const string Params = "params";
        internal const string CsrfToken = "csrf_token";
        internal const string EncSeckey = "encSecKey";

        private readonly Crypto _crypto;
        private readonly Cookies _cookies;
        private readonly Dictionary<string, string> _formFields;

        internal Network(Crypto crypto, Cookies cookies)
        {
            _crypto = crypto;
            _cookies = cookies;
            _formFields = new Dictionary<string, string>(5) { { EncSeckey, _crypto._encSecKey } };
        }

        internal async Task<(string json, Dictionary<string, string> headers)> PostWithHeadersAsync(string url, string json)
        {
            string param = _crypto.Encrypt(json);
            WWWForm form = new WWWForm();
            form.AddField("params", param);
            form.AddField("encSecKey", _crypto._encSecKey);
            using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(webRequest.error);
            return (webRequest.downloadHandler.text, webRequest.GetResponseHeaders());
        }

        /// <summary>
        /// 返回数据及标头响应数据，一般需要的是Set-Cookie字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="object"></param>
        /// <returns></returns>
        internal async Task<(string json, Dictionary<string, string> headers)> PostWithHeadersAsync<T>(string url, T @object)
            where T : class
        {
            string param = _crypto.Encrypt(@object);
            WWWForm form = new WWWForm();
            form.AddField("params", param);
            form.AddField("encSecKey", _crypto._encSecKey);
            using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(webRequest.error);
            return (webRequest.downloadHandler.text, webRequest.GetResponseHeaders());
        }

        internal async Task<string> PostAsync(string url, string json, bool needCsrfToken = false, bool needCookies = false)
        {
            string param = _crypto.Encrypt(json);
            if (_formFields.ContainsKey(Params))
                _formFields.Remove(Params);
            _formFields.Add(Params, param);
            switch (needCsrfToken)
            {
                case true when !_formFields.ContainsKey(CsrfToken):
                    Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
                    _formFields.Add(CsrfToken, cookie.value);
                    break;
                case false when _formFields.ContainsKey(CsrfToken):
                    _formFields.Remove(CsrfToken);
                    break;
            }

            using UnityWebRequest webRequest = UnityWebRequest.Post(url, _formFields);
            SetRequestHeaders(webRequest, needCookies);
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(webRequest.error);

            return Encoding.UTF8.GetString(webRequest.downloadHandler.data);
        }

        internal async Task<string> PostAsync<T>(string url, T @object, bool needCsrfToken = false, bool needCookies = false)
            where T : class
        {
            string param = _crypto.Encrypt(@object);
            if (_formFields.ContainsKey(Params))
                _formFields.Remove(Params);
            _formFields.Add(Params, param);
            switch (needCsrfToken)
            {
                case true when !_formFields.ContainsKey(CsrfToken):
                    Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
                    _formFields.Add(CsrfToken, cookie.value);
                    break;
                case false when _formFields.ContainsKey(CsrfToken):
                    _formFields.Remove(CsrfToken);
                    break;
            }

            using UnityWebRequest webRequest = UnityWebRequest.Post(url, _formFields);
            SetRequestHeaders(webRequest, needCookies);
            await webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(webRequest.downloadHandler.data);
            //快速点击两次会出现Unity65错误，当前最佳解决方法为再post一遍
            using UnityWebRequest request = UnityWebRequest.Post(url, _formFields);
            SetRequestHeaders(request, needCookies);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                throw new UnityException(request.error);
            return Encoding.UTF8.GetString(webRequest.downloadHandler.data);
        }

        internal async Task<string> PostAsync(string url, bool needCookies = false)
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, _formFields);
            SetRequestHeaders(unityWebRequest, needCookies);
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(unityWebRequest.downloadHandler.data);
            using UnityWebRequest webRequest = UnityWebRequest.Post(url, _formFields);
            SetRequestHeaders(webRequest, needCookies);
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(webRequest.error);
            return Encoding.UTF8.GetString(webRequest.downloadHandler.data);
        }

        internal async Task<string> PostAsync(string url, WWWForm wwwForm, bool needCookies = false)
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);
            SetRequestHeaders(unityWebRequest, needCookies);
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(unityWebRequest.error);
            return Encoding.UTF8.GetString(unityWebRequest.downloadHandler.data);
        }

        internal async Task<string> GetAsync(string url, bool needCookies = false, params string[] keyValue)
        {
            Uri uri = CombineUri(url, keyValue);
            using UnityWebRequest unityWebRequest = UnityWebRequest.Get(uri);
            SetRequestHeaders(unityWebRequest, needCookies);
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(unityWebRequest.error);
            return Encoding.UTF8.GetString(unityWebRequest.downloadHandler.data);
        }

        internal async Task<AudioClip> GetAudioClipAsync(string url, string md5, AudioType audioType)
        {
            using UnityWebRequest unityWebRequest = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            SetRequestHeaders(unityWebRequest, false);
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
                throw new UnityException(unityWebRequest.error);
            if (!_crypto.Md5Verify(md5, unityWebRequest.downloadHandler.data))
                Debug.LogError($"md5校验未通过。\nRequest:{md5}");
            if (!(unityWebRequest.downloadHandler is DownloadHandlerAudioClip downloadHandlerAudioClip)) 
                return null;
            downloadHandlerAudioClip.streamAudio = true;
            return downloadHandlerAudioClip.audioClip;
        }

        internal async Task<Texture2D> GetTextureAsync(string url, params string[] keyValue)
        {
            Uri uri = CombineUri(url, keyValue);
            using UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(uri);
            SetRequestHeaders(unityWebRequest, false);
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
                return DownloadHandlerTexture.GetContent(unityWebRequest);
            if (unityWebRequest.error.Contains("404"))
                return null;
            throw new UnityException(unityWebRequest.error);
        }

        internal void UpdateFormData(string key, string newValue)
        {
            if (_formFields.ContainsKey(key))
                _formFields.Remove(key);
            _formFields.Add(key, newValue);
        }

        internal Uri CombineUri(string url, params string[] keyValue)
        {
            if (keyValue.Length % 2 != 0)
                throw new ArgumentException($"需要的键值对的长度不正确。Length:{keyValue.Length}");
            UriBuilder uriBuilder = new UriBuilder(url);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < keyValue.Length; i++)
            {
                stringBuilder.Append(Regex.Unescape(keyValue[i]));
                stringBuilder.Append('=');
                i++;
                stringBuilder.Append(Regex.Unescape(keyValue[i]));
                stringBuilder.Append('&');
            }

            stringBuilder.Length--;
            uriBuilder.Query = stringBuilder.ToString();

            return uriBuilder.Uri;
        }

        private void SetRequestHeaders(UnityWebRequest unityWebRequest, bool needCookies)
        {
            unityWebRequest.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
            unityWebRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            unityWebRequest.SetRequestHeader("Accept", "*/*");
            unityWebRequest.SetRequestHeader("Accept-Language",
                "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7,zh-TW;q=0.6,or;q=0.5");
            if (needCookies)
                unityWebRequest.SetRequestHeader("Cookie", _cookies.GetCookies);
        }
    }
}