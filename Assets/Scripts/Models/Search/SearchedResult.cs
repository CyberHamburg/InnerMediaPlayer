using System.Collections.Generic;

namespace InnerMediaPlayer.Models.Search
{

    public class ArItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
    }

    public class Al
    {
        /// <summary>
        /// 
        /// </summary>
        public string picUrl { get; set; }
    }

    public class FreeTrialPrivilege
    {
        /// <summary>
        /// 
        /// </summary>
        public bool resConsumable { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public bool userConsumable { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cannotListenReason { get; set; }
    }

    public class Privilege
    {
        public int pl { get; set; }

        public int dl {  get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FreeTrialPrivilege freeTrialPrivilege { get; set; }
    }

    public class SongsItem
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
        
        public int cp {  get; set; }

        public int t { get; set; }

        public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ArItem> ar { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Al al { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Privilege privilege { get; set; }

        internal CannotListenReason CanPlay()
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


    public class Result
    {
        /// <summary>
        /// 
        /// </summary>
        public List<SongsItem> songs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int songCount { get; set; }
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