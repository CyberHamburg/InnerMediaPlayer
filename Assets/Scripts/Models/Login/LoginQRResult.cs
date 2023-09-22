namespace InnerMediaPlayer.Models.Login
{
    public class LoginQRResult
    {
        public string avatarUrl { get; set; }
        /// <summary>
        /// <example>800二维码不存在或已过期</example>
        /// <example>801等待扫码</example>
        /// <example>802授权中</example>
        /// <example>803授权登录成功</example>
        /// </summary>
        public int code { get; set; }
        public string message { get; set; }
        public string nickname { get; set; }
    }
}