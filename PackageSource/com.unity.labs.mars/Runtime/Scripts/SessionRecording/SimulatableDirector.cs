using UnityEngine;
using UnityEngine.Playables;

namespace Unity.Labs.MARS
{
    [RequireComponent(typeof(PlayableDirector))]
    public class SimulatableDirector : MonoBehaviour, ISimulatable
    {
        PlayableDirector m_Director;

        public PlayableDirector Director
        {
            get
            {
                if (m_Director == null)
                    m_Director = GetComponent<PlayableDirector>();

                return m_Director;
            }
        }

        void OnEnable()
        {
            Director.Play();
        }

        void OnDisable()
        {
            Director.Stop();
        }
    }
}
