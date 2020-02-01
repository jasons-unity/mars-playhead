Shader "Unlit/TransparentOccluder"
{
	Properties
	{
		_FadeTex ("Fade Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
		LOD 100
        COLORMASK A
        Zwrite on
        ZTest LEqual

        Blend One One, SrcAlpha Zero

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _FadeTex;
			float4 _FadeTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _FadeTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_FadeTex, i.uv);

                // We write the fade value in the alpha channel, so that the occluded objects can read this for blending purposes
                col.a = col.r;
				return col;
			}
			ENDCG
		}
	}
}
