using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PrefabHandles;
using UnityEngine.Profiling;

namespace UnityEditor.PrefabHandles
{
    public abstract class EditorHandleContext : HandleContext
    {
        const int k_VisibleLayer = 0;
        const int k_HiddenLayer = 4;
        
        static readonly List<Animator> s_AnimatorCache = new List<Animator>(64);
        
        Camera m_HandleRenderCamera;

        protected EditorHandleContext() : this(null)
        {
        }

        protected EditorHandleContext(Camera camera) : base(camera)
        {
            EditorApplication.update += Update;
        }

        public sealed override void Dispose()
        {
            base.Dispose();
            EditorApplication.update -= Update;
        }

        void Update()
        {
            UpdateAnimators();
        }
        
        public sealed override GameObject CreateHandle(GameObject prefab)
        {
            var instance = base.CreateHandle(prefab);
            HandlePreviewScene.AddHandle(instance);
            return instance;
        }

        public sealed override bool DestroyHandle(GameObject handle)
        {
            return base.DestroyHandle(handle);
        }

        void UpdateAnimators()
        {
            //TODO reenable feature properly. Tracked: https://unity3d.atlassian.net/browse/WB-1650
            //GetOrCreateComponentCache(s_AnimatorCache);
            //for (int i = 0; i < s_AnimatorCache.Count; ++i)
            //{
            //    var animator = s_AnimatorCache[i];
            //    if (animator == null || !animator.isActiveAndEnabled)
            //        continue;

            //    animator.Update(Time.deltaTime);
            //}
        }

        public void RenderHandles(Rect rect, Camera cameraToCopy)
        {
            Profiler.BeginSample("Draw Handles");
            if (!HasActiveHandle(handles))
                return;

            var camera = HandlePreviewScene.camera;

            HandlePreviewScene.CopyCamera(cameraToCopy);

            camera.transform.localPosition = cameraToCopy.transform.position;
            camera.transform.localRotation = cameraToCopy.transform.rotation;

            camera.targetTexture = RenderTexture.GetTemporary(
                cameraToCopy.scaledPixelWidth,
                cameraToCopy.scaledPixelHeight,
                cameraToCopy.targetTexture.depth,
                cameraToCopy.targetTexture.graphicsFormat);
              
            DisableUnusedHandles(handles);
            
            SendPreRender(camera);
            camera.Render();
            SendPostRender(camera);
            
            Graphics.Blit(camera.targetTexture, cameraToCopy.targetTexture);
            
            RenderTexture.ReleaseTemporary(camera.targetTexture);
            camera.targetTexture = null;
            
            Profiler.EndSample();
        }

        bool HasActiveHandle(IReadOnlyList<GameObject> handles)
        {
            for (int i = 0, count = handles.Count; i < count; ++i)
            {
                if (handles[i].activeSelf)
                    return true;
            }

            return false;
        }

        void DisableUnusedHandles(IReadOnlyList<GameObject> handleToRender)
        {
            for (int i = 0, count = HandlePreviewScene.handles.Count; i < count; ++i)
            {
                var handle = HandlePreviewScene.handles[i];
                SetLayer_Recursive(handle.transform, k_HiddenLayer);
            }

            for (int i = 0, count = handleToRender.Count; i < count; ++i)
            { 
                SetLayer_Recursive(handleToRender[i].transform, k_VisibleLayer);
            }
        }

        void SetLayer_Recursive(Transform current, int layer)
        {
            current.gameObject.layer = layer;

            for (int i = 0, count = current.childCount; i < count; ++i)
            {
                SetLayer_Recursive(current.GetChild(i), layer);
            }
        }
    }
}