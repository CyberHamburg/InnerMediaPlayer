using UnityEngine;

namespace InnerMediaPlayer.Management
{
    internal class GameSetting
    {
        internal GameSetting()
        {
#if LIMIT_FRAME
            Application.targetFrameRate = 60;
#endif
        }
    }
}
