using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public abstract class DependencyConditionInspector : SpatialConditionInspector
    {
        protected DependencyCondition dependencyCondition { get; private set; }

        public override void OnEnable()
        {
            base.OnEnable();

            dependencyCondition = (DependencyCondition)target;
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawDependencyCondition(DependencyCondition dependencyCondition, GizmoType gizmoType)
        {
            if (!dependencyCondition.useExplicitDependency)
                return;

            var client = dependencyCondition.proxy;
            if (client == null)
                return;

            var isSimulationView = SceneView.currentDrawingSceneView is SimulationView;
            var simulated = SimulatedObjectsManager.IsSimulatedObject(client.gameObject);
            if (isSimulationView && !simulated || !isSimulationView && simulated)
                return;

            if (simulated && client.queryState != QueryState.Tracking)
                return;

            var transform = dependencyCondition.transform;
            var startPosition = transform.position;
            Handles.matrix = Matrix4x4.identity;
            Handles.color =
                transform.GetComponents<Condition>().Any(condition => condition.adjusting)
                ? MARSUserPreferences.instance.editingRelationColor
                : MARSUserPreferences.instance.relationColor;
            HandleUtils.SphereHandleCap(startPosition);

            if (dependencyCondition.dependency == null)
                return;

            var dependencyPosition = dependencyCondition.dependency.transform.position;
            if (dependencyPosition != startPosition)
            {
                var transformToDependency = dependencyPosition - startPosition;
                HandleUtils.DrawLine(startPosition, dependencyPosition);
                HandleUtils.ConeHandleCap(dependencyPosition, Quaternion.LookRotation(transformToDependency));
            }
        }
    }
}
