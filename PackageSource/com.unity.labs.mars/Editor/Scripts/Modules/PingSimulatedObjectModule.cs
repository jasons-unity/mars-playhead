using Unity.Labs.ModuleLoader;
using UnityEditor;

namespace Unity.Labs.MARS
{
    public class PingSimulatedObjectModule : IModule
    {
        public void LoadModule()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void UnloadModule()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            HierarchyTreeView.UnsetPing();
        }

        static void OnSelectionChanged()
        {
            HierarchyTreeView.PingObjects(Selection.transforms);
        }
    }
}
