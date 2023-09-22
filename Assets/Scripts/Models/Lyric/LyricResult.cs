namespace InnerMediaPlayer.Models.Lyric
{
    public class Lrc
    {
        public int version { get; set; }
        public string lyric { get; set; }
    }

    public class Klyric
    {
        public int version { get; set; }
        public string lyric { get; set; }
    }

    public class Tlyric
    {
        public int version { get; set; }

        public string lyric { get; set; }
    }

    public class LyricResult
    {
        /// <summary>
        /// 上传歌词通道是否开放
        /// </summary>
        public bool sgc { get; set; }
        /// <summary>
        /// 上传歌词翻译通道是否开放
        /// </summary>
        public bool sfy { get; set; }
        /// <summary>
        /// 是否缺少歌词翻译
        /// </summary>
        public bool qfy { get; set; }
        /// <summary>
        /// 原歌词
        /// </summary>
        public Lrc lrc { get; set; }
        /// <summary>
        /// 带有毫秒戳的歌词
        /// </summary>
        public Klyric klyric { get; set; }
        /// <summary>
        /// 歌词翻译
        /// </summary>
        public Tlyric tlyric { get; set; }
        /// <summary>
        /// 是否为纯音乐
        /// </summary>
        public bool nolyric { get; set; }
        /// <summary>
        /// 是否没有人上传过歌词
        /// </summary>
        public bool uncollected { get; set; }
        public int code { get; set; }
    }
}