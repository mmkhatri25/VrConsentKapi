//#undef UNITY_EDITOR

#if !USE_EASY_MOVIE_TEXTURE                     // Use Easy Movie Texture.
#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_2017_1_OR_NEWER
#define USE_UNITY_VIDEO_PLAYER                  // Use Unity Video Player.
#else
#define USE_ANDROID_MEDIA_PLAYER                // Use Android Media Player.
#endif
#elif UNITY_5_6_OR_NEWER
#define USE_UNITY_VIDEO_PLAYER                  // Use Unity Video Player.
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define USE_VIVE_MEDIA_DECODER                  // Use Vive Media Decoder.
#endif  // UNITY_ANDROID && !UNITY_EDITOR
#endif  // !USE_EASY_MOVIE_TEXTURE

using UnityEngine;
using System.Collections;

namespace Evereal.VRVideoPlayer
{
    [RequireComponent(typeof(Renderer))]
    public class VideoCopyTexture : MonoBehaviour
    {
        public VideoPlayerCtrl videoPlayerCtrl;

#if USE_ANDROID_MEDIA_PLAYER
        private int skippedFrame = 0;
#endif

        private void Update()
        {
            // Video source not ready yet.
            if (videoPlayerCtrl == null ||
                videoPlayerCtrl.VideoTexture == null ||
                videoPlayerCtrl.CurrentState != VideoPlayerCtrl.StateType.Started)
            {
                return;
            }
#if USE_ANDROID_MEDIA_PLAYER
            // Hack to skip first blank frames on Android.
            if (!GetComponent<Renderer>().material.mainTexture.Equals(videoPlayerCtrl.VideoTexture) &&
                skippedFrame++ >= VideoPlayerCtrl.SKIP_FRAME_COUNT)
            {
                GetComponent<Renderer>().material.mainTexture = videoPlayerCtrl.VideoTexture;
            }
#else
            if (!GetComponent<Renderer>().material.mainTexture.Equals(videoPlayerCtrl.VideoTexture))
            {
                GetComponent<Renderer>().material.mainTexture = videoPlayerCtrl.VideoTexture;
            }
#endif
        }
    }
}