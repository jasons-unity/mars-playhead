using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CompositeCameraRenderer : MonoBehaviour
    {
        public Action PreRenderCamera;
        public Action PostRenderCamera;

        public Action<RenderTexture, RenderTexture> RenderImage;

        void OnPreRender()
        {
            if (PreRenderCamera != null)
                PreRenderCamera();
        }

        void OnPostRender()
        {
            if (PostRenderCamera != null)
                PostRenderCamera();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (RenderImage != null)
                RenderImage(src, dest);
            else
                Graphics.Blit(src, dest);
        }
    }
}
