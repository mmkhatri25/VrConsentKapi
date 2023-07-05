using UnityEditor;
using System.Collections.Generic;

namespace Evereal.VRVideoPlayer.Editor
{
    public enum VideoPlayerType
    {
        AndroidMediaPlayer,
        UnityVideoPlayer,
        UnityMovieTexture,
        HTCMediaDecoder,
        EasyMovieTexture,
    }

    public class VideoPlayerSelector
    {
        private static readonly Dictionary<VideoPlayerType, string> PLAYER_DEFINE =
            new Dictionary<VideoPlayerType, string>() {
            {
                VideoPlayerType.AndroidMediaPlayer, "USE_ANDROID_MEDIA_PLAYER"
            },
            {
                VideoPlayerType.UnityVideoPlayer, "USE_UNITY_VIDEO_PLAYER"
            },
            {
                VideoPlayerType.UnityMovieTexture, "" // The default case will use Unity Movie Texture
            },
            {
                VideoPlayerType.HTCMediaDecoder, "USE_VIVE_MEDIA_DECODER"
            },
            {
                VideoPlayerType.EasyMovieTexture, "USE_EASY_MOVIE_TEXTURE"
            }
        };

        public static void SelectPlayer(VideoPlayerType player)
        {
            string playerDefine = PLAYER_DEFINE[player];
#if UNITY_STANDALONE
            UpdateDefineSymbols(playerDefine, BuildTargetGroup.Standalone);
#elif UNITY_ANDROID
            UpdateDefineSymbols(playerDefine, BuildTargetGroup.Android);
#elif UNITY_IOS
            UpdateDefineSymbols(playerDefine, BuildTargetGroup.iOS);
#endif
        }

        public static void ResetPlayer()
        {
#if UNITY_STANDALONE
            UpdateDefineSymbols("", BuildTargetGroup.Standalone);
#elif UNITY_ANDROID
            UpdateDefineSymbols("", BuildTargetGroup.Android);
#elif UNITY_IOS
            UpdateDefineSymbols("", BuildTargetGroup.iOS);
#endif
        }

        private static void UpdateDefineSymbols(string playerDefine, BuildTargetGroup target)
        {
            string currentDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            string updateDefine = currentDefine;
            if (currentDefine.Length > 0)
            {
                string[] defines = currentDefine.Split(';');
                bool updated = false;
                foreach (string define in defines)
                {
                    // Check if define is player define
                    foreach (string existedPlayer in PLAYER_DEFINE.Values)
                    {
                        if (define == existedPlayer)
                        {
                            updateDefine = currentDefine.Replace(existedPlayer, playerDefine);
                            updated = true;
                            break;
                        }
                    }
                    if (updated)
                    {
                        break;
                    }
                }
                if (!updated)
                {
                    if (playerDefine != "")
                    {
                        updateDefine = currentDefine + ";" + playerDefine;
                    }
                }
            }
            else
            {
                updateDefine = playerDefine;
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, updateDefine);
        }

        public static VideoPlayerType AutoSelectPlayer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            SelectPlayer(VideoPlayerType.AndroidMediaPlayer);
            return VideoPlayerType.AndroidMediaPlayer;
#elif UNITY_5_6_OR_NEWER
            SelectPlayer(VideoPlayerType.UnityVideoPlayer);
            return VideoPlayerType.UnityVideoPlayer;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            SelectPlayer(VideoPlayerType.HTCMediaDecoder);
            return VideoPlayerType.HTCMediaDecoder;
#else
            SelectPlayer(VideoPlayerType.UnityMovieTexture);
            return VideoPlayerType.UnityMovieTexture;
#endif
        }
    }
}