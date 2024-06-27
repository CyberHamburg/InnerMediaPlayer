using LitJson.Extension;
using System.Collections.Generic;

namespace InnerMediaPlayer.Models.Search.FullName
{
    public class SearchedResult
    {
        public List<SongResult> results { get; set; }
    }

    public class SongResult : ISongBindable
    {
        public Privilege privilege { get; set; }

        public Album album { get; set; }

        public List<Artist> artists { get; set; }

        public string name { get; set; }

        [JsonIgnore]
        public List<Artist> ar
        {
            get => artists;
            set => artists = value;
        }

        [JsonIgnore]
        public Album al
        {
            get => album;
            set => album = value;
        }

        public int id { get; set; }

        public int status { get; set; }

        public CannotListenReason CanPlay()
        {
            if (privilege == null)
                return CannotListenReason.NoCopyright;
            if (status < 0)
                return CannotListenReason.NoCopyright;
            if (privilege.pl == 0 && privilege.dl == 0)
                return CannotListenReason.NoCopyright;
            return CannotListenReason.None;
        }
    }
}
