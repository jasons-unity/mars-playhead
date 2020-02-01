using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    public class SceneSwitcher : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Title;
#pragma warning restore 649

#if PLATFORM_LUMIN
        void Awake()
        {
            gameObject.SetActive(false);
        }
#endif

        void Start()
        {
            SetTitle();
        }

        public void PreviousScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
            SetTitle();
        }

        public void NextScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            SetTitle();
        }

        void SetTitle()
        {
            m_Title.text = SceneManager.GetActiveScene().name;
        }

        static void LoadScene(int index)
        {
            if (index < 0)
                index = SceneManager.sceneCountInBuildSettings - 1;

            if (index >= SceneManager.sceneCountInBuildSettings)
                index = 0;

            // We assume each scene loads a "fresh state" so unpause before switching scenes
            MARSCore.instance.paused = false;
            SceneManager.LoadScene(index);
        }
    }
}
