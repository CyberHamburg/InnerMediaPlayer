using UnityEngine;

namespace InnerMediaPlayer.Models.Signal
{
    public struct CookieSpreadSignal
    {
        public string CsrfToken;

        public CookieSpreadSignal(string csrfToken)
        {
            CsrfToken = csrfToken;
        }
    }
}
