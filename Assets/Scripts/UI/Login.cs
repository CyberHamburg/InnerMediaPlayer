using System;
using System.Collections.Generic;
using InnerMediaPlayer.Base;
using InnerMediaPlayer.Logical;
using InnerMediaPlayer.Models.Login;
using InnerMediaPlayer.Tools;
using LitJson;
using QRCoder;
using QRCoder.Unity;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Network = InnerMediaPlayer.Tools.Network;

namespace InnerMediaPlayer.UI
{
    public class Login : UIViewerBase
    {
        private Button _qrLogin;
        private Image _qrCode;
        private Text _qrState;
        private Cookies _cookies;
        private Network _network;

        [Inject]
        private void Initialize(Cookies cookies,Network network)
        {
            _cookies = cookies;
            _network = network;
        }

        private async void Start()
        {
            _qrLogin = FindGameObjectInList("Login", null).GetComponent<Button>();
            _qrCode = FindGameObjectInList("QrCode", null).GetComponent<Image>();
            _qrState = FindGameObjectInList("State", "QrCode").GetComponent<Text>();

            await _cookies.LoadFromFileAsync();
            if (_cookies.Count == 0)
            {
                _qrLogin.onClick.AddListener(QrLogin);
            }
            else
            {
                #region 对登录状态续存（看url应该是）

                Cookies.Cookie cookie = await _cookies.GetCsrfTokenAsync();
                Dictionary<string, string> crsfToken = new Dictionary<string, string>(1) { { Network.CsrfToken, cookie.value } };
                string result = await _network.PostAsync(Network.LoginRefreshUrl, crsfToken, true, true);
                LoginRefreshResult refreshCode = JsonMapper.ToObject<LoginRefreshResult>(result);
                //貌似这个状态码是登录过期？
                if (refreshCode.code == 301)
                {
                    _qrLogin.onClick.AddListener(QrLogin);
                    return;
                }

                #endregion

                gameObject.SetActive(false);
            }
        }

        private async void QrLogin()
        {
            #region Post登录请求并制作二维码

            LoginUnikeyRequest loginRequest = new LoginUnikeyRequest();
            string loginResult = await _network.PostAsync(Network.LoginUrl, loginRequest);
            LoginUnikeyResult loginResultObject = JsonMapper.ToObject<LoginUnikeyResult>(loginResult);
            Uri qrCodeUrl = _network.CombineUri(Network.QrCodeGenerateUrl, "codekey", loginResultObject.unikey);
            Sprite sprite = GenerateQrCode(qrCodeUrl.ToString());
            _qrCode.gameObject.SetActive(true);
            _qrCode.sprite = sprite;
            _qrState.gameObject.SetActive(true);

            #endregion

            LoginQRRequest loginQrRequest = new LoginQRRequest(loginResultObject.unikey);
            LoginQRResult loginQrResult;
            Dictionary<string, string> localHeaders;
            do
            {
                (string json, Dictionary<string, string> headers) = await _network.PostWithHeadersAsync(Network.QrCodeUrl, loginQrRequest);
                loginQrResult = JsonMapper.ToObject<LoginQRResult>(json);
                localHeaders = headers;
                _qrState.text = loginQrResult.message;
                await new WaitForSeconds(2f);
            } while (loginQrResult.code == 801 || loginQrResult.code == 802);

            //登录成功则重设本地cookie
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
                _cookies.Add("NMTID", Crypto.LastKeyString);
                _cookies.Add("__remember_me", true.ToString());
                await _cookies.SaveToFileAsync();
            }
            _qrCode.gameObject.SetActive(false);
            await new WaitForSeconds(2f);
            _qrState.gameObject.SetActive(false);
            await new WaitForSeconds(1f);
            gameObject.SetActive(false);
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

        private void OnDisable()
        {
            _qrLogin.onClick.RemoveAllListeners();
        }

        private void OnDestroy()
        {
            _qrLogin.onClick.RemoveAllListeners();
        }
    }
}
