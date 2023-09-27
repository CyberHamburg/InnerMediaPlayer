using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string _fileLocation;
        [JsonIgnore]
        private StringBuilder _cookies;
        [JsonIgnore]
        private bool _loadDone;
        [JsonIgnore]
        internal Cookie _csrfToken;
        [JsonIgnore]
        private const string CsrfTokenName = "__csrf";
        [JsonIgnore]
        internal string GetCookies
        {
            get
            {
                _cookies = new StringBuilder();
                foreach (Cookie cookie in allCookies)
                {
                    _cookies.Append(cookie.name).Append('=').Append(cookie.value).Append(';');
                }

                _cookies.Remove(_cookies.Length - 1, 1);
                return _cookies.ToString();
            }
        }
        [JsonIgnore]
        internal int Count => allCookies.Count;

        public Cookies()
        {
            allCookies = new List<Cookie>();
            _fileLocation = Path.Combine(UnityEngine.Application.persistentDataPath, "Cookie.json");
        }

        internal void Add(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            allCookies.Add(new Cookie(name, value));
        }

        internal void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            allCookies.RemoveAll(cookie => cookie.name.Equals(name));
        }

        internal void Replace(string name,string newValue)
        {
            Cookie cookie = allCookies.Find(cookie => cookie.name.Equals(name));
            cookie.value = newValue;
        }

        internal async Task<Cookie> FindAsync(string name)
        {
            while (!_loadDone)
            {
                await Task.Yield();
            }
            Cookie cookie =allCookies.Find(cookie => cookie.name.Equals(name));
            return cookie;
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

            public Cookie()
            {

            }
        }
    }
}