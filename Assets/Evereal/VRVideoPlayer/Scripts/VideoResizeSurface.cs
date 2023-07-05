using UnityEngine;
using System.Collections;

namespace Evereal.VRVideoPlayer
{
    public enum VideoSourceType
    {
        Flat,
        FlatLeftRight,
        FlatUpDown,
    }

    public class VideoResizeSurface : MonoBehaviour
    {
        public VideoSourceType videoSourceType = VideoSourceType.Flat;
        public VideoPlayerCtrl videoPlayerCtrl;

        private int videoWidth = 0;
        private int videoHeight = 0;

        private void Update()
        {
            // Video source size not changed or not initialized.
            if (videoPlayerCtrl == null ||
                videoPlayerCtrl.VideoWidth == videoWidth ||
                videoPlayerCtrl.VideoHeight == videoHeight ||
                videoPlayerCtrl.VideoWidth == 0 ||
                videoPlayerCtrl.VideoHeight == 0)
            {
                return;
            }

            videoWidth = videoPlayerCtrl.VideoWidth;
            videoHeight = videoPlayerCtrl.VideoHeight;

            // Keep the video source ratio when rendering.
            float scaleY = gameObject.transform.localScale.y;
            float scaleX = (videoWidth / (float)videoHeight) * scaleY;
            float scaleZ = gameObject.transform.localScale.z;

            if (videoSourceType == VideoSourceType.FlatLeftRight)
            {
                scaleX *= 0.5f;
            }
            gameObject.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
    }
}