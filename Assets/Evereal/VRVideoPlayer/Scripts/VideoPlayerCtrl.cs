//#define UNITY_ANDROID
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

using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Evereal.VRVideoPlayer
{
    /// <summary>
    /// Usage:
    /// 1) Place a textured surface with the needed setting in your scene.
    ///
    /// 2) Add the VideoPlayerCtrl.cs script to the game object and setup
    /// surface material.
    ///
    /// 3) Provide the file name of the media file to play.
    ///
    /// Note:
    /// 1) This script assumes the media file is placed in "Assets/StreamingAssets",
    /// e.g "ProjectName/Assets/StreamingAssets/MovieName.mp4".
    /// Remote url or local video file on disk will support soon.
    ///
    /// 2) On Android, used Android native MediaPlayer as video player.
    ///
    /// 3) On Desktop, if use Unity 5.6 or later, it uses Unity VideoPlayer as video player.
    /// If before Unity 5.6, it uses MediaDecoder on Windows and Unity MovieTexture on Mac OSX.
    /// If Unity MovieTexture functionality is used. The media file is loaded at runtime,
    /// and therefore expected to be converted to Ogg Theora beforehand.
    ///
    /// 4) This plugin can work with EasyMovieTexture plugin to support more video
    /// format and more platform. EasyMovieTexture not included since it's not a
    /// free plugin, you can purchase it from:
    /// https://www.assetstore.unity3d.com/en/#!/content/10032
    ///
    /// Credits:
    /// 1) This script get inspired and modified according to a video play sample
    /// script in Oculus SDK example project.
    ///
    /// 2) The Windows platform video player is backed by MediaDecoder from HTC Vive software,
    /// it's free! You can download it direct from:
    /// https://www.assetstore.unity3d.com/en/#!/content/63938
    /// </summary>
    public class VideoPlayerCtrl : MonoBehaviour
    {
        /// <summary>
        /// VideoPlayerCtrl state. Take reference from:
        /// https://developer.android.com/reference/android/media/MediaPlayer.html
        ///
        ///                      Idle
        ///                       |
        ///          Initialize() |
        ///                       | LoadVideo()
        ///                       |
        ///                       v
        ///                   Initialized
        ///                       |
        ///             Prepare() |
        ///                       |
        ///                       v
        ///                    Prepared
        ///                       |
        ///                Play() |
        ///                       |
        ///                       v          Pause()
        ///            ------> Started ------------------> Paused
        ///            |          |  ^                       |
        ///            |   Stop() |  |        Play()         |
        ///     Play() |          |  -------------------------
        ///            |          v
        ///            ------- Stopped
        ///
        ///                       .
        ///   WaitVideoComplete() |
        ///                       v
        ///                      End
        ///
        /// </summary>
        public enum StateType
        {
            Idle = 0,
            Initialized = 1,
            Prepared = 2,
            Started = 3,
            Paused = 4,
            Stopped = 5,
            End = 6,
            Error = 7
        }
        /// <summary>
        /// File path for video inside StreamingAssets.
        /// </summary>
        public string videoFile = string.Empty;
        // TODO, make auto public.
        private bool auto = true;
        /// <summary>
        /// Set true for video looping.
        /// </summary>
        public bool loop = true;
        /// <summary>
        /// Used to convert from YUV to RGB.
        /// </summary>
        public Material convertMaterial;
        /// <summary>
        /// Target render surface.
        /// </summary>
        public GameObject targetSurface;
        /// <summary>
        /// Stereo render surface.
        /// </summary>
        public GameObject stereoSurface;
        /// <summary>
        /// Stereo render surface 2.
        /// </summary>
        public GameObject stereoSurface2;
        /// <summary>
        /// Use OnVideoReady += Callback to register event occur after video ready.
        /// </summary>
        public delegate void VideoReadyEvent();
        public VideoReadyEvent OnVideoReady;
        /// <summary>
        /// Use OnVideoEnd += Callback to register event occur after video end.
        /// </summary>
        public delegate void VideoEndEvent();
        public VideoEndEvent OnVideoEnd;

        private const string TAG = "[VRVideoPlayer]";

#if USE_EASY_MOVIE_TEXTURE
        private MediaPlayerCtrl easyMovieTexture;
#elif USE_UNITY_VIDEO_PLAYER
        /// <summary>
        /// The unity video player.
        /// https://docs.unity3d.com/ScriptReference/Video.VideoPlayer.html
        /// </summary>
        private UnityEngine.Video.VideoPlayer videoPlayer;
        private AudioSource audioSource;
#elif USE_ANDROID_MEDIA_PLAYER
        public static int SKIP_FRAME_COUNT = 45;
        private Texture2D nativeTexture;
        private IntPtr nativeTextureID = IntPtr.Zero;
        private int textureWidth = 2560;
        private int textureHeight = 1440;
        private int videoTime = 0;
        private int skippedFrame = 0;
        private AndroidJavaObject mediaPlayer;

        private enum MediaSurfaceEventType
        {
            Initialize = 0,
            Shutdown = 1,
            Update = 2,
            Max_EventType
        };
#elif USE_VIVE_MEDIA_DECODER
        private HTC.UnityPlugin.Multimedia.MediaDecoder mediaDecoder;
#else
        private MovieTexture movieTexture;
        private AudioSource audioSource;
#endif
        private Renderer movieRenderer;

        private StateType currentState = StateType.Idle;
        private bool pausedBeforeAppPause = false;
        private bool checkingComplete = false;

        /// <summary>
        /// Get the state of the current video player.
        /// </summary>
        public StateType CurrentState { get { return currentState; } }
        /// <summary>
        /// Get the width of the video.
        /// </summary>
        public int VideoWidth
        {
            get
            {
#if USE_EASY_MOVIE_TEXTURE
                if (easyMovieTexture == null) { return 0; }
                return easyMovieTexture.GetVideoWidth();
#elif USE_UNITY_VIDEO_PLAYER
                if (videoPlayer == null || videoPlayer.texture == null) { return 0; }
                return videoPlayer.texture.width;
#elif USE_ANDROID_MEDIA_PLAYER
                if (mediaPlayer == null) { return 0; }
                return mediaPlayer.Call<int>("getVideoWidth");
#elif USE_VIVE_MEDIA_DECODER
                if (mediaDecoder == null) { return 0; }
                int width = 1, height = 1;
                mediaDecoder.getVideoResolution(ref width, ref height);
                return width;
#else
                if (movieTexture == null) { return 0; }
                return movieTexture.width;
#endif
            }
        }
        /// <summary>
        /// Get the height of the video.
        /// </summary>
        public int VideoHeight
        {
            get
            {
#if USE_EASY_MOVIE_TEXTURE
                if (easyMovieTexture == null) { return 0; }
                return easyMovieTexture.GetVideoHeight();
#elif USE_UNITY_VIDEO_PLAYER
                if (videoPlayer == null || videoPlayer.texture == null) { return 0; }
                return videoPlayer.texture.height;
#elif USE_ANDROID_MEDIA_PLAYER
                if (mediaPlayer == null) { return 0; }
                return mediaPlayer.Call<int>("getVideoHeight");
#elif USE_VIVE_MEDIA_DECODER
                if (mediaDecoder == null) { return 0; }
                int width = 1, height = 1;
                mediaDecoder.getVideoResolution(ref width, ref height);
                return height;
#else
                if (movieTexture == null) { return 0; }
                return movieTexture.height;
#endif
            }
        }
        /// <summary>
        /// Get the duration of the video.
        /// </summary>
        public int VideoDuration
        {
            get
            {
#if USE_EASY_MOVIE_TEXTURE
                if (easyMovieTexture == null) { return 0; }
                return easyMovieTexture.GetDuration();
#elif USE_UNITY_VIDEO_PLAYER
                if (videoPlayer == null || videoPlayer.frameRate < 1) { return 0; }
                return (int)(videoPlayer.frameCount / videoPlayer.frameRate);
#elif USE_ANDROID_MEDIA_PLAYER
                if (VideoManager1 == null) { return 0; }
                return mediaPlayer.Call<int>("getDuration") / 1000;
#elif USE_VIVE_MEDIA_DECODER
                if (mediaDecoder == null) { return 0; }
                return (int)mediaDecoder.videoTotalTime;
#else
                if (movieTexture == null) { return 0; }
                return (int)movieTexture.duration;
#endif
            }
        }

        /// <summary>
        /// Get the video texture.
        /// </summary>
        public Texture VideoTexture
        {
            get
            {
#if USE_EASY_MOVIE_TEXTURE
                if (easyMovieTexture == null) { return null; }
                return easyMovieTexture.GetVideoTexture();
#elif USE_UNITY_VIDEO_PLAYER
                return videoPlayer.texture;
#elif USE_ANDROID_MEDIA_PLAYER
                return nativeTexture;
#elif USE_VIVE_MEDIA_DECODER
                return targetSurface.GetComponent<Renderer>().material.mainTexture;
#else
                return movieTexture;
#endif
            }
        }

        #region Public Player API
        /// <summary>
        /// Play/Resume the video.
        /// </summary>
        public void Play()
        {
            if (CurrentState != StateType.Prepared &&
                CurrentState != StateType.Paused &&
                CurrentState != StateType.Stopped) { return; }
#if USE_EASY_MOVIE_TEXTURE
            if (easyMovieTexture == null) { return; }
            easyMovieTexture.Play();
#elif USE_UNITY_VIDEO_PLAYER
            if (videoPlayer == null) { return; }
            videoPlayer.Play();
#elif USE_ANDROID_MEDIA_PLAYER
            if (mediaPlayer == null) { return; }
            try
            {
                mediaPlayer.Call("start");
            }
            catch (Exception e)
            {
                UpdateState(StateType.Error);
                Debug.LogError(TAG + " Failed to start mediaPlayer: " + e.Message);
            }
            if (!checkingComplete)
            {
                StartCoroutine(WaitVideoComplete());
            }
#elif USE_VIVE_MEDIA_DECODER
            if (mediaDecoder == null) { return; }
            if (CurrentState == StateType.Prepared)
            {
                mediaDecoder.startDecoding();
            }
            if (CurrentState == StateType.Paused || CurrentState == StateType.Stopped)
            {
                mediaDecoder.setResume();
            }
#else
            if (movieTexture == null || audioSource == null) { return; }
            movieTexture.Play();
            audioSource.Play();
            if (!checkingComplete)
            {
                StartCoroutine(WaitVideoComplete());
            }
#endif
            Debug.Log(TAG + " Start to play: " + videoFile);
            UpdateState(StateType.Started);
        }

        /// <summary>
        /// Pause the video.
        /// </summary>
        public void Pause()
        {
            if (CurrentState != StateType.Started) { return; }
#if USE_EASY_MOVIE_TEXTURE
            if (easyMovieTexture == null) { return; }
            easyMovieTexture.Pause();
#elif USE_UNITY_VIDEO_PLAYER
            if (videoPlayer == null) { return; }
            videoPlayer.Pause();
#elif USE_ANDROID_MEDIA_PLAYER
            if (mediaPlayer == null) { return; }
            try
            {
                mediaPlayer.Call("pause");
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Failed to pause mediaPlayer: " + e.Message);
            }
#elif USE_VIVE_MEDIA_DECODER
            if (mediaDecoder == null) { return; }
            mediaDecoder.setPause();
#else
            if (movieTexture == null || audioSource == null) { return; }
            movieTexture.Pause();
            audioSource.Pause();
#endif
            UpdateState(StateType.Paused);
        }

        /// <summary>
        /// Stop the video.
        /// </summary>
        public void Stop()
        {
            if (CurrentState != StateType.Started) { return; }
#if USE_EASY_MOVIE_TEXTURE
            if (easyMovieTexture == null) { return; }
            easyMovieTexture.Stop();
#elif USE_UNITY_VIDEO_PLAYER
            if (videoPlayer == null) { return; }
            videoPlayer.Stop();
#elif USE_ANDROID_MEDIA_PLAYER
            if (mediaPlayer == null) { return; }
            Pause();
            try
            {
                mediaPlayer.Call("seekTo", 0);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Failed to stop mediaPlayer: " + e.Message);
                UpdateState(StateType.Error);
            }
#elif USE_VIVE_MEDIA_DECODER
            if (mediaDecoder == null) { return; }
            Pause();
            mediaDecoder.setSeekTime(0);
#else
            if (movieTexture == null || audioSource == null) { return; }
            movieTexture.Stop();
            audioSource.Stop();
#endif
            UpdateState(StateType.Stopped);
        }

        /// <summary>
        /// Seek to specific time of video.
        /// </summary>
        /// <param name="seekTime">Seek time.</param>
        public void SeekTo(float seekTime)
        {
            if (CurrentState != StateType.Started) { return; }
#if USE_EASY_MOVIE_TEXTURE
            if (easyMovieTexture == null) { return; }
            easyMovieTexture.SeekTo((int)seekTime);
#elif USE_UNITY_VIDEO_PLAYER
            if (videoPlayer == null) { return; }
            long seekFrames = (long)(seekTime * videoPlayer.frameRate);
            videoPlayer.frame = seekFrames;
#elif USE_ANDROID_MEDIA_PLAYER
            if (mediaPlayer == null) { return; }
            try
            {
                mediaPlayer.Call("seekTo", (int)(seekTime * 1000));
                //mediaPlayer.Call("seekTo", new object[] { (long)(seekTime * 1000), 0 });
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Failed to seek mediaPlayer: " + e.Message);
                UpdateState(StateType.Error);
            }
#elif USE_VIVE_MEDIA_DECODER
            if (mediaDecoder == null) { return; }
            mediaDecoder.setSeekTime(seekTime);
#else
            Debug.Log(TAG + " SeekTo function not supported for Unity Movie Texture");
#endif
        }
        #endregion // Public Player API

        #region Internal Player API
        /// <summary>
        /// Initialize and load video.
        /// </summary>
        private IEnumerator Initialize()
        {
            movieRenderer = targetSurface.GetComponent<Renderer>();
            if (movieRenderer == null ||
                movieRenderer.material == null ||
                movieRenderer.material.mainTexture == null)
            {
                UpdateState(StateType.Error);
                throw new Exception(TAG + " No surface for media found.");
            }
#if USE_EASY_MOVIE_TEXTURE
            // Prevent Awake called before properties loaded.
            gameObject.SetActive(false);
            // Load EasyMovieTexture properties at runtime.
            easyMovieTexture = gameObject.AddComponent<MediaPlayerCtrl>() as MediaPlayerCtrl;
            easyMovieTexture.m_bAutoPlay = false;
            easyMovieTexture.m_bLoop = loop;
            //easyMovieTexture.m_strFileName = videoFile;
            easyMovieTexture.m_TargetMaterial = new GameObject[1];
            easyMovieTexture.m_TargetMaterial[0] = targetSurface;
            easyMovieTexture.m_bSupportRockchip = true;
            easyMovieTexture.m_shaderYUV = Shader.Find("Unlit/Unlit_YUV");
            gameObject.SetActive(true);

            easyMovieTexture.OnReady += OnPrepareComplete;
            easyMovieTexture.OnEnd += OnVideoPlayComplete;

            easyMovieTexture.Load(videoFile);
            // Following steps will be handled by EasyMovieTexture.
            yield break;
#elif USE_UNITY_VIDEO_PLAYER
            videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = targetSurface.GetComponent<MeshRenderer>();

            audioSource = gameObject.AddComponent<AudioSource>();
            videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            videoPlayer.skipOnDrop = true;
            videoPlayer.isLooping = loop;

            videoPlayer.prepareCompleted += (e) => { OnPrepareComplete(); };
            videoPlayer.loopPointReached += (e) => { OnVideoPlayComplete(); };

            yield return null;//ApiDataSave.JsonVideoData;
            string mediaUrl =  "file://" + Application.streamingAssetsPath + "/" + videoFile;
#if UNITY_ANDROID && !UNITY_EDITOR
            //mediaUrl = "jar:" + mediaUrl;
            string assetsPath = Application.streamingAssetsPath + "/" + videoFile;
            string dataPath =  Application.persistentDataPath + "/" + videoFile;
            if (!File.Exists(dataPath))
            {
                WWW wwwReader = new WWW(assetsPath);
                yield return wwwReader;

                if (wwwReader.error != null)
                {
                    Debug.LogError(TAG + " wwwReader error: " + wwwReader.error);
                }

                System.IO.File.WriteAllBytes(dataPath, wwwReader.bytes);
            }
            mediaUrl = dataPath;
#endif
            videoPlayer.url = mediaUrl;
#elif USE_ANDROID_MEDIA_PLAYER
            Media_Surface_Init();
            // TODO: Fix ArgumentException: nativeTex can not be null in Unity 2017.
            nativeTexture = Texture2D.CreateExternalTexture(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                true,
                false,
                IntPtr.Zero
            );
            IssuePluginEvent(MediaSurfaceEventType.Initialize);

            string assetsPath =  Application.streamingAssetsPath + "/" + videoFile;
            string dataPath = Application.persistentDataPath + "/" + videoFile;
            if (!File.Exists(dataPath))
            {
                WWW wwwReader = new WWW(assetsPath);
                yield return wwwReader;

                if (wwwReader.error != null)
                {
                    Debug.LogError(TAG + " wwwReader error: " + wwwReader.error);
                }

                System.IO.File.WriteAllBytes(dataPath, wwwReader.bytes);
            }

            // Delay 1 frame to allow MediaSurfaceInit from the render thread.
            yield return null;
            mediaPlayer = GetMediaPlayerOnTextureID(textureWidth, textureHeight);

            try
            {
                mediaPlayer.Call("setDataSource", dataPath);
                mediaPlayer.Call("setLooping", loop);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Failed to load video for mediaPlayer: " + e.Message);
            }
#elif USE_VIVE_MEDIA_DECODER
            targetSurface.GetComponent<Renderer>().material = convertMaterial;

            mediaDecoder = targetSurface.AddComponent<HTC.UnityPlugin.Multimedia.MediaDecoder>()
                as HTC.UnityPlugin.Multimedia.MediaDecoder;

            if (stereoSurface != null && stereoSurface2 != null)
            {
                HTC.UnityPlugin.Multimedia.StereoHandler.StereoType stereoType
                    = HTC.UnityPlugin.Multimedia.StereoHandler.StereoType.SIDE_BY_SIDE;
                if (stereoSurface.name == "UpSurface")
                {
                    stereoType = HTC.UnityPlugin.Multimedia.StereoHandler.StereoType.TOP_DOWN;
                }
                HTC.UnityPlugin.Multimedia.StereoHandler.SetStereoPair(
                    stereoSurface,
                    stereoSurface2,
                    movieRenderer.material,
                    stereoType,
                    true);
            }
            mediaDecoder.onInitComplete = new UnityEvent();
            mediaDecoder.onInitComplete.AddListener(OnPrepareComplete);
            mediaDecoder.onVideoEnd = new UnityEvent();
            mediaDecoder.onVideoEnd.AddListener(OnVideoPlayComplete);

            mediaDecoder.mediaPath = Application.streamingAssetsPath + "/" + videoFile;
            yield return null;
#else
            audioSource = gameObject.AddComponent<AudioSource>();

            if (!Path.GetExtension(videoFile).Equals(".ogv") &&
                !Path.GetExtension(videoFile).Equals(".ogg"))
            {
                if (File.Exists(
                    Application.streamingAssetsPath + "/" +
                    Path.GetDirectoryName(videoFile) + "/" +
                    Path.GetFileNameWithoutExtension(videoFile) + ".ogv"
                ))
                {
                    videoFile = Path.GetDirectoryName(videoFile) + "/" +
                        Path.GetFileNameWithoutExtension(videoFile) + ".ogv";
                }
                else if (File.Exists(
                    Application.streamingAssetsPath + "/" +
                    Path.GetDirectoryName(videoFile) + "/" +
                    Path.GetFileNameWithoutExtension(videoFile) + ".ogg"
                ))
                {
                    videoFile = Path.GetDirectoryName(videoFile) + "/" +
                        Path.GetFileNameWithoutExtension(videoFile) + ".ogg";
                }
                else
                {
                    UpdateState(StateType.Error);
                    throw new Exception("Only support ogv/ogg video format on Standalone platform.");
                }
            }
            WWW wwwReader = new WWW("file:///" + Application.streamingAssetsPath + "/" + videoFile);
            yield return wwwReader;
            if (wwwReader.error != null)
            {
                UpdateState(StateType.Error);
                Debug.LogError(TAG + " wwwReader error: " + wwwReader.error);
            }
            // Here is an issue in Unity Editor, please ignore the error if you
            // see the error below in console:
            // LoadMoveData got NULL!
            // Error: Cannot create FMOD::Sound instance for resource (null), (An invalid parameter was passed to this function. )
            //
            // Issue:
            // https://issuetracker.unity3d.com/issues/calling-www-dot-movie-fails-to-load-and-prints-error-loadmovedata-got-null
            // https://issuetracker.unity3d.com/issues/movietexture-fmod-error-when-trying-to-play-video-using-www-class
            movieTexture = wwwReader.movie;
            movieRenderer.material.mainTexture = movieTexture;
            audioSource.clip = movieTexture.audioClip;
            movieTexture.loop = loop;
#endif
            UpdateState(StateType.Initialized);
            StartCoroutine(Prepare());

        }

        /// <summary>
        /// Prepare for video play.
        /// </summary>
        private IEnumerator Prepare()
        {
            Stop();
#if USE_EASY_MOVIE_TEXTURE
            // Do nothing, will handled by EasyMovieTexture.
            yield break;
#elif USE_UNITY_VIDEO_PLAYER
            videoPlayer.Prepare();
            // Async prepare.
            yield break;
#elif USE_ANDROID_MEDIA_PLAYER
            try
            {
                // TODO, use prepareAsync instead.
                mediaPlayer.Call("prepare");
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Failed to prepare for mediaPlayer: " + e.Message);
            }
            yield return null;
#elif USE_VIVE_MEDIA_DECODER
            mediaDecoder.initDecoder(mediaDecoder.mediaPath);
            // Async prepare.
            yield break;
#else
            // Wait until movie texture get ready.
            while (movieTexture != null && !movieTexture.isReadyToPlay)
            {
                yield return null;
            }
#endif
            OnPrepareComplete();
        }

        /// <summary>
        /// Update the player state.
        /// </summary>
        /// <param name="state">State.</param>
        private void UpdateState(StateType state)
        {
            StateType previousState = CurrentState;
            currentState = state;
            Debug.Log(TAG + " State: " + previousState + " --> " + CurrentState);
        }
        #endregion // Internal Player API

        #region Internal Player Event
        /// <summary>
        /// Triggered when video prepare complete.
        /// </summary>
        private void OnPrepareComplete()
        {
            UpdateState(StateType.Prepared);

            if (OnVideoReady != null)
            {
                OnVideoReady();
            }

            if (auto)
            {
                Play();
            }
        }

        /// <summary>
        /// Triggered when video play complete.
        /// </summary>
        private void OnVideoPlayComplete()
        {
            if (OnVideoEnd != null)
            {
                OnVideoEnd();
            }

            if (!loop)
            {
                UpdateState(StateType.End);
            }

            if (loop)
            {
                Stop();
                Play();
            }
        }

        /// <summary>
        /// Wait for the video complete, manually trigger OnVideoPlayComplete event.
        /// </summary>
        private IEnumerator WaitVideoComplete()
        {
#if USE_EASY_MOVIE_TEXTURE || USE_UNITY_VIDEO_PLAYER || USE_VIVE_MEDIA_DECODER
            yield break;
#else
            checkingComplete = true;
            while (true)
            {
                yield return new WaitForSeconds(1);

                if (currentState != StateType.Started)
                {
                    continue;
                }
#if USE_ANDROID_MEDIA_PLAYER
                if (++videoTime >= VideoDuration)
                {
                    OnVideoPlayComplete();
                    videoTime = 0;
                    break;
                }
#else
                if (!movieTexture.isPlaying)
                {
                    OnVideoPlayComplete();
                    break;
                }
#endif
            }
#endif
            checkingComplete = false;

        }
        #endregion // Internal Player Event

        #region Unity Life Cycle
        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            if (targetSurface == null)
            {
                UpdateState(StateType.Error);
                throw new Exception(TAG + " No target surface provided.");
            }

            if (videoFile == string.Empty || videoFile == null)
            {
                UpdateState(StateType.Error);
                throw new Exception(TAG + " No video file provided.");
            }

#if USE_VIVE_MEDIA_DECODER
            if (convertMaterial == null)
            {
                UpdateState(StateType.Error);
                throw new Exception(TAG + " Vive convert material not set.");
            }
#endif

            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
#if USE_ANDROID_MEDIA_PLAYER
            if (CurrentState == StateType.Started)
            {
                IntPtr currTextureID = Media_Surface_GetNativeTexture();
                if (currTextureID != nativeTextureID)
                {
                    nativeTextureID = currTextureID;
                    nativeTexture.UpdateExternalTexture(currTextureID);
                }
                IssuePluginEvent(MediaSurfaceEventType.Update);
                // Hack to skip first blank frames on Android.
                if (movieRenderer.material.mainTexture != nativeTexture &&
                    (CurrentState == StateType.Prepared || CurrentState == StateType.Started) &&
                    skippedFrame++ >= SKIP_FRAME_COUNT)
                {
                    movieRenderer.material.mainTexture = nativeTexture;
                }
            }
#endif
        }

        /// <summary>
        /// Pauses video playback when the app loses or gains focus.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {

#if USE_EASY_MOVIE_TEXTURE || USE_UNITY_VIDEO_PLAYER
            return;
#endif
            if (paused)
            {
                pausedBeforeAppPause = (CurrentState == StateType.Paused);
            }
            // Pause/Resume the video only if it had been playing prior to app pause.
            if (!pausedBeforeAppPause)
            {
                if (paused)
                {
                    Pause();
                }
                else if (CurrentState == StateType.Paused)
                {
                    Play();
                }
            }
        }

        private void OnDestroy()
        {
#if USE_EASY_MOVIE_TEXTURE || USE_UNITY_VIDEO_PLAYER
            return;
#elif USE_ANDROID_MEDIA_PLAYER
            // This will trigger the shutdown on the render thread
            IssuePluginEvent(MediaSurfaceEventType.Shutdown);
            if (mediaPlayer == null) return;
            mediaPlayer.Call("stop");
            mediaPlayer.Call("release");
            mediaPlayer = null;
#elif USE_VIVE_MEDIA_DECODER
            if (mediaDecoder == null)
            {
                return;
            }
            mediaDecoder.stopDecoding();
            mediaDecoder = null;
#else
            if (movieTexture == null)
            {
                return;
            }
            movieTexture.Stop();
            movieTexture = null;
            if (audioSource == null)
            {
                return;
            }
            audioSource.Stop();
            audioSource = null;
#endif
        }
        #endregion // Unity Life Cycle

