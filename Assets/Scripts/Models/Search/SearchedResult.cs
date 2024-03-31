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
		public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FreeTrialPrivilege freeTrialPrivilege { get; set; }
		
		internal CannotListenReason CanPlay()
        {
			if (freeTrialPrivilege.resConsumable)
			{
				if (freeTrialPrivilege.cannotListenReason == 0.ToString() && fee == 1)
					return CannotListenReason.NotVip;
			}
			else
			{
				if (freeTrialPrivilege.cannotListenReason == 1.ToString())
				{
					if (fee == 1)
						return CannotListenReason.NotVip;
					if (fee == 0 || fee == 8)
						return CannotListenReason.None;
				}
				if (freeTrialPrivilege.cannotListenReason == null && (fee == 8 || fee == 0))
					return CannotListenReason.None;
			}
			
			return CannotListenReason.Unknown;
        }
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