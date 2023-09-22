namespace InnerMediaPlayer.Models.Search
{
    public class SearchRequestData
    {
        public string hlpretag { get; set; }
        public string hlposttag { get; set; }
        /// <summary>
        /// �����б���������֣������Ǹ�����ר����
        /// </summary>
        public string s { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// �Ƿ���������
        /// </summary>
        public bool queryCorrect { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string offset { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string limit { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string csrf_token { get; set; }

        public SearchRequestData(string csrfToken)
        {
            s = "";
            hlpretag = "<span class=\\\"s-fc7\\\">";
            hlposttag = "</span>";
            type = "1";
            queryCorrect = false;
            offset = "0";
            total = "true";
            limit = "20";
            csrf_token = csrfToken;
        }
    }
}