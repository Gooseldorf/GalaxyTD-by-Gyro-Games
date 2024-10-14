Shader "Custom/HpBarShader"
{
	Properties
	{
		_Health("Health", Range(0,1)) = 0
	}
		SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma vertex vert alpha
			#pragma fragment frag alpha
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			//#include "UnityCG.cginc"

			CBUFFER_START(UnityPerMaterial)
				float _Health;
			CBUFFER_END

#ifdef DOTS_INSTANCING_ON
			UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
				UNITY_DOTS_INSTANCED_PROP(float, _Health)
			UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _Health         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Health)
#endif

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED
				uint instanceID : INSTANCEID_SEMANTIC;
#endif
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
#if UNITY_ANY_INSTANCING_ENABLED
				uint instanceID : CUSTOM_INSTANCE_ID;
#endif
			};

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				//o.vertex = UnityObjectToClipPos(v.vertex);
				//o.vertex = v.vertex;
				o.uv = v.uv;
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				//if (i.uv.y < (1.0 / i.vertex.w) || i.uv.y > 1 - (1.0 / i.vertex.w) || i.uv.x < (1.0 / i.vertex.z) || i.uv.x > 1 - (10. / i.vertex.z)) return fixed4(0, 0, 0, 0.9);

				if (i.uv.x > _Health)
				{
					return half4(0, 0, 0, 0.5);
				}
				return lerp(half4(0.6, 0, 0, 1), half4(1, 0, 0, 1), i.uv.y);
			}
			ENDHLSL
		}
	}
}
