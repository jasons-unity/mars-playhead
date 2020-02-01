using UnityEditor.EditorTools;
using UnityEditor.PrefabHandles;
using UnityEngine;
using UnityEngine.PrefabHandles;

namespace UnityEditor.ManipulationTools
{
    [EditorTool("Move Tool")]
    public sealed class MoveTool : EditorTool
    {
        public override GUIContent toolbarIcon
        {
            get { return GUIContent.none; } //TODO
        }

        PositionHandle m_PositionHandle;

        void OnEnable()
        {
            m_PositionHandle = SceneViewContext.activeViewContext.CreateHandle(DefaultHandle.PositionHandle).GetComponent<PositionHandle>();
            m_PositionHandle.translationUpdated += TranslationUpdated;
            m_PositionHandle.gameObject.SetActive(false);
            EditorTools.EditorTools.activeToolChanging += OnActiveToolChanging;
            EditorTools.EditorTools.activeToolChanged += OnActiveToolChanged; 
        }
        
        void OnActiveToolChanging()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
            {
                if (m_PositionHandle) m_PositionHandle.gameObject.SetActive(false);
            }
        }

        void OnActiveToolChanged()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
            {
                if (m_PositionHandle) m_PositionHandle.gameObject.SetActive(true);
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.Layout)
            {
                bool isHidden = Tools.hidden || Selection.activeTransform == null;
                if (isHidden)
                {
                    m_PositionHandle.gameObject.SetActive(false);
                }
                else
                {
                    m_PositionHandle.gameObject.SetActive(true);
                    m_PositionHandle.transform.localPosition = Tools.handlePosition;
                    m_PositionHandle.transform.localRotation = Tools.handleRotation;
                }
            }
        }

        public void OnDisable()
        {
            EditorTools.EditorTools.activeToolChanging -= OnActiveToolChanging;
            EditorTools.EditorTools.activeToolChanged -= OnActiveToolChanged;

            SceneViewContext.activeViewContext.DestroyHandle(m_PositionHandle.gameObject);
            m_PositionHandle = null;
        }

        void TranslationUpdated(TranslationUpdateInfo info)
        {
            foreach (var transform in Selection.transforms)
            {
                Undo.RecordObject(transform, "Move Transform with handle");
                transform.position += info.world.delta;
            }
        }
    }
}