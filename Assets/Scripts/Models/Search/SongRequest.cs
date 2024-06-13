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
        /// <para>解码类型,有aac和mp3等</para>
        /// <para>aac可获得更好的音频质量，但需要对unity不支持的m4a做转码</para>
        /// <para>mp3得到较低的质量，不用转码</para>
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