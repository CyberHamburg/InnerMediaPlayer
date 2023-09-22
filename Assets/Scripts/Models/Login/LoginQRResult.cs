namespace InnerMediaPlayer.Models.Login
{
    public class LoginQRResult
    {
        public string avatarUrl { get; set; }
        /// <summary>
        /// <example>800��ά�벻���ڻ��ѹ���</example>
        /// <example>801�ȴ�ɨ��</example>
        /// <example>802��Ȩ��</example>
        /// <example>803��Ȩ��¼�ɹ�</example>
        /// </summary>
        public int code { get; set; }
        public string message { get; set; }
        public string nickname { get; set; }
    }
}