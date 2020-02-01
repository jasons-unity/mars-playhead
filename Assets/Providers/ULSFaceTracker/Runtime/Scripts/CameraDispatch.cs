using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Queue = System.Collections.Generic.List<System.Action>;

namespace ULSTrackerForUnity
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Color32Bytes
    {
        [FieldOffset(0)]
        public byte[] byteArray;

        [FieldOffset(0)]
        public Color32[] colors;
    }

    public sealed class CameraDispatch : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("Which WebCam to use")]
        int m_WebcamIndex;
#pragma warning restore 649

        public bool isRunning
        {
            get { return running; }
        }

        bool m_WebCamInitialized;

        Thread mainThread;
        Queue invocation, execution;

        readonly object queueLock = new object();
        volatile bool running;

        public Action<bool> SetApplicationFocus;
        public Func<Texture2D> GetCameraTexture;

#if UNITY_STANDALONE || UNITY_EDITOR
        Color32Bytes m_ColorData;

        void OnDisable()
        {
            if (m_WebCamInitialized)
            {
                m_WebCamInitialized = false;
                Plugins.ULS_UnityCloseVideoCapture();
            }
        }
#endif

        void OnApplicationPause(bool paused)
        {
            if (SetApplicationFocus != null)
                SetApplicationFocus(paused);
        }

        public void SetRunning(bool enable)
        {
            running = enable;
        }

        public CameraDispatch()
        {
            mainThread = Thread.CurrentThread;
            invocation = new Queue();
            execution = new Queue();
            running = true;
        }

        ~CameraDispatch()
        {
            running = false;

            invocation.Clear();
            execution.Clear();

            invocation = execution = null;
            mainThread = null;
        }

        public void Clear()
        {
            invocation.Clear();
            execution.Clear();
        }

        public void Dispatch(Action action)
        {
            //Check that we aren't already on the target thread
            if (Thread.CurrentThread.ManagedThreadId == mainThread.ManagedThreadId)
            {
                //Debug.Log("Dispatch Execute");
                action();
            }
            //Enqueue
            else
            {
                lock (queueLock)
                {
                    invocation.Add(action);
                }
            }
        }

        //private void Update (Camera unused) {
        void Update()
        {
            //Lock
            lock (queueLock)
            {
                execution.AddRange(invocation);
                invocation.Clear();
            }

            //Execute
            foreach (var e in execution)
            {
                e();
            }

            execution.Clear();

#if UNITY_STANDALONE || UNITY_EDITOR
            Texture2D cameraTexture = null;
            if (GetCameraTexture != null)
                cameraTexture = GetCameraTexture();

            if (cameraTexture)
            {
                if (m_WebCamInitialized)
                {
                    m_WebCamInitialized = false;
                    Plugins.ULS_UnityCloseVideoCapture();
                }

                var width = cameraTexture.width;
                var height = cameraTexture.height;
                var colors = m_ColorData.colors;
                var sizeChange = colors == null || colors.Length != width * height;
                m_ColorData.colors = cameraTexture.GetPixels32(0);
                Plugins.ULS_UnityTrackerUpdate(m_ColorData.byteArray, width, height);

                if (sizeChange)
                {
                    if (Plugins.OnPreviewStart != null)
                        Plugins.OnPreviewStart(cameraTexture, 0);
                }
                else
                {
                    if (Plugins.OnPreviewUpdate != null)
                        Plugins.OnPreviewUpdate(cameraTexture);
                }
            }
            else
            {
                if (!m_WebCamInitialized)
                {
                    m_WebCamInitialized = true;
                    int ret = Plugins.ULS_UnityCreateVideoCapture(m_WebcamIndex, 640, 480, 60, -1);
                    Debug.Log("Initialize Desktop Camera: " + ret);
                }

                if (isRunning)
                {
                    Plugins.ULS_UnityUpdateVideoCapture();
                }
            }
#endif
        }
    }
}
