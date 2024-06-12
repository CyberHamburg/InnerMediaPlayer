namespace InnerMediaPlayer.Models
{
    public class SongRequest
    {
        private string _ids;
        public string ids
        {
            get => _ids;
            set
            {
                _ids = $"[{value}]";
            }
        }
        public string level { get; set; }
        public string encodeType { get; set; }
        public string csrf_token { get; set; }

        public SongRequest(int id, string csrfToken)
        {
            level = "standard";
            encodeType = "aac";
            ids = id.ToString();
            csrf_token = csrfToken;
        }

        public SongRequest()
        {
            level = "standard";
            encodeType = "aac";
        }
    }
}