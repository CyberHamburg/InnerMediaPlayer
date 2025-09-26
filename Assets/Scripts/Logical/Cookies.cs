using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnerMediaPlayer.Models.Signal;
using LitJson;
using LitJson.Extension;
using Zenject;

namespace InnerMediaPlayer.Logical
{
    internal class Cookies : IInitializable
    {
        public List<Cookie> allCookies;
        [JsonIgnore]
        private Dictionary<string, Cookie> _allCookiesByName;
        [JsonIgnore]
        private readonly string _fileLocation = Path.Combine(UnityEngine.Application.persistentDataPath, "Cookie.json");
        [JsonIgnore]
        private StringBuilder _cookies;
        [JsonIgnore]
        private Cookie _csrfToken;
        [JsonIgnore]
        internal SignalBus signalBus;
        [JsonIgnore]
        internal const string MusicU = "MUSIC_U";
        [JsonIgnore]
        internal const string RememberMe = "__remember_me";
        [JsonIgnore]
        internal const string CsrfTokenName = "__csrf";
        [JsonIgnore]
        internal const string NmTidName = "NMTID";
        [JsonIgnore]
        internal const string WmTidName = "WM_TID";
        [JsonIgnore]
        internal const string SnakerIdName = "__snaker__id";
        [JsonIgnore]
        internal const string GdxidpyhxdEName = "gdxidpyhxdE";
        [JsonIgnore]
        internal const string JsessionIdWyyyName = "JSESSIONID-WYYY";
        [JsonIgnore]
        internal const string SDeviceIdName = "sDeviceId";

        [Inject]
        internal Cookies(SignalBus signalBus)
        {
            this.signalBus = signalBus;
        }

        /// <summary>
        /// json序列化需要勿删
        /// </summary>
        public Cookies()
        {
            
        }

        void IInitializable.Initialize()
        {
            allCookies = new List<Cookie>();
            _allCookiesByName = new Dictionary<string, Cookie>();
            //暂时没研究以下cookie是什么用处
            Add(NmTidName, "00OKiza-FPBFxcRxkbyoNXefGYXsHQAAAGIWAdhJw");
            Add(WmTidName, "4bkIGNiAj1dBAEBBUAeQlXaAVX4n70dh");
            Add(SnakerIdName, "gnFrczq64Xk00ykb");
            //网易易盾加密信息
            // ReSharper disable StringLiteralTypo
            Add(GdxidpyhxdEName, "QdLumCONk7TIEb9MtMzZBMrxPfETjSZKx3DLjAJorGaYtJtm4b%5C2tpACpcBBUueRgAkA%2B50kJc%5CqyE7P6qS8RcNywZGiUamq8ShM%2Bqr9Bju5O6a30Zhzb0Ws9%5Chf6cIDlpKRj%5C%2FjhqPWQKKnQf41Z6VezE5GX5YXeuRlP3RZy5n%2FWNet%3A1686311771907");
            Add(JsessionIdWyyyName, "t%5Cp%2BZcM3cYSy9GhWJwMqBTvtN6UvaDPECMDrMEal%5CfCfHHV4oQ5ypHH953ZeEtcn8vCxdIz37XO%5CivHVw067aBP8JBScsupIZgwGlUI71Dl1f5i44K%5Cyip2DW%2B2xGOw47uezo4G%2FfNdj5%5CeqQx%2Bt%5CSE2f5q73B3jGjBYGa0CkWdyol8%2B%3A1686312668805");
            Add(SDeviceIdName, "YD-tBV0FdoTLCRBVwRARFPBfEHLsYnHy+On");
            Add(RememberMe, "True");
        }

        public string this[params string[] keys]
        {
            get
            {
                _cookies ??= new StringBuilder();
                _cookies.Clear();
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
        
        internal bool Contains(string name) => _allCookiesByName.ContainsKey(name);

        internal Cookie Find(string name)
        {
            _allCookiesByName.TryGetValue(name, out Cookie foundCookie);
            return foundCookie ?? throw new NullReferenceException($"不存在名为{name}的cookie!");
        }

        internal Cookie GetCsrfToken()
        {
            if (_csrfToken != null)
                return _csrfToken;
            _csrfToken = Find(CsrfTokenName);
            return _csrfToken;
        }
        
        internal void BroadcastCsrf(string csrfToken) => signalBus.Fire(new CookieSpreadSignal(csrfToken));

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
            if (readCount != 0)
            {
                string json = Encoding.UTF8.GetString(data);
                Cookies cookies = JsonMapper.ToObject<Cookies>(json);
                if (cookies.allCookies != null)
                    allCookies.AddRange(cookies.allCookies);
                foreach (Cookie cookie in allCookies)
                    if (!_allCookiesByName.ContainsKey(cookie.name))
                        _allCookiesByName.Add(cookie.name, cookie);
            }
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