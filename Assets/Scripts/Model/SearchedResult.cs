using System.Collections.Generic;

namespace InnerMediaPlayer.Model.Search
{

    public class ArItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 小师尊
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> tns { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> @alias { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> alia { get; set; }
    }

    public class Al
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 大海 大海柯柯柯啊 大海张雨生
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string picUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> tns { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string pic_str { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pic { get; set; }
    }

    public class H
    {
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float vd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sr { get; set; }
    }

    public class M
    {
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float vd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sr { get; set; }
    }

    public class L
    {
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float vd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sr { get; set; }
    }

    public class Sq
    {
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float vd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sr { get; set; }
    }

    public class ArtistsItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 张雨生
        /// </summary>
        public string name { get; set; }
    }

    public class AlbumMeta
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 大海
        /// </summary>
        public string name { get; set; }
    }

    public class OriginSongSimpleData
    {
        /// <summary>
        /// 
        /// </summary>
        public int songId { get; set; }
        /// <summary>
        /// 大海
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ArtistsItem> artists { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AlbumMeta albumMeta { get; set; }
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

    public class ChargeInfoListItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int rate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string chargeUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string chargeMessage { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int chargeType { get; set; }
    }

    public class Privilege
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int payed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int st { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int dl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int cp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int subp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool cs { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int maxbr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool toast { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int flag { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool preSell { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int playMaxbr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int downloadMaxbr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string maxBrLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string playMaxBrLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string downloadMaxBrLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string plLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string dlLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string flLevel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rscl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public FreeTrialPrivilege freeTrialPrivilege { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int rightSource { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ChargeInfoListItem> chargeInfoList { get; set; }
    }

    public class Hr
    {
        /// <summary>
        /// 
        /// </summary>
        public int br { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float vd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int sr { get; set; }
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
        public int pst { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int t { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<ArItem> ar { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> alia { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float pop { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int st { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int v { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string crbt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cf { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Al al { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int dt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public H h { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public M m { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public L l { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Sq sq { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Hr hr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string a { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int no { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rtUrl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ftype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> rtUrls { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int djId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int copyright { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int s_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mark { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int originCoverType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public OriginSongSimpleData originSongSimpleData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string tagPicList { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool resourceState { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int version { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string songJumpInfo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string entertainmentTags { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int single { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string noCopyrightRcmd { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int rtype { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rurl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mst { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int cp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int mv { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int publishTime { get; set; }
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
        public string searchQcReminder { get; set; }
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