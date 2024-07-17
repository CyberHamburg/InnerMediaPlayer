using System.Collections.Generic;
using UnityEngine;

namespace InnerMediaPlayer.Models
{
    public enum CannotListenReason
    {
        None,
        NoCopyrightOrNotVIP,
        NotContainRequestSongEncoderType,
        NetworkError,
        Unknown
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
        public string listenType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cannotListenReason { get; set; }
    }

    public class FreeTimeTrialPrivilege
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
        public int type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int remainTime { get; set; }
    }

    public class DataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string md5 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int expi { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float gain { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float? peak { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string uf { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int payed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int flag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool canExtend { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string freeTrialInfo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string level { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string encodeType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FreeTrialPrivilege freeTrialPrivilege { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FreeTimeTrialPrivilege freeTimeTrialPrivilege { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int urlSource { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int rightSource { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string podcastCtrp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string effectTypes { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int time { get; set; }
        
        internal AudioType ConvertType()
        {
            switch (type.ToLowerInvariant())
            {
                case "mp3":
                    return AudioType.MPEG;
                case "mp2":
                    return AudioType.MPEG;
                case "wav":
                    return AudioType.WAV;
                case "m4a":
                    //暂时未做m4a的转码
                    return AudioType.UNKNOWN;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        internal CannotListenReason CanPlay()
        {
            if (url == null && code == 404)
                return CannotListenReason.NotContainRequestSongEncoderType;
            if (url == null)
                return CannotListenReason.NoCopyrightOrNotVIP;
            return CannotListenReason.None;
        }
    }

    public class SongResult
    {
        /// <summary>
        /// 
        /// </summary>
        public List<DataItem> data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
    }

}