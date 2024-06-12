using System.Collections.Generic;

namespace InnerMediaPlayer.Models
{
    /// <summary>
    /// ���ڱ��ز����б������ȡ����
    /// </summary>
    internal class PlaylistJsonData
    {
        public List<Cell> AllSongs { get; set; }

        internal class Cell
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Artist { get; set; }
            public string PictureUrl { get; set; }
        }
    }
}
