Shader"Custom/TestShader1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_OutlineColor("Outline Color", Color) = (1,1,1,1)
		_OutlineSize("Outline Size", int) = 1
        _Treshold("Treshold", float) = .6
    }
    SubShader
    {
        Tags
        { 
            "RenderType"="Transparent" 
            "Queue" = "Transparent"
			"PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
            // make fog work
            #pragma multi_compile_fog

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OutlineColor;
            int _OutlineSize;
            float _Treshold;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }

            fixed4 frag (v2f inp) : SV_Target
            {
                            // sample the texture
                fixed4 col = tex2D(_MainTex, inp.uv);
                if (col.a > _Treshold)
                {
                    float totalAlpha = 1.0;
                    [unroll(16)]
                    for (int i = 1; i < _OutlineSize + 1; i++)
                    {
                        fixed4 pixelUp = tex2D(_MainTex, inp.uv + fixed2(0, i * _MainTex_TexelSize.y));
                        fixed4 pixelDown = tex2D(_MainTex, inp.uv - fixed2(0, i * _MainTex_TexelSize.y));
                        fixed4 pixelRight = tex2D(_MainTex, inp.uv + fixed2(i * _MainTex_TexelSize.x, 0));
                        fixed4 pixelLeft = tex2D(_MainTex, inp.uv - fixed2(i * _MainTex_TexelSize.x, 0));

                        totalAlpha = totalAlpha * pixelUp.a * pixelDown.a * pixelRight.a * pixelLeft.a;
                    }
        
                    if (totalAlpha < _Treshold)
                        return _OutlineColor;
        
                }

                return col;
            }
            ENDCG
        }
    }
}
