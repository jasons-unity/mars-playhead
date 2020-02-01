using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Labs.MARS
{
    [Serializable]
    public class SimulationRenderSettings
    {
        [SerializeField]
        float m_FogEndDistance = 300f;

        [SerializeField]
        FogMode m_FogMode = FogMode.ExponentialSquared;

        [SerializeField]
        Color m_FogColor = Color.grey;

        [SerializeField]
        float m_FogDensity = 0.01f;

        [SerializeField]
        AmbientMode m_AmbientMode = AmbientMode.Skybox;

        [SerializeField]
        Color m_AmbientSkyColor = new Color(0.212f, 0.227f, 0.259f);

        [SerializeField]
        Color m_AmbientEquatorColor = new Color(0.115f, 0.125f, 0.135f);

        [SerializeField]
        Color m_AmbientGroundColor = new Color(0.047f, 0.045f, 0.035f);

        [SerializeField]
        float m_AmbientIntensity = 1f;

        [SerializeField]
        Color m_AmbientLight = new Color(0.212f, 0.227f, 0.259f);

        [SerializeField]
        Color m_SubtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f);

        [SerializeField]
        float m_ReflectionIntensity = 1f;

        [SerializeField]
        int m_ReflectionBounces = 1;

        [SerializeField]
        DefaultReflectionMode m_DefaultReflectionMode = DefaultReflectionMode.Skybox;

        [SerializeField]
        int m_DefaultReflectionResolution = 128;

        [SerializeField]
        float m_HaloStrength = 0.5f;

        [SerializeField]
        float m_FlareStrength = 1f;

        [SerializeField]

        float m_FlareFadeSpeed = 3f;
#pragma warning disable 649
        [SerializeField]
        bool m_Fog;

        [SerializeField]
        float m_FogStartDistance;

        [SerializeField]
        Material m_Skybox;

        [SerializeField]
        Cubemap m_CustomReflection;

        [SerializeField]
        bool m_UseSceneSun;

        [SerializeField]
        Light m_Sun;

        [SerializeField]
        SphericalHarmonicsL2 m_AmbientProbe;
#pragma warning restore 649

        Light m_SceneSun;
        SphericalHarmonicsL2 m_SceneAmbientProbe;
        AmbientMode m_SceneAmbientMode;

        public void UseSceneRenderSettings()
        {
            m_Fog = RenderSettings.fog;
            m_FogStartDistance = RenderSettings.fogStartDistance;
            m_FogEndDistance = RenderSettings.fogEndDistance;
            m_FogMode = RenderSettings.fogMode;
            m_FogColor = RenderSettings.fogColor;
            m_AmbientSkyColor = RenderSettings.ambientSkyColor;
            m_AmbientEquatorColor = RenderSettings.ambientEquatorColor;
            m_AmbientGroundColor = RenderSettings.ambientGroundColor;
            m_AmbientIntensity = RenderSettings.ambientIntensity;
            m_AmbientLight = RenderSettings.ambientLight;
            m_SubtractiveShadowColor = RenderSettings.subtractiveShadowColor;
            m_Skybox = RenderSettings.skybox;
            m_CustomReflection = RenderSettings.customReflection;
            m_ReflectionIntensity = RenderSettings.reflectionIntensity;
            m_ReflectionBounces = RenderSettings.reflectionBounces;
            m_DefaultReflectionMode = RenderSettings.defaultReflectionMode;
            m_DefaultReflectionResolution = RenderSettings.defaultReflectionResolution;
            m_HaloStrength = RenderSettings.haloStrength;
            m_FlareStrength = RenderSettings.flareStrength;
            m_FlareFadeSpeed = RenderSettings.flareFadeSpeed;

            m_UseSceneSun = true;
            UpdateUsingSunSceneRenderSettingsValues();
        }

        public void ApplyTempRenderSettings()
        {
            UpdateUsingSunSceneRenderSettingsValues();

            RenderSettings.fog = m_Fog;
            RenderSettings.fogStartDistance = m_FogStartDistance;
            RenderSettings.fogEndDistance = m_FogEndDistance;
            RenderSettings.fogMode = m_FogMode;
            RenderSettings.fogColor = m_FogColor;
            RenderSettings.fogDensity = m_FogDensity;
            RenderSettings.ambientMode = m_UseSceneSun ? m_SceneAmbientMode : m_AmbientMode;
            RenderSettings.ambientSkyColor = m_AmbientSkyColor;
            RenderSettings.ambientEquatorColor = m_AmbientEquatorColor;
            RenderSettings.ambientGroundColor = m_AmbientGroundColor;
            RenderSettings.ambientIntensity = m_AmbientIntensity;
            RenderSettings.ambientLight = m_AmbientLight;
            RenderSettings.subtractiveShadowColor = m_SubtractiveShadowColor;
            RenderSettings.skybox = m_Skybox;
            RenderSettings.sun = m_UseSceneSun ? m_SceneSun : m_Sun;
            RenderSettings.ambientProbe = m_UseSceneSun ? m_SceneAmbientProbe : m_AmbientProbe;
            RenderSettings.customReflection = m_CustomReflection;
            RenderSettings.reflectionIntensity = m_ReflectionIntensity;
            RenderSettings.reflectionBounces = m_ReflectionBounces;
            RenderSettings.defaultReflectionMode = m_DefaultReflectionMode;
            RenderSettings.defaultReflectionResolution = m_DefaultReflectionResolution;
            RenderSettings.haloStrength = m_HaloStrength;
            RenderSettings.flareStrength = m_FlareStrength;
            RenderSettings.flareFadeSpeed = m_FlareFadeSpeed;
        }

        void UpdateUsingSunSceneRenderSettingsValues()
        {
            if (!m_UseSceneSun)
                return;

            m_SceneSun = RenderSettings.sun;
            m_SceneAmbientProbe = RenderSettings.ambientProbe;
            m_SceneAmbientMode = RenderSettings.ambientMode;
        }
    }
}
