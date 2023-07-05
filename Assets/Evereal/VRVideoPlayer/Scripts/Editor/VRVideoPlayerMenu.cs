using UnityEngine;
using UnityEditor;

namespace Evereal.VRVideoPlayer.Editor
{
    public class VRVideoPlayerMenu
    {
        private const string TAG = "[VRVideoPlayer]";

        [MenuItem("Evereal/VRVideoPlayer/Use Easy Movie Texture", false, 0)]
        private static void UseEasyMovieTexture()
        {
            VideoPlayerSelector.SelectPlayer(VideoPlayerType.EasyMovieTexture);
            Debug.Log(TAG + " Set player to: " + VideoPlayerType.EasyMovieTexture);
        }

        [MenuItem("Evereal/VRVideoPlayer/Reset Video Player", false, 0)]
        private static void ResetVideoPlayer()
        {
            VideoPlayerSelector.ResetPlayer();
            Debug.Log(TAG + " Reset to default video player");
        }
    }
}