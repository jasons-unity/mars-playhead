using Unity.Labs.MARS.Data;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class LocationUserExample : MonoBehaviour, IUsesGeoLocation
    {
#pragma warning disable 649
        [SerializeField]
        TextMesh m_Text;
#pragma warning restore 649

        void Start ()
        {
            this.SubscribeGeoLocationChanged(OnLocationUpdate);
        }

        void OnLocationUpdate (GeographicCoordinate coordinate)
        {
            m_Text.text = coordinate.ToString();
        }
    }
}

