namespace InnerMediaPlayer.Models
{
    public class SongRequest
    {
        private string _ids;
        public string ids
        {
            get => _ids;
            set
            {
                _ids = $"[{value}]";
            }
        }

        public string level { get; set; }
        /// <summary>
        /// <para>��������,��aac��mp3��</para>
        /// <para>aac�ɻ�ø��õ���Ƶ����������Ҫ��unity��֧�ֵ�m4a��ת��</para>
        /// <para>mp3�õ��ϵ͵�����������ת��</para>
        /// </summary>
        public string encodeType { get; set; }
        public string csrf_token { get; set; }

        public SongRequest(int id, string csrfToken)
        {
            level = "standard";
            encodeType = "mp3";
            ids = id.ToString();
            csrf_token = csrfToken;
        }

        public SongRequest()
        {
            level = "standard";
            encodeType = "mp3";
        }
    }
}