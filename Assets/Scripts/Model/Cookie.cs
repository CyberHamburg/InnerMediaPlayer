using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LitJson;
using LitJson.Extension;
using UnityEngine;

namespace InnerMediaPlayer.Model
{
    public class Cookies
    {
        public List<Cookie> AllCookies;
        [JsonIgnore]
        private readonly string _fileLocation;
        [JsonIgnore]
        private StringBuilder _cookies;
        [JsonIgnore]
        public string GetCookies
        {
            get
            {
                _cookies = new StringBuilder();
                foreach (Cookie cookie in AllCookies)
                {
                    _cookies.Append(cookie.name).Append('=').Append(cookie.value).Append(';');
                }

                _cookies.Remove(_cookies.Length - 1, 1);
                return _cookies.ToString();
            }
        }
        [JsonIgnore]
        public int Count => AllCookies.Count;

        public Cookies()
        {
            AllCookies = new List<Cookie>();
            _fileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Cookie.json");
        }

        public void Add(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            AllCookies.Add(new Cookie(name, value));
        }

        public void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException($"{nameof(name)}不能为空");
            AllCookies.RemoveAll(cookie => cookie.name.Equals(name));
        }

        public void Replace(string name,string newValue)
        {
            Cookie cookie = AllCookies.Find(cookie => cookie.name.Equals(name));
            cookie.value = newValue;
        }

        public Cookie Find(string name)
        {
            return AllCookies.Find(cookie => cookie.name.Equals(name));
        }

        public void Clear()
        {
            AllCookies.Clear();
        }

        public async Task LoadFromFile()
        {
            using FileStream fileStream = File.Open(_fileLocation, FileMode.OpenOrCreate, FileAccess.Read);
            byte[] data = new byte[fileStream.Length];
            int readCount = await fileStream.ReadAsync(data, 0, data.Length);
            if (readCount == 0)
                return;
            string json = Encoding.UTF8.GetString(data);
            Cookies cookies = JsonMapper.ToObject<Cookies>(json);
            AllCookies = cookies.AllCookies;
        }

        public async void SaveToFile()
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

            public Cookie(string name, string value)
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