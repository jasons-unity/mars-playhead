using UnityEngine;
using UnityEngine.PrefabHandles;

namespace Unity.Labs.MARS
{
    [ExecuteInEditMode]
    public class BillboardHandle : HandleBehaviour
    {
        protected override void PreRender(Camera camera)
        {
            base.PreRender(camera);
            if (camera == null)
                return;

            var forward = camera.transform.forward;
            if (forward != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(forward, camera.transform.up);
        }
    }
}