#if USE_ANDROID_MEDIA_PLAYER
        /// <summary>
        /// The start of the numeric range used by event IDs.
        /// </summary>
        /// <description>
        /// If multiple native rundering plugins are in use, the Media Surface
        /// plugin's event IDs can be re-mapped to avoid conflicts.
        ///
        /// Set this value so that it is higher than the highest event ID
        /// number used by your plugin.
        /// Media Surface plugin event IDs start at eventBase and end at
        /// eventBase plus the highest value in MediaSurfaceEventType.
        /// </description>
        private static int EventBase
        {
            get
            {
                return eventBase;
            }
            set
            {
                eventBase = value;
                Media_Surface_SetEventBase(eventBase);
            }
        }
        private static int eventBase = 0;

        private static void IssuePluginEvent(MediaSurfaceEventType eventType)
        {
            GL.IssuePluginEvent((int)eventType + EventBase);
        }

        /// <summary>
        /// Set up the video player with the movie surface texture id.
        /// </summary>
        AndroidJavaObject GetMediaPlayerOnTextureID(int texWidth, int texHeight)
        {
            Media_Surface_SetTextureParms(textureWidth, textureHeight);

            IntPtr androidSurface = Media_Surface_GetObject();
            AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");

            // Can't use AndroidJavaObject.Call() with a jobject, must use low level interface
            // mediaPlayer.Call("setSurface", androidSurface);
            IntPtr setSurfaceMethodID = AndroidJNI.GetMethodID(
                mediaPlayer.GetRawClass(),
                "setSurface", "(Landroid/view/Surface;)V");
            jvalue[] parms = new jvalue[1];
            parms[0] = new jvalue();
            parms[0].l = androidSurface;
            AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodID, parms);

            return mediaPlayer;
        }

        [DllImport("MediaSurface")]
        private static extern void Media_Surface_Init();

        [DllImport("MediaSurface")]
        private static extern void Media_Surface_SetEventBase(int eventBase);

        // This function returns an Android Surface object that is
        // bound to a SurfaceTexture object on an independent OpenGL texture id.
        [DllImport("MediaSurface")]
        private static extern IntPtr Media_Surface_GetObject();

        [DllImport("MediaSurface")]
        private static extern IntPtr Media_Surface_GetNativeTexture();

        [DllImport("MediaSurface")]
        private static extern void Media_Surface_SetTextureParms(int texWidth, int texHeight);
#endif
    }
}
