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
        /// �ϴ����ͨ���Ƿ񿪷�
        /// </summary>
        public bool sgc { get; set; }
        /// <summary>
        /// �ϴ���ʷ���ͨ���Ƿ񿪷�
        /// </summary>
        public bool sfy { get; set; }
        /// <summary>
        /// �Ƿ�ȱ�ٸ�ʷ���
        /// </summary>
        public bool qfy { get; set; }
        /// <summary>
        /// ԭ���
        /// </summary>
        public Lrc lrc { get; set; }
        /// <summary>
        /// ���к�����ĸ��
        /// </summary>
        public Klyric klyric { get; set; }
        /// <summary>
        /// ��ʷ���
        /// </summary>
        public Tlyric tlyric { get; set; }
        /// <summary>
        /// �Ƿ�Ϊ������
        /// </summary>
        public bool nolyric { get; set; }
        /// <summary>
        /// �Ƿ�û�����ϴ������
        /// </summary>
        public bool uncollected { get; set; }
        public int code { get; set; }
    }
}