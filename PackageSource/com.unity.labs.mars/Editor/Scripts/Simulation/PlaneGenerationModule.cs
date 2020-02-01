using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class PlaneGenerationModule : IModule, IUsesPlaneFinding
    {
#if !FI_AUTOFILL
        public IProvidesPlaneFinding provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        // Reference type collections must also be cleared after use
        static readonly List<MRPlane> k_Planes = new List<MRPlane>();
        static readonly List<GeneratedPlanesRoot> k_PlanesRoots = new List<GeneratedPlanesRoot>();

        public void LoadModule() { }

        public void UnloadModule() { }

        public static void TryDestroyPreviousPlanes(GameObject environmentRoot, string preserveObjectsDialogTitle, UndoBlock undoBlock)
        {
            // k_PlanesRoots is cleared by GetComponentsInChildren
            environmentRoot.GetComponentsInChildren(k_PlanesRoots);
            foreach (var planesRoot in k_PlanesRoots)
            {
                var preserveModifiedObjects = false;
                if (planesRoot.anyObjectsModified)
                {
                    preserveModifiedObjects = EditorUtility.DisplayDialog(preserveObjectsDialogTitle,
                        "The previous generated planes have been modified. Would you like to preserve the modified objects?",
                        "Yes", "No");
                }

                if (preserveModifiedObjects)
                    planesRoot.DestroyExceptModifiedObjects(undoBlock);
                else
                    Undo.DestroyObjectImmediate(planesRoot.gameObject);
            }

            k_PlanesRoots.Clear();
        }

        public void SavePlanesFromSimulation(GameObject environmentRoot)
        {
            using (var undoBlock = new UndoBlock("Save Planes From Simulation"))
            {
                k_Planes.Clear();
                this.GetPlanes(k_Planes);

                TryDestroyPreviousPlanes(environmentRoot, "Saving Planes", undoBlock);

                var newPlanesRoot = new GameObject(GeneratedPlanesRoot.PlanesRootName, typeof(GeneratedPlanesRoot)).transform;
                newPlanesRoot.SetParent(environmentRoot.transform);
                undoBlock.RegisterCreatedObject(newPlanesRoot.gameObject);

                var simPlanePrefab = MARSEditorPrefabs.instance.GeneratedSimulatedPlanePrefab;
                foreach (var plane in k_Planes)
                {
                    var synthPlane = Object.Instantiate(simPlanePrefab, newPlanesRoot.transform);
                    synthPlane.transform.SetWorldPose(plane.pose);
                    synthPlane.SetMRPlaneData(plane.vertices, plane.center, plane.extents);
                }
            }
        }
    }
}
