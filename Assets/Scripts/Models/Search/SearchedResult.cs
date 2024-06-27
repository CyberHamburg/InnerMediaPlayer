using System.Collections.Generic;

namespace InnerMediaPlayer.Models.Search
{

    public class Artist
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
    }

    public class Album
    {
        /// <summary>
        /// 
        /// </summary>
        public string picUrl { get; set; }
    }

    public class Privilege
    {
        public int pl { get; set; }

        public int dl {  get; set; }
    }

    public class SongsItem : ISongBindable
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }

        public int st { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Artist> ar { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Album al { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Privilege privilege { get; set; }

        public CannotListenReason CanPlay()
        {
            if (privilege == null)
                return CannotListenReason.NoCopyright;
            if (st < 0)
                return CannotListenReason.NoCopyright;
            if (privilege.pl == 0 && privilege.dl == 0)
                return CannotListenReason.NoCopyright;
            return CannotListenReason.None;
        }
    }

    public class ArtistItem : IRelationshipSortable
    {
        public int id { get; set; }

        public string name { get; set; }

        public string picUrl { get; set; }

        public List<Artist> ar { get; set; }
    }

    public interface IRelationshipSortable
    {
        public string name { get; set; }
        public List<Artist> ar { get; set; }
    }

    public interface ISongBindable : IRelationshipSortable
    {
        public int id { get; set; }
        public Album al { get; set; }
        public CannotListenReason CanPlay();
    }

    public class Result
    {
        /// <summary>
        /// 
        /// </summary>
        public List<SongsItem> songs { get; set; }

        public List<ArtistItem> artists { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int songCount { get; set; }

        public int artistCount { get; set; }
    }

    public class SearchedResult
    {
        /// <summary>
        /// 
        /// </summary>
        public bool needLogin { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Result result { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
    }

}