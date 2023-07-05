﻿using UnityEngine;

namespace Evereal.VRVideoPlayer
{
    public class VideoEventCtrl : MonoBehaviour
    {
        public VideoPlayerCtrl videoPlayerCtrl;

        private const string TAG = "[VRVideoPlayer]";

        private void OnEnable()
        {
            videoPlayerCtrl.OnVideoReady += HandleVideoReadyEvent;
            videoPlayerCtrl.OnVideoEnd += HandleVideoEndEvent;
        }

        private void OnDisable()
        {
            videoPlayerCtrl.OnVideoReady -= HandleVideoReadyEvent;
            videoPlayerCtrl.OnVideoEnd -= HandleVideoEndEvent;
        }

        private void HandleVideoReadyEvent()
        {
            // Add you custom function here.
            Debug.Log(TAG + " Video is ready to play.");
        }

        private void HandleVideoEndEvent()
        {
            // Add you custom function here.
            Debug.Log(TAG + " Video playback is end.");
        }
    }
}