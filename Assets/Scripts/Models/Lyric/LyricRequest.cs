namespace InnerMediaPlayer.Models.Lyric
{
    public class LyricRequest
    {
        /// <summary>
        /// 歌曲id
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// -1获取原歌词，默认为1不获取
        /// </summary>
        public int lv { get; set; }
        /// <summary>
        /// -1获取歌词的翻译，默认为1不获取
        /// </summary>
        public int tv { get; set; }
        /// <summary>
        /// -1获取带有毫秒戳的歌词，默认为1不获取
        /// </summary>
        public int kv { get; set; }
        /// <summary>
        /// 账号验证字符串
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