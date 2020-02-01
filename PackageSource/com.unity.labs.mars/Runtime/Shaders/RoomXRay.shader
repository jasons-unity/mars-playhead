Shader "MARS/Room X-Ray"
{
    // A standard shader variant that takes the global room properties and applies them to cut out a view into the geometry based on camera location
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow nofog finalcolor:fadeEdge

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "XRayCommon.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half lightValue = getXRayFade(IN.worldPos);

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            o.Alpha = lightValue;
        }

        void fadeEdge(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            color *= o.Alpha;
        }
        ENDCG

        Zwrite On
        blend srcalpha oneminussrcalpha

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf NoLight noshadow nofog keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "XRayCommon.cginc"

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            half lightValue = getXRayEdgeFade(IN.worldPos);

            o.Albedo = 0;
            o.Alpha = lightValue;
        }

        half4 LightingNoLight (SurfaceOutput s, half3 lightDir, half atten)
        {
            half4 c;
            c.rgb = s.Albedo;
            c.a = s.Alpha;
            return c;
        }

        ENDCG
    }
    Fallback "Unlit/Transparent"
}
