using UnityEngine;

namespace Unity.Labs.MARS
{
    public class RedirectSelection : MonoBehaviour
    {
        // When this gameObject is selected, the target is the GameObject that should be selected instead
        public GameObject target { get; set; }
    }
}
