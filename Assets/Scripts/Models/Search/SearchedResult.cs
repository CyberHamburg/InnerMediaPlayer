using System;
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
        /// 真则需要购买，假则可以听（猜测）
        /// </summary>
        public bool resConsumable { get; set; }
        /// <summary>
        /// 为空则可以播放，为1则是会员才可以播放（猜测）
        /// </summary>
        public string cannotListenReason { get; set; }
    }

    public class Privilege
    {
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