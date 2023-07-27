#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UniRx
{
    [InitializeOnLoad]
    public class ScenePlaybackDetector
    {
        private static bool _isPlaying = false;

        private static bool AboutToStartScene
        {
            get
            {
                return EditorPrefs.GetBool("AboutToStartScene");
            }
            set
            {
                EditorPrefs.SetBool("AboutToStartScene", value);
            }
        }

        public static bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                }
            }
        }

        // This callback is notified after scripts have been reloaded.
        [DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            // Filter DidReloadScripts callbacks to the moment where playmodeState transitions into isPlaying.
            if (AboutToStartScene)
            {
                IsPlaying = true;
            }
        }

        // InitializeOnLoad ensures that this constructor is called when the Unity Editor is started.
        static ScenePlaybackDetector()
        {
#pragma warning disable CS0618 // “EditorApplication.playmodeStateChanged”已过时:“Use EditorApplication.playModeStateChanged and/or EditorApplication.pauseStateChanged”
            EditorApplication.playmodeStateChanged += () =>
#pragma warning restore CS0618 // “EditorApplication.playmodeStateChanged”已过时:“Use EditorApplication.playModeStateChanged and/or EditorApplication.pauseStateChanged”
            {
                // Before scene start:          isPlayingOrWillChangePlaymode = false;  isPlaying = false
                // Pressed Playback button:     isPlayingOrWillChangePlaymode = true;   isPlaying = false
                // Playing:                     isPlayingOrWillChangePlaymode = false;  isPlaying = true
                // Pressed stop button:         isPlayingOrWillChangePlaymode = true;   isPlaying = true
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    AboutToStartScene = true;
                }
                else
                {
                    AboutToStartScene = false;
                }

                // Detect when playback is stopped.
                if (!EditorApplication.isPlaying)
                {
                    IsPlaying = false;
                }
            };
        }
    }
}

#endif