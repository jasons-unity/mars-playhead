using UnityEngine;
using UnityEngine.PrefabHandles;

namespace UnityEditor.PrefabHandles
{
    public sealed class SceneViewContext : EditorWindowContext
    {
        public static IHandleContext activeViewContext
        {
            get { return s_ActiveViewContext; }
        }

        static readonly SceneViewContext s_ActiveViewContext = new SceneViewContext();

        static SceneViewContext()
        {
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        static void DuringSceneGUI(SceneView view)
        {
            if (view == SceneView.lastActiveSceneView)
                s_ActiveViewContext.OnGUI();
        }

        public void OnGUI()
        {
            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null)
                return;

            var rect = EditorGUIUtility.PixelsToPoints(sceneView.camera.pixelRect);
            OnGUI(sceneView, rect);
        }

        protected override void Repaint(Rect rect)
        {
            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null)
                return;

            Handles.BeginGUI();
            RenderHandles(rect, sceneView.camera);
            Handles.EndGUI();
        }
    }
}