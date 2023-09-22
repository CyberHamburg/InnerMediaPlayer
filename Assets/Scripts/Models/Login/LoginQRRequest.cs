namespace InnerMediaPlayer.Models.Login
{
    public class LoginQRRequest
    {
        public int type { get; set; }
        public bool noCheckToken { get; set; }
        public string key { get; set; }

        public LoginQRRequest(string key)
        {
            type = 1;
            noCheckToken = true;
            this.key = key;
        }
    }
}