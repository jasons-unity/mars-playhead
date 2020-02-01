using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class MARSRuntimeUtils
    {
        /// <summary>
        /// If this method is called during simulation, it returns the user camera in the simulation scene.
        /// Otherwise this method returns the main camera.
        /// </summary>
        public static Camera GetMainOrSimulatedCamera()
        {
#if UNITY_EDITOR
            if (EditorOnlyDelegates.TryGetSimulatedCamera != null)
            {
                var simulatedCamera = EditorOnlyDelegates.TryGetSimulatedCamera();
                if (simulatedCamera != null)
                    return simulatedCamera;
            }
#endif
            return Camera.main;
        }
    }
}
