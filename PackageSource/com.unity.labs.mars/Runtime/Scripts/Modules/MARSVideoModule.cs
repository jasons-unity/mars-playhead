using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.Labs.MARS
{
    public class MARSVideoModule : MonoBehaviour, IModuleBehaviorCallbacks, IProvidesCameraTexture
    {
        RenderTexture m_RenderTexture;
        Texture2D m_VideoTexture;

        public bool IsPaused { get; private set; }

        public VideoPlayer videoPlayer { get; private set; }

        public Texture2D GetCameraTexture() { return m_VideoTexture; }

        public void LoadModule()
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        public void UnloadModule() {}

        public void OnBehaviorAwake() {}

        public void OnBehaviorEnable() {}

        public void OnBehaviorStart() {}

        public void OnBehaviorUpdate()
        {
            if (m_RenderTexture == null)
                return;

            TextureUtils.RenderTextureToTexture2D(m_RenderTexture, m_VideoTexture);
        }

        public void OnBehaviorDisable() {}

        public void OnBehaviorDestroy() {}

        public void SetVideoClip(VideoClip value)
        {
            if (value == null)
            {
                m_RenderTexture = null;
                m_VideoTexture = null;
                videoPlayer.Stop();
            }
            else
            {
                var height = (int)value.height;
                var width = (int)value.width;

                if (m_RenderTexture == null || m_RenderTexture.width != width || m_RenderTexture.height != height)
                {
                    m_RenderTexture = new RenderTexture(width, height, 0);
                    m_VideoTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                    videoPlayer.targetTexture = m_RenderTexture;
                }

                if (!videoPlayer.isPlaying)
                {
                    videoPlayer.Play();
                    IsPaused = false;
                }
            }

            videoPlayer.clip = value;
        }

        public void LoadProvider() {}

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cameraPoseSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraTexture>;
            if (cameraPoseSubscriber != null)
                cameraPoseSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() {}

        internal void SetPauseVideo(bool isPaused, bool isPlaying = false)
        {
            IsPaused = isPaused;
            if (!isPlaying)
                return;

            if (videoPlayer == null)
                return;

            if (isPaused)
                videoPlayer.Pause();
            else
                videoPlayer.Play();
        }
    }
}
