using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InnerMediaPlayer.Model;
using InnerMediaPlayer.Model.Login;
using InnerMediaPlayer.Model.Search;
using InnerMediaPlayer.Model.Song;
using LitJson;
using QRCoder;
using QRCoder.Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PostData : MonoBehaviour
{
    public Button searchButton;
    public Button loginButton;
    public Image qrCodeImage;
    public Text loginState;
    public InputField inputField;
    public ScrollRect scrollRect;
    public GameObject item;
    public GameObject nullItem;
    public GameObject cannotPlay;
    public Transform content;
    public string lastKeyString;

    //搜索时的链接
    private string _searchUrl;
    //歌曲页面链接
    private string _songUrl;
    //请求获取uniKey
    private string _loginUrl;
    //询问二维码扫描状态
    private string _qrCodeUrl;
    //生成二维码的前缀url
    private string _qrCodeGenerateUrl;
    private string _csrfToken;
    private Dictionary<string, string> _formFields;
    private SearchRequestData _requestJsonData;
    private AudioSource _audioSource;
    private Transform _canvas;
    private Cookies _cookies;
    private bool _isSearching;
    private string _encSecKey;
    private byte[] _iv;
    private byte[] _firstKey;
    private byte[] _lastKey;

    private const string Params = "params";

    private void Awake()
    {
        _searchUrl = "https://music.163.com/weapi/cloudsearch/get/web";
        _songUrl = "http://music.163.com/api/song/enhance/player/url";
        _loginUrl = "https://music.163.com/weapi/login/qrcode/unikey";
        _qrCodeUrl = "https://music.163.com/weapi/login/qrcode/client/login";
        _qrCodeGenerateUrl = "http://music.163.com/login?codekey=";

        _iv = Encoding.UTF8.GetBytes("0102030405060708");
        _firstKey = Encoding.UTF8.GetBytes("0CoJUm6Qyw8W8jud");
        _lastKey = Encoding.UTF8.GetBytes(lastKeyString);
        _cookies = new Cookies();
        _formFields = new Dictionary<string, string>(4);
        _audioSource = GetComponent<AudioSource>();
        _canvas = FindObjectOfType<Canvas>().transform;
        _encSecKey =
            "d569d0d55e4864407c738f3cc0d1921cee0259f56ac84a2239d274a6342756ed0dd5614731718e51aa5f94dc109b8ca203e2824070cc4644b8cc25ed1cd66cc8b21bc74cdbf102c9aa4b17d1762d3decccce939199b46ed6119a64a92e51da92424fd1ea1fe51d5a9ac2fcf5144697be0c7c98a214bf30fa6f39224f7f15efd6";
    }

    private async void Start()
    {
        searchButton.onClick.AddListener(SearchSong);
        inputField.onEndEdit.AddListener(OnEndEdit);
        scrollRect.onValueChanged.AddListener(OnDrag);
        loginButton.onClick.AddListener(Login);

        await _cookies.LoadFromFile();
        _csrfToken = _cookies.Find("__csrf")?.value;
        _formFields.Add("csrf_token", _csrfToken);
        _formFields.Add("encSecKey", _encSecKey);
    }

    private async Task<(string json,Dictionary<string,string> headers)> PostWithHeaders(string url, string json)
    {
        string one = AesEncrypt(json, _iv, _firstKey);
        string two = AesEncrypt(one, _iv, _lastKey);
        WWWForm form = new WWWForm();
        form.AddField("params", two);
        form.AddField("encSecKey", _encSecKey);
        using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }
        return (webRequest.downloadHandler.text,webRequest.GetResponseHeaders());
    }

    private async Task<(string json, Dictionary<string, string> headers)> PostWithHeaders<T>(string url, T @object)
    {
        string json = JsonMapper.ToJson(@object);
        string one = AesEncrypt(json, _iv, _firstKey);
        string two = AesEncrypt(one, _iv, _lastKey);
        WWWForm form = new WWWForm();
        form.AddField("params", two);
        form.AddField("encSecKey", _encSecKey);
        using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }
        return (webRequest.downloadHandler.text, webRequest.GetResponseHeaders());
    }

    private async Task<string> Post(string url,string json)
    {
        string one = AesEncrypt(json, _iv, _firstKey);
        string two = AesEncrypt(one, _iv, _lastKey);
        WWWForm form = new WWWForm();
        form.AddField("params", two);
        form.AddField("encSecKey", _encSecKey);
        using UnityWebRequest webRequest=UnityWebRequest.Post(url,form);
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }
        return webRequest.downloadHandler.text;
    }

    private async Task<string> Post<T>(string url, T @object)
    {
        string json = JsonMapper.ToJson(@object);
        string one = AesEncrypt(json, _iv, _firstKey);
        string two = AesEncrypt(one, _iv, _lastKey);
        WWWForm form = new WWWForm();
        form.AddField("params", two);
        form.AddField("encSecKey", _encSecKey);
        using UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }
        return webRequest.downloadHandler.text;
    }

    /// <summary>
    /// 生成二维码并创建为Sprite
    /// </summary>
    /// <param name="content">带有unikey的登录url</param>
    /// <returns></returns>
    private Sprite GenerateQrCode(string content)
    {
        using QRCodeGenerator qrCodeGenerator = new QRCodeGenerator();
        using QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M, true);
        UnityQRCode unityQrCode = new UnityQRCode(qrCodeData);
        Texture2D texture = unityQrCode.GetGraphic(10);
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        return Sprite.Create(texture, rect, Vector2.one * 0.5f);
    }

    private async void Login()
    {
        //TODO:登陆前判断登录状态是否过期
        #region Post登录请求并制作二维码

        LoginUnikeyRequest loginRequest = new LoginUnikeyRequest();
        string loginJson = JsonMapper.ToJson(loginRequest);
        string loginResult = await Post(_loginUrl, loginJson);
        LoginUnikeyResult loginResultObject = JsonMapper.ToObject<LoginUnikeyResult>(loginResult);
        string qrCodeUrl = _qrCodeGenerateUrl + loginResultObject.unikey;
        Sprite sprite = GenerateQrCode(qrCodeUrl);
        qrCodeImage.gameObject.SetActive(true);
        qrCodeImage.sprite = sprite;
        loginState.gameObject.SetActive(true);

        #endregion

        LoginQRRequest loginQrRequest = new LoginQRRequest(loginResultObject.unikey);
        LoginQRResult loginQrResult;
        Dictionary<string, string> localHeaders;
        do
        {
            (string json,Dictionary<string,string> headers) = await PostWithHeaders(_qrCodeUrl, loginQrRequest);
            loginQrResult = JsonMapper.ToObject<LoginQRResult>(json);
            localHeaders = headers;
            loginState.text = loginQrResult.message;
            await new WaitForSeconds(2f);
        } while (loginQrResult.code == 801 || loginQrResult.code == 802);

        if (loginQrResult.code == 803)
        {
            _cookies.Clear();
            string headers = localHeaders["Set-Cookie"];
            const string csrf = "__csrf";
            int csrfUIndex = headers.LastIndexOf(csrf, StringComparison.Ordinal);
            string csrfUString = headers.Substring(csrfUIndex + csrf.Length + 1);
            int csrfLength = csrfUString.IndexOf(';');
            const string musicU = "MUSIC_U";
            int musicUIndex = headers.LastIndexOf(musicU, StringComparison.Ordinal);
            string musicUString = headers.Substring(musicUIndex + musicU.Length + 1);
            int musicLength = musicUString.IndexOf(';');
            _cookies.Add(csrf, csrfUString.Substring(0, csrfLength));
            _cookies.Add(musicU, musicUString.Substring(0, musicLength));
            _cookies.Add("NMTID", lastKeyString);
            _cookies.Add("__remember_me",true.ToString());
            _cookies.SaveToFile();
        }
        qrCodeImage.gameObject.SetActive(false);
        await new WaitForSeconds(2f);
        loginState.gameObject.SetActive(false);
    }

    /// <summary>
    /// 上下拉自动翻页
    /// </summary>
    /// <param name="vector2"></param>
    private void OnDrag(Vector2 vector2)
    {
        if(_isSearching)
            return;
        if (vector2.y < 0.01f && _requestJsonData != null)
        {
            int offset = int.Parse(_requestJsonData.offset);
            int limit = int.Parse(_requestJsonData.limit);
            int page = offset / limit;
            _requestJsonData.offset = (++page * limit).ToString();
            string encryptRequestData = Encrypt(_requestJsonData);
            if (_formFields.ContainsKey(Params))
                _formFields.Remove(Params);
            _formFields.Add(Params, encryptRequestData);
            SearchSong();
        }
        else if (vector2.y > 0.99f && _requestJsonData != null)
        {
            int offset = int.Parse(_requestJsonData.offset);
            int limit = int.Parse(_requestJsonData.limit);
            int page = offset / limit;
            if (page > 0)
            {
                _requestJsonData.offset = (--page * limit).ToString();
                string encryptRequestData = Encrypt(_requestJsonData);
                if (_formFields.ContainsKey(Params))
                    _formFields.Remove(Params);
                _formFields.Add(Params, encryptRequestData);
                SearchSong();
            }
        }
    }

    /// <summary>
    /// 当搜索框结束编辑时生成搜索需要的数据
    /// </summary>
    /// <param name="str">结束编辑时的字符串</param>
    private void OnEndEdit(string str)
    {
        if (_requestJsonData == null)
            _requestJsonData = new SearchRequestData(str,_csrfToken);
        else
            _requestJsonData.s = str;
        string unencryptedString = JsonMapper.ToJson(_requestJsonData);
        string unescapedString = System.Text.RegularExpressions.Regex.Unescape(unencryptedString);
        string encryptOne = AesEncrypt(unescapedString, _iv, _firstKey);
        string encryptTwo = AesEncrypt(encryptOne, _iv, _lastKey);
        if (_formFields.ContainsKey(Params))
            _formFields.Remove(Params);
        _formFields.Add(Params, encryptTwo);
    }

    /// <summary>
    /// 点击搜索后生成列表，并对其中元素进行按键绑定
    /// </summary>
    private async void SearchSong()
    {
        _isSearching = true;
        #region 搜索请求

        using UnityWebRequest webRequest = UnityWebRequest.Post(_searchUrl, _formFields);
        webRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        webRequest.SetRequestHeader("Accept", "*/*");
        if(_cookies.Count!=0)
            webRequest.SetRequestHeader("Cookie",_cookies.GetCookies);
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }

        string json = Encoding.UTF8.GetString(webRequest.downloadHandler.data);
        Debug.Log(json);
        SearchedResult result = JsonMapper.ToObject<SearchedResult>(json);

        #endregion

        //重新搜索后去掉对旧结果的引用
        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        if (result.code!=200)
        {
            throw new HttpRequestException($"返回的状态码{result.code}不正确,检查网络问题");
        }
        //如果搜索到是一些违禁词的话，提示为空并返回结果
        if (result.result==null)
        {
            Instantiate(nullItem, content);
            return;
        }
        //搜索结果为空的话就返回空
        if (result.result.songs == null)
            return;
        //对搜索到的结果实行对数据和ui的绑定
        for (int i = 0; i < result.result.songs.Count; i++)
        {
            GameObject go = Instantiate(item, content);
            SongsItem song = result.result.songs[i];
            #region 下载歌曲封面并作为图片显示

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(song.al.picUrl + "?param=100y100");
            await request.SendWebRequest();
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Image album = go.transform.Find("Image").GetComponent<Image>();
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            album.sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f, 100);

            #endregion
            Button play = go.transform.Find("Button").GetComponent<Button>();
            Text songName = play.transform.GetChild(0).GetComponent<Text>();
            #region 歌名末尾添加作家

            songName.text = song.name + " (";
            for (int j = 0; j < song.ar.Count; j++)
            {
                songName.text += song.ar[j].name + ",";
            }
            songName.text = songName.text.Remove(songName.text.Length - 1, 1) + ")";

            #endregion
            string songUrl = _songUrl + "?id=" + song.id + "&ids=[" + song.id + "]&br=3200000";
            play.onClick.AddListener(() => Play(songUrl));
        }
        _isSearching = false;
    }

    private async void Play(string url)
    {
        #region 由歌曲获取到歌曲详情，包括播放的url

        using UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        webRequest.SetRequestHeader("Accept", "*/*");
        await webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }

        string json = Encoding.UTF8.GetString(webRequest.downloadHandler.data);
        SongResult songResult = JsonMapper.ToObject<SongResult>(json);

        #endregion

        //如果没有会员或者没有版权则url为空，播放不了
        //TODO:加入其他在平台搜索后再尝试播放的选择
        if (string.IsNullOrEmpty(songResult.data[0].url))
        {
            GameObject go = Instantiate(cannotPlay, _canvas);
            await new WaitForSeconds(2f);
            Destroy(go);
            return;
        }

        #region 获取url中的歌曲并播放

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(songResult.data[0].url, AudioType.MPEG);
        request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        request.SetRequestHeader("Accept", "*/*");
        await request.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception();
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        _audioSource.clip = clip;
        _audioSource.Play();

        #endregion
    }

    private string Encrypt<T>(T data)
    {
        string json = JsonMapper.ToJson(data);
        string encryptOne = AesEncrypt(json, _iv, _firstKey);
        string encryptTwo = AesEncrypt(encryptOne, _iv, _lastKey);
        return encryptTwo;
    }

    private string AesEncrypt(string content,byte[] iv,byte[] key)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(content);
        using SymmetricAlgorithm aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using ICryptoTransform enCrypt = aes.CreateEncryptor();
        byte[] result = enCrypt.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        aes.Clear();
        return Convert.ToBase64String(result);
    }

    private string Md5Encrypt(string content)
    {
        byte[] sor = Encoding.UTF8.GetBytes(content);
        var md5 = MD5.Create();
        byte[] result = md5.ComputeHash(sor);
        StringBuilder strbul = new StringBuilder(40);
        for (int i = 0; i < result.Length; i++)
        {
            strbul.Append(result[i].ToString("x2"));
        }
        return strbul.ToString();
    }
}