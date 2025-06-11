using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitJson;
using LitJson.Extension;

namespace InnerMediaPlayer.Logical
{
    internal class Cookies
    {
        public List<Cookie> allCookies;
        [JsonIgnore]
        private Dictionary<string, Cookie> _allCookiesByName;
        [JsonIgnore]
        private readonly string _fileLocation = Path.Combine(UnityEngine.Application.persistentDataPath, "Cookie.json");
        [JsonIgnore]
        private StringBuilder _cookies;
        [JsonIgnore]
        private bool _loadDone;
        [JsonIgnore]
        internal Cookie _csrfToken;
        [JsonIgnore]
        private const string CsrfTokenName = "__csrf";
        [JsonIgnore]
        internal int Count => allCookies.Count;

        public string this[params string[] keys]
        {
            get
            {
                _cookies ??= new StringBuilder();
                IEnumerable<Cookie> cookies = from cookie in allCookies join key in keys on cookie.name equals key select cookie;
                foreach (Cookie cookie in cookies)
                {
                    _cookies.Append(cookie.name).Append('=').Append(cookie.value).Append(';');
                }

                _cookies.Remove(_cookies.Length - 1, 1);
                return _cookies.ToString();
            }
        }

        internal void Add(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            if (_allCookiesByName.ContainsKey(name))
            {
                Replace(name, value);
                return;
            }
            
            allCookies.Add(new Cookie(name, value));
            _allCookiesByName.Add(name, allCookies[allCookies.Count - 1]);
        }

        internal void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            if (!_allCookiesByName.TryGetValue(name, out Cookie cookie)) 
                return;
            allCookies.Remove(cookie);
            _allCookiesByName.Remove(name);
        }

        internal void Replace(string name,string newValue)
        {
            if (!_allCookiesByName.TryGetValue(name, out Cookie cookie))
                throw new NullReferenceException($"不存在名为{name}的cookie!");
            cookie.value = newValue;
        }

        internal async Task<Cookie> FindAsync(string name)
        {
            while (!_loadDone)
            {
                await Task.Yield();
            }
            
            _allCookiesByName.TryGetValue(name, out Cookie foundCookie);
            return foundCookie ?? throw new NullReferenceException($"不存在名为{name}的cookie!");
        }

        internal async Task<Cookie> GetCsrfTokenAsync()
        {
            if (_csrfToken != null)
                return _csrfToken;
            _csrfToken = await FindAsync(CsrfTokenName);
            return _csrfToken;
        }

        internal void Clear()
        {
            allCookies.Clear();
            _allCookiesByName.Clear();
        }

        internal async Task LoadFromFileAsync()
        {
            using FileStream fileStream = File.Open(_fileLocation, FileMode.OpenOrCreate, FileAccess.Read);
            byte[] data = new byte[fileStream.Length];
            int readCount = await fileStream.ReadAsync(data, 0, data.Length);
            if (readCount == 0)
                return;
            string json = Encoding.UTF8.GetString(data);
            Cookies cookies = JsonMapper.ToObject<Cookies>(json);
            allCookies = cookies.allCookies;
            _allCookiesByName ??= new Dictionary<string, Cookie>(allCookies.Count + 1);
            foreach (Cookie cookie in allCookies)
                _allCookiesByName.Add(cookie.name, cookie);
            //TODO:添加sDeviceId等默认cookie
            _loadDone = true;
        }

        internal async Task SaveToFileAsync()
        {
            string json = JsonMapper.ToJson(this);
            byte[] data = Encoding.UTF8.GetBytes(json);
            using FileStream fileStream = File.OpenWrite(_fileLocation);
            if (fileStream.CanSeek)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.SetLength(0);
            }
            await fileStream.WriteAsync(data, 0, data.Length);
            _loadDone = true;
        }

        public class Cookie
        {
            public string name;
            public string value;

            internal Cookie(string name, string value)
            {
                this.name = name;
                this.value = value;
            }

            /// <summary>
            /// LitJson创建对象时使用不能删
            /// </summary>
            public Cookie()
            {

            }
        }
    }
}