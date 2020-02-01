using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Labs.MARS
{
    [ExecuteInEditMode]
    public class GeneratedPlanesRoot : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const string PlanesRootName = "Generated Planes";

        [SerializeField]
        List<Transform> m_ModifiedChildrenList = new List<Transform>();

#pragma warning disable 649
        [SerializeField]
        bool m_RootModified;
#pragma warning restore 649

        HashSet<Transform> m_ModifiedChildrenSet = new HashSet<Transform>();

        public bool anyObjectsModified { get { return m_RootModified || m_ModifiedChildrenSet.Count > 0; } }

        void Awake()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications += OnPostprocessModifications;
#endif
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            Undo.postprocessModifications -= OnPostprocessModifications;
#endif
        }

#if UNITY_EDITOR
        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                var target = modification.currentValue.target;
                var modifiedObject = target as GameObject;
                if (modifiedObject == null)
                {
                    var modifiedComponent = target as Component;
                    if (modifiedComponent != null)
                        modifiedObject = modifiedComponent.gameObject;
                }

                if (modifiedObject == null)
                    continue;

                var modifiedTrans = modifiedObject.transform;
                if (modifiedTrans.IsChildOf(transform))
                {
                    if (modifiedObject == gameObject)
                    {
                        m_RootModified = true;
                        continue;
                    }

                    // Mark the direct child of the root as modified
                    var modifiedDirectChild = modifiedTrans;
                    while (modifiedDirectChild.parent != transform)
                        modifiedDirectChild = modifiedDirectChild.parent;

                    m_ModifiedChildrenSet.Add(modifiedDirectChild);
                }
            }

            return modifications;
        }
#endif

        public void OnBeforeSerialize()
        {
            m_ModifiedChildrenList.Clear();
            m_ModifiedChildrenList.AddRange(m_ModifiedChildrenSet);
        }

        public void OnAfterDeserialize()
        {
            m_ModifiedChildrenSet.Clear();
            foreach (var child in m_ModifiedChildrenList)
            {
                m_ModifiedChildrenSet.Add(child);
            }
        }

#if UNITY_EDITOR
        public void DestroyExceptModifiedObjects(UndoBlock undoBlock)
        {
            var parent = transform.parent;
            foreach (var child in m_ModifiedChildrenSet)
            {
                // A child could have been destroyed after it was added to the modified set, so we need a null check
                if (child != null)
                    undoBlock.SetTransformParent(child.transform, parent);
            }
            Undo.DestroyObjectImmediate(gameObject);
        }
#endif
    }
}
