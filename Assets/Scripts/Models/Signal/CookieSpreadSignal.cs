using UnityEngine;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct CookieSpreadSignal
    {
        internal string CsrfToken { get; private set; }

        internal CookieSpreadSignal(string csrfToken)
        {
            CsrfToken = csrfToken;
        }
    }
}
