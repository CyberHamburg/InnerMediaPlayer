using UnityEngine;

namespace InnerMediaPlayer.Models.Login
{
    public class LoginUnikeyRequest
    {
        public int type { get; set; }
        public bool noCheckToken { get; set; }

        public LoginUnikeyRequest()
        {
            type = 1;
            noCheckToken = true;
        }
    }
}
