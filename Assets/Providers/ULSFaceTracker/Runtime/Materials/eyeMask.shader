﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/eyeMask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
  		_Color ("Main Color", COLOR) = (1,1,1,1)
		_ColorMask("ColorMask",Int) = 0 

		_Alpha("Alpha",Float) = 0.5

		_Stencil("Stencil",Int) = 0 
		[Enum(UnityEngine.Rendering.CompareFunction)] _Compare("Compare", Int) = 8 //"Always"
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("StencilOp", Int) = 0 //"Keep"

		[Enum(Off,0, On,1)] _ZWrite ("ZWrite", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend Mode", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend Mode", Int) = 1
	}
	SubShader
	{
		ColorMask [_ColorMask]
		ZWrite [_ZWrite]
		Blend [_SrcBlend] [_DstBlend]

		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Stencil {
                Ref [_Stencil]
                Comp [_Compare]
                Pass [_StencilOp]
            }
        
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//make fog work
			//#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Alpha;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				//UNITY_APPLY_FOG(i.fogCoord, col);
				col.a *=_Alpha;
				return col*_Color;
			}
			ENDCG
		}
	}
}
