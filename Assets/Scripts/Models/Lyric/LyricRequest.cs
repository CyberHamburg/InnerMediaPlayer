namespace InnerMediaPlayer.Models.Lyric
{
    public class LyricRequest
    {
        /// <summary>
        /// ����id
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// -1��ȡԭ��ʣ�Ĭ��Ϊ1����ȡ
        /// </summary>
        public int lv { get; set; }
        /// <summary>
        /// -1��ȡ��ʵķ��룬Ĭ��Ϊ1����ȡ
        /// </summary>
        public int tv { get; set; }
        /// <summary>
        /// -1��ȡ���к�����ĸ�ʣ�Ĭ��Ϊ1����ȡ
        /// </summary>
        public int kv { get; set; }
        /// <summary>
        /// �˺���֤�ַ���
        /// </summary>
        public string csrf_token { get; set; }

        public LyricRequest(int id,string csrfToken)
        {
            this.id = id;
            csrf_token = csrfToken;
            lv = -1;
            tv = -1;
            kv = 1;
        }
    }
}